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
using Newtonsoft.Json.Linq;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;

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

        [UsedImplicitly]
        [Option(Template = "-j|--json", Description = "Output results as JSON.")]
        public bool OutputJson { get; }

        public override void Execute()
        {
            var rulesetApiName = LegacyHelper.GetRulesetShortNameFromId(Ruleset ?? 0);
            var leaderboard = GetJsonFromApi($"rankings/{rulesetApiName}/performance?cursor[page]={LeaderboardPage - 1}");

            var calculatedPlayers = new List<LeaderboardPlayerInfo>();

            foreach (var player in leaderboard.ranking)
            {
                if (calculatedPlayers.Count >= Limit)
                    break;

                var plays = new List<(double, double)>(); // (local, live)

                var ruleset = LegacyHelper.GetRulesetFromLegacyID(Ruleset ?? 0);

                Console.WriteLine($"Calculating {player.user.username} top scores...");

                foreach (var play in GetJsonFromApi($"users/{player.user.id}/scores/best?mode={rulesetApiName}&limit=100"))
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
                    plays.Add((performanceCalculator.Calculate(categories), play.pp));
                }

                var localOrdered = plays.Select(x => x.Item1).OrderByDescending(x => x).ToList();
                var liveOrdered = plays.Select(x => x.Item2).OrderByDescending(x => x).ToList();

                int index = 0;
                double totalLocalPP = localOrdered.Sum(play => Math.Pow(0.95, index++) * play);
                double totalLivePP = player.pp;

                index = 0;
                double nonBonusLivePP = liveOrdered.Sum(play => Math.Pow(0.95, index++) * play);

                //todo: implement properly. this is pretty damn wrong.
                var playcountBonusPP = (totalLivePP - nonBonusLivePP);
                totalLocalPP += playcountBonusPP;

                calculatedPlayers.Add(new LeaderboardPlayerInfo
                {
                    LivePP = totalLivePP,
                    LocalPP = totalLocalPP,
                    Username = player.user.username
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
