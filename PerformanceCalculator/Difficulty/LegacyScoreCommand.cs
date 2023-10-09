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
using osu.Game.Beatmaps;
using osu.Game.Online.API;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring.Legacy;

namespace PerformanceCalculator.Difficulty
{
    [Command(Name = "legacy-score", Description = "Computes the legacy scoring attributes of a beatmap.")]
    public class LegacyScoreCommand : ProcessorCommand
    {
        [UsedImplicitly]
        [Required]
        [Argument(0, Name = "path", Description = "Required. A beatmap file (.osu), beatmap ID, or a folder containing .osu files to compute the difficulty for.")]
        public string Path { get; }

        [UsedImplicitly]
        [Option(CommandOptionType.SingleOrNoValue, Template = "-r|--ruleset:<ruleset-id>", Description = "Optional. The ruleset to compute the beatmap difficulty for, if it's a convertible beatmap.\n"
                                                                                                         + "Values: 0 - osu!, 1 - osu!taiko, 2 - osu!catch, 3 - osu!mania")]
        [AllowedValues("0", "1", "2", "3")]
        public int? Ruleset { get; }

        [UsedImplicitly]
        [Option(CommandOptionType.MultipleValue, Template = "-m|--m <mod>", Description = "One for each mod. The mods to compute the difficulty with."
                                                                                          + "Values: hr, dt, hd, fl, ez, 4k, 5k, etc...")]
        public string[] Mods { get; }

        public override void Execute()
        {
            var resultSet = new ResultSet();

            if (Directory.Exists(Path))
            {
                foreach (string file in Directory.GetFiles(Path, "*.osu", SearchOption.AllDirectories))
                {
                    try
                    {
                        var beatmap = new ProcessorWorkingBeatmap(file);
                        resultSet.Results.Add(processBeatmap(beatmap));
                    }
                    catch (Exception e)
                    {
                        resultSet.Errors.Add($"Processing beatmap \"{file}\" failed:\n{e.Message}");
                    }
                }
            }
            else
                resultSet.Results.Add(processBeatmap(ProcessorWorkingBeatmap.FromFileOrId(Path)));

            if (OutputJson)
            {
                string json = JsonConvert.SerializeObject(resultSet);

                Console.WriteLine(json);

                if (OutputFile != null)
                    File.WriteAllText(OutputFile, json);
            }
            else
            {
                var document = new Document();

                foreach (var error in resultSet.Errors)
                    document.Children.Add(new Span(error), "\n");
                if (resultSet.Errors.Count > 0)
                    document.Children.Add("\n");

                foreach (var group in resultSet.Results.GroupBy(r => r.RulesetId))
                {
                    var ruleset = LegacyHelper.GetRulesetFromLegacyID(group.First().RulesetId);
                    document.Children.Add(new Span($"ruleset: {ruleset.ShortName}"), "\n");

                    Grid grid = new Grid();
                    bool firstResult = true;

                    foreach (var result in group)
                    {
                        // Headers
                        if (firstResult)
                        {
                            foreach (var column in new[] { "Beatmap", "Mods", "Accuracy score", "Combo score", "Bonus score ratio", "Mod multiplier" })
                            {
                                grid.Columns.Add(GridLength.Auto);
                                grid.Children.Add(new Cell(column));
                            }
                        }

                        // Values
                        grid.Children.Add(new Cell($"{result.BeatmapId} - {result.Beatmap}"));
                        grid.Children.Add(new Cell(string.Join(", ", result.Mods.Select(mod => mod.Acronym))));
                        grid.Children.Add(new Cell($"{result.ScoreAttributes.AccuracyScore:N0}") { Align = Align.Right });
                        grid.Children.Add(new Cell($"{result.ScoreAttributes.ComboScore:N0}") { Align = Align.Right });
                        grid.Children.Add(new Cell($"{result.ScoreAttributes.BonusScoreRatio:N6}") { Align = Align.Right });
                        grid.Children.Add(new Cell($"{result.ModMultiplier:N4}") { Align = Align.Right });

                        firstResult = false;
                    }

                    document.Children.Add(grid, "\n");
                }

                OutputDocument(document);
            }
        }

        private Result processBeatmap(WorkingBeatmap beatmap)
        {
            // Get the ruleset
            var ruleset = LegacyHelper.GetRulesetFromLegacyID(Ruleset ?? beatmap.BeatmapInfo.Ruleset.OnlineID);

            // bit of a hack to discard non-legacy mods.
            var mods = ruleset.ConvertFromLegacyMods(ruleset.ConvertToLegacyMods(getMods(ruleset))).ToList();

            var legacyRuleset = (ILegacyRuleset)ruleset;
            var simulator = legacyRuleset.CreateLegacyScoreSimulator();
            var playableBeatmap = beatmap.GetPlayableBeatmap(ruleset.RulesetInfo, mods);
            var attributes = simulator.Simulate(beatmap, playableBeatmap);

            var conversionInfo = LegacyBeatmapConversionDifficultyInfo.FromBeatmap(playableBeatmap);
            var modMultiplier = simulator.GetLegacyScoreMultiplier(mods, conversionInfo);

            return new Result
            {
                RulesetId = ruleset.RulesetInfo.OnlineID,
                BeatmapId = beatmap.BeatmapInfo.OnlineID,
                Beatmap = beatmap.BeatmapInfo.ToString(),
                Mods = mods.Select(m => new APIMod(m)).ToList(),
                ScoreAttributes = attributes,
                ModMultiplier = modMultiplier
            };
        }

        private Mod[] getMods(Ruleset ruleset)
        {
            var mods = new List<Mod>();
            if (Mods == null)
                return Array.Empty<Mod>();

            var availableMods = ruleset.CreateAllMods().ToList();

            foreach (var modString in Mods)
            {
                Mod newMod = availableMods.FirstOrDefault(m => string.Equals(m.Acronym, modString, StringComparison.CurrentCultureIgnoreCase));
                if (newMod == null)
                    throw new ArgumentException($"Invalid mod provided: {modString}");

                mods.Add(newMod);
            }

            return mods.ToArray();
        }

        private class ResultSet
        {
            [JsonProperty("errors")]
            public List<string> Errors { get; set; } = new List<string>();

            [JsonProperty("results")]
            public List<Result> Results { get; set; } = new List<Result>();
        }

        private class Result
        {
            [JsonProperty("ruleset_id")]
            public int RulesetId { get; set; }

            [JsonProperty("beatmap_id")]
            public int BeatmapId { get; set; }

            [JsonProperty("beatmap")]
            public string Beatmap { get; set; }

            [JsonProperty("mods")]
            public List<APIMod> Mods { get; set; }

            [JsonProperty("score_attributes")]
            public LegacyScoreAttributes ScoreAttributes { get; set; }

            [JsonProperty("mod_multiplier")]
            public double ModMultiplier { get; set; }
        }
    }
}
