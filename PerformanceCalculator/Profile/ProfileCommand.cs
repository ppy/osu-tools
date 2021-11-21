// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Alba.CsConsoleFormat;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;

namespace PerformanceCalculator.Profile
{
    [Command(Name = "profile", Description = "Computes the total performance (pp) of a profile.")]
    public class ProfileCommand : ApiCommand
    {
        [UsedImplicitly]
        [Required]
        [Argument(0, Name = "user", Description = "User ID is preferred, but username should also work.")]
        public string ProfileName { get; }

        [UsedImplicitly]
        [Option(Template = "-r|--ruleset:<ruleset-id>", Description = "The ruleset to compute the profile for.\n"
                                                                      + "Values: 0 - osu!, 1 - osu!taiko, 2 - osu!catch, 3 - osu!mania")]
        [AllowedValues("0", "1", "2", "3")]
        public int? Ruleset { get; }

        [UsedImplicitly]
        [Option(Template = "-j|--json", Description = "Output results as JSON.")]
        public bool OutputJson { get; }

        public override void Execute()
        {
            var displayPlays = new List<UserPlayInfo>();

            var ruleset = LegacyHelper.GetRulesetFromLegacyID(Ruleset ?? 0);
            var rulesetApiName = LegacyHelper.GetRulesetShortNameFromId(Ruleset ?? 0);

            Console.WriteLine("Getting user data...");
            dynamic userData = GetJsonFromApi($"users/{ProfileName}/{rulesetApiName}");

            Console.WriteLine("Getting user top scores...");

            foreach (var play in GetJsonFromApi($"users/{userData.id}/scores/best?mode={rulesetApiName}&limit=100"))
            {
                var working = ProcessorWorkingBeatmap.FromFileOrId((string)play.beatmap.id);

                var modsAcronyms = ((JArray)play.mods).Select(x => x.ToString()).ToArray();
                Mod[] mods = ruleset.CreateAllMods().Where(m => modsAcronyms.Contains(m.Acronym)).ToArray();

                var scoreInfo = new ScoreInfo
                {
                    Ruleset = ruleset.RulesetInfo,
                    TotalScore = play.score,
                    MaxCombo = play.max_combo,
                    Mods = mods,
                    Statistics = new Dictionary<HitResult, int>()
                };

                scoreInfo.SetCount300((int)play.statistics.count_300);
                scoreInfo.SetCountGeki((int)play.statistics.count_geki);
                scoreInfo.SetCount100((int)play.statistics.count_100);
                scoreInfo.SetCountKatu((int)play.statistics.count_katu);
                scoreInfo.SetCount50((int)play.statistics.count_50);
                scoreInfo.SetCountMiss((int)play.statistics.count_miss);

                var score = new ProcessorScoreDecoder(working).Parse(scoreInfo);

                var difficultyCalculator = ruleset.CreateDifficultyCalculator(working);
                var difficultyAttributes = difficultyCalculator.Calculate(LegacyHelper.TrimNonDifficultyAdjustmentMods(ruleset, scoreInfo.Mods).ToArray());
                var performanceCalculator = ruleset.CreatePerformanceCalculator(difficultyAttributes, score.ScoreInfo);

                var categories = new Dictionary<string, double>();
                var localPP = performanceCalculator.Calculate(categories);
                var thisPlay = new UserPlayInfo
                {
                    Beatmap = working.BeatmapInfo,
                    LocalPP = localPP,
                    LivePP = play.pp,
                    Mods = scoreInfo.Mods.Select(m => m.Acronym).ToArray(),
                    MissCount = play.statistics.count_miss,
                    Accuracy = scoreInfo.Accuracy * 100,
                    Combo = play.max_combo,
                    MaxCombo = (int)categories.GetValueOrDefault("Max Combo")
                };

                displayPlays.Add(thisPlay);
            }

            var localOrdered = displayPlays.OrderByDescending(p => p.LocalPP).ToList();
            var liveOrdered = displayPlays.OrderByDescending(p => p.LivePP).ToList();

            int index = 0;
            double totalLocalPP = localOrdered.Sum(play => Math.Pow(0.95, index++) * play.LocalPP);
            double totalLivePP = userData.statistics.pp;

            index = 0;
            double nonBonusLivePP = liveOrdered.Sum(play => Math.Pow(0.95, index++) * play.LivePP);

            //todo: implement properly. this is pretty damn wrong.
            var playcountBonusPP = (totalLivePP - nonBonusLivePP);
            totalLocalPP += playcountBonusPP;
            double totalDiffPP = totalLocalPP - totalLivePP;

            if (OutputJson)
            {
                var json = JsonConvert.SerializeObject(new
                {
                    Username = userData.username,
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

                if (OutputFile != null)
                    File.WriteAllText(OutputFile, json);
            }
            else
            {
                OutputDocument(new Document(
                    new Span($"User:     {userData.username}"), "\n",
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
