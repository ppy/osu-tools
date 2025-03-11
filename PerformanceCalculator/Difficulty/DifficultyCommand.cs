﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Alba.CsConsoleFormat;
using Humanizer;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using osu.Game.Beatmaps;
using osu.Game.Online.API;
using osu.Game.Rulesets.Difficulty;

namespace PerformanceCalculator.Difficulty
{
    [Command(Name = "difficulty", Description = "Computes the difficulty of a beatmap.")]
    public class DifficultyCommand : ProcessorCommand
    {
        [UsedImplicitly]
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

        [UsedImplicitly]
        [Option(CommandOptionType.MultipleValue, Template = "-o|--mod-option <option>",
            Description = "The options of mods, with one for each setting. Specified as acryonym_settingkey=value. Example: DT_speed_change=1.35")]
        public string[] ModOptions { get; set; } = [];

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
            }
            else
            {
                var document = new Document();

                foreach (string error in resultSet.Errors)
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
                        var attributeValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(result.Attributes)) ?? new Dictionary<string, object>();

                        // Headers
                        if (firstResult)
                        {
                            grid.Columns.Add(GridLength.Auto);
                            grid.Children.Add(new Cell("beatmap"));

                            foreach (var column in attributeValues)
                            {
                                grid.Columns.Add(GridLength.Auto);
                                grid.Children.Add(new Cell(column.Key.Humanize()));
                            }
                        }

                        // Values
                        grid.Children.Add(new Cell($"{result.BeatmapId} - {result.Beatmap}"));
                        foreach (var column in attributeValues)
                            grid.Children.Add(new Cell($"{column.Value:N2}") { Align = Align.Right });

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
            var mods = ParseMods(ruleset, Mods, ModOptions);
            var attributes = ruleset.CreateDifficultyCalculator(beatmap).Calculate(mods);

            return new Result
            {
                RulesetId = ruleset.RulesetInfo.OnlineID,
                BeatmapId = beatmap.BeatmapInfo.OnlineID,
                Beatmap = beatmap.BeatmapInfo.ToString(),
                Mods = mods.Select(m => new APIMod(m)).ToList(),
                Attributes = attributes
            };
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

            [JsonProperty("attributes")]
            public DifficultyAttributes Attributes { get; set; }
        }
    }
}
