// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Alba.CsConsoleFormat;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;

namespace PerformanceCalculator.Profile
{
    [Command(Name = "profile", Description = "Computes the total performance (pp) of a profile.")]
    public class ProfileCommand : ApiCommand
    {
        [UsedImplicitly]
        [Argument(0, Name = "user", Description = "User ID is preferred, but username should also work.")]
        public string ProfileName { get; }

        [UsedImplicitly]
        [Option(Template = "-r|--ruleset:<ruleset-id>", Description = "The ruleset to compute the profile for.\n"
                                                                      + "Values: 0 - osu!, 1 - osu!taiko, 2 - osu!catch, 3 - osu!mania")]
        [AllowedValues("0", "1", "2", "3")]
        public int? Ruleset { get; }

        private const int max_api_scores = 200;
        private const int max_api_scores_in_one_query = 100;

        public override void Execute()
        {
            var displayPlays = new List<UserPlayInfo>();

            var ruleset = LegacyHelper.GetRulesetFromLegacyID(Ruleset ?? 0);
            string rulesetApiName = LegacyHelper.GetRulesetShortNameFromId(Ruleset ?? 0);

            Console.WriteLine("Getting user data...");
            var userData = GetJsonFromApi<APIUser>($"users/{ProfileName}/{ruleset.ShortName}");

            Console.WriteLine("Getting user top scores...");

            var apiScores = new List<SoloScoreInfo>();

            for (int i = 0; i < max_api_scores; i += max_api_scores_in_one_query)
            {
                apiScores.AddRange(GetJsonFromApi<List<SoloScoreInfo>>($"users/{userData.Id}/scores/best?mode={rulesetApiName}&limit={max_api_scores_in_one_query}&offset={i}"));
                Thread.Sleep(200);
            }

            foreach (var play in apiScores)
            {
                var working = ProcessorWorkingBeatmap.FromFileOrId(play.BeatmapID.ToString());

                Mod[] mods = play.Mods.Select(x => x.ToMod(ruleset)).ToArray();

                var scoreInfo = play.ToScoreInfo(mods, working.BeatmapInfo);
                scoreInfo.Ruleset = ruleset.RulesetInfo;

                var difficultyCalculator = ruleset.CreateDifficultyCalculator(working);
                var difficultyAttributes = difficultyCalculator.Calculate(scoreInfo.Mods);
                var performanceCalculator = ruleset.CreatePerformanceCalculator();

                var ppAttributes = performanceCalculator?.Calculate(scoreInfo, difficultyAttributes);
                var thisPlay = new UserPlayInfo
                {
                    Beatmap = working.BeatmapInfo,
                    LocalPP = ppAttributes?.Total ?? 0,
                    LivePP = play.PP ?? 0.0,
                    Mods = scoreInfo.Mods.Select(m => m.Acronym).ToArray(),
                    MissCount = play.Statistics.GetValueOrDefault(HitResult.Miss),
                    Accuracy = scoreInfo.Accuracy * 100,
                    Combo = play.MaxCombo,
                    MaxCombo = difficultyAttributes.MaxCombo
                };

                displayPlays.Add(thisPlay);
            }

            var localOrdered = displayPlays.OrderByDescending(p => p.LocalPP).ToList();
            var liveOrdered = displayPlays.OrderByDescending(p => p.LivePP).ToList();

            int index = 0;
            double totalLocalPP = localOrdered.Sum(play => Math.Pow(0.95, index++) * play.LocalPP);
            double totalLivePP = (double)(userData.Statistics.PP ?? 0);

            index = 0;
            double nonBonusLivePP = liveOrdered.Sum(play => Math.Pow(0.95, index++) * play.LivePP);

            //todo: implement properly. this is pretty damn wrong.
            double playcountBonusPP = (totalLivePP - nonBonusLivePP);
            totalLocalPP += playcountBonusPP;
            double totalDiffPP = totalLocalPP - totalLivePP;

            if (OutputJson)
            {
                string json = JsonConvert.SerializeObject(new
                {
                    userData.Username,
                    LivePp = totalLivePP,
                    LocalPp = totalLocalPP,
                    PlaycountPp = playcountBonusPP,
                    Scores = localOrdered.Select(item => new
                    {
                        BeatmapId = item.Beatmap.OnlineID,
                        BeatmapName = item.Beatmap.ToString(),
                        item.Combo,
                        item.Accuracy,
                        item.MissCount,
                        item.Mods,
                        LivePp = item.LivePP,
                        LocalPp = item.LocalPP,
                        PositionChange = liveOrdered.IndexOf(item) - localOrdered.IndexOf(item)
                    })
                });

                Console.Write(json);
            }
            else
            {
                OutputDocument(new Document(
                    new Span($"User:     {userData.Username}"), "\n",
                    new Span($"Live PP:  {totalLivePP:F1} (including {playcountBonusPP:F1}pp from playcount)"), "\n",
                    new Span($"Local PP: {totalLocalPP:F1} ({totalDiffPP:+0.0;-0.0;-})"), "\n",
                    new Grid
                    {
                        Columns =
                        {
                            GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto
                        },
                        Children =
                        {
                            new Cell("#"),
                            new Cell("beatmap"),
                            new Cell("max combo"),
                            new Cell("accuracy"),
                            new Cell("misses"),
                            new Cell("mods"),
                            new Cell("live pp"),
                            new Cell("local pp"),
                            new Cell("pp change"),
                            new Cell("position change"),
                            localOrdered.Select(item => new[]
                            {
                                new Cell($"{localOrdered.IndexOf(item) + 1}"),
                                new Cell($"{item.Beatmap.OnlineID} - {item.Beatmap}"),
                                new Cell($"{item.Combo}/{item.MaxCombo}x") { Align = Align.Right },
                                new Cell($"{Math.Round(item.Accuracy, 2)}%") { Align = Align.Right },
                                new Cell($"{item.MissCount}") { Align = Align.Right },
                                new Cell($"{(item.Mods.Length > 0 ? string.Join(", ", item.Mods) : "None")}") { Align = Align.Right },
                                new Cell($"{item.LivePP:F1}") { Align = Align.Right },
                                new Cell($"{item.LocalPP:F1}") { Align = Align.Right },
                                new Cell($"{item.LocalPP - item.LivePP:F1}") { Align = Align.Right },
                                new Cell($"{liveOrdered.IndexOf(item) - localOrdered.IndexOf(item):+0;-0;-}") { Align = Align.Center },
                            })
                        }
                    })
                );
            }
        }
    }
}
