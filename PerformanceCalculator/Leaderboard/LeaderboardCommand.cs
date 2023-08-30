// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Alba.CsConsoleFormat;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets.Mods;

namespace PerformanceCalculator.Leaderboard
{
    [Command(Name = "leaderboard", Description = "Computes the performance (pp) for every player in a part of the leaderboard.")]
    public class LeaderboardCommand : ApiCommand
    {
        [UsedImplicitly]
        [Option(Template = "-r|--ruleset:<ruleset-id>", Description = "The ruleset to compute the leaderboard for.\n"
                                                                      + "Values: 0 - osu!, 1 - osu!taiko, 2 - osu!catch, 3 - osu!mania")]
        [AllowedValues("0", "1", "2", "3")]
        public int? Ruleset { get; }

        [UsedImplicitly]
        [Option(Template = "-l|--limit:<amount-of-players>", Description = "How many players to compute (max. 50)")]
        public int? Limit { get; } = 10;

        [UsedImplicitly]
        [Option(Template = "-p|--page:<page-number>", Description = "Leaderboard page number.")]
        public int? LeaderboardPage { get; } = 1;

        public override void Execute()
        {
            var rulesetApiName = LegacyHelper.GetRulesetShortNameFromId(Ruleset ?? 0);
            var leaderboard = GetJsonFromApi<GetTopUsersResponse>($"rankings/{rulesetApiName}/performance?cursor[page]={LeaderboardPage - 1}");

            var calculatedPlayers = new List<LeaderboardPlayerInfo>();

            foreach (var player in leaderboard.Users)
            {
                if (calculatedPlayers.Count >= Limit)
                    break;

                var plays = new List<(double local, double live)>();

                var ruleset = LegacyHelper.GetRulesetFromLegacyID(Ruleset ?? 0);

                Console.WriteLine($"Calculating {player.User.Username} top scores...");

                foreach (var play in GetJsonFromApi<List<SoloScoreInfo>>($"users/{player.User.Id}/scores/best?mode={rulesetApiName}&limit=100"))
                {
                    var working = ProcessorWorkingBeatmap.FromFileOrId(play.BeatmapID.ToString());

                    Mod[] mods = play.Mods.Select(x => x.ToMod(ruleset)).ToArray();

                    var scoreInfo = play.ToScoreInfo(mods);

                    var score = new ProcessorScoreDecoder(working).Parse(scoreInfo);

                    var difficultyCalculator = ruleset.CreateDifficultyCalculator(working);
                    var difficultyAttributes = difficultyCalculator.Calculate(LegacyHelper.ConvertToLegacyDifficultyAdjustmentMods(working.BeatmapInfo, ruleset, scoreInfo.Mods).ToArray());
                    var performanceCalculator = ruleset.CreatePerformanceCalculator();

                    plays.Add((performanceCalculator?.Calculate(score.ScoreInfo, difficultyAttributes).Total ?? 0, play.PP ?? 0.0));
                }

                var localOrdered = plays.Select(x => x.Item1).OrderByDescending(x => x).ToList();
                var liveOrdered = plays.Select(x => x.Item2).OrderByDescending(x => x).ToList();

                int index = 0;
                double totalLocalPP = localOrdered.Sum(play => Math.Pow(0.95, index++) * play);
                double totalLivePP = (double)(player.PP ?? 0);

                index = 0;
                double nonBonusLivePP = liveOrdered.Sum(play => Math.Pow(0.95, index++) * play);

                //todo: implement properly. this is pretty damn wrong.
                var playcountBonusPP = (totalLivePP - nonBonusLivePP);
                totalLocalPP += playcountBonusPP;

                calculatedPlayers.Add(new LeaderboardPlayerInfo
                {
                    LivePP = totalLivePP,
                    LocalPP = totalLocalPP,
                    Username = player.User.Username
                });
            }

            calculatedPlayers = calculatedPlayers.OrderByDescending(x => x.LocalPP).ToList();
            var liveOrderedPlayers = calculatedPlayers.OrderByDescending(x => x.LivePP).ToList();

            if (OutputJson)
            {
                var json = JsonConvert.SerializeObject(calculatedPlayers);

                Console.Write(json);

                if (OutputFile != null)
                    File.WriteAllText(OutputFile, json);
            }
            else
            {
                OutputDocument(new Document(
                    new Grid
                    {
                        Columns =
                        {
                            GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto
                        },
                        Children =
                        {
                            new Cell("#"),
                            new Cell("username"),
                            new Cell("live pp"),
                            new Cell("local pp"),
                            new Cell("pp change"),
                            calculatedPlayers.Select(item => new[]
                            {
                                new Cell($"{liveOrderedPlayers.IndexOf(item) - calculatedPlayers.IndexOf(item):+0;-0;-}"),
                                new Cell($"{item.Username}"),
                                new Cell($"{item.LivePP:F1}") { Align = Align.Right },
                                new Cell($"{item.LocalPP:F1}") { Align = Align.Right },
                                new Cell($"{item.LocalPP - item.LivePP:F1}") { Align = Align.Right }
                            })
                        }
                    })
                );
            }
        }
    }
}
