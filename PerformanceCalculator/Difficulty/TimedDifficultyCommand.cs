// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
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
    [Command(Name = "timed", Description = "Computes the timed difficulty attributes of a beatmap.")]
    public class TimedDifficultyCommand : ProcessorCommand
    {
        [UsedImplicitly]
        [Argument(0, Name = "path", Description = "Required. A beatmap file (.osu), or a beatmap ID.")]
        public string Path { get; } = null!;

        [UsedImplicitly]
        [Option(CommandOptionType.SingleOrNoValue, Template = "-r|--ruleset:<ruleset-id>", Description = "Optional. The ruleset to compute the beatmap difficulty for, if it's a convertible beatmap.\n"
                                                                                                         + "Values: 0 - osu!, 1 - osu!taiko, 2 - osu!catch, 3 - osu!mania")]
        [AllowedValues("0", "1", "2", "3")]
        public int? Ruleset { get; }

        [UsedImplicitly]
        [Option(CommandOptionType.MultipleValue, Template = "-m|--m <mod>", Description = "One for each mod. The mods to compute the difficulty with."
                                                                                          + "Values: hr, dt, hd, fl, ez, 4k, 5k, etc...")]
        public string[] Mods { get; } = [];

        [UsedImplicitly]
        [Option(CommandOptionType.MultipleValue, Template = "-o|--mod-option <option>",
            Description = "The options of mods, with one for each setting. Specified as acryonym_settingkey=value. Example: DT_speed_change=1.35")]
        public string[] ModOptions { get; } = [];

        public override void Execute()
        {
            var result = processBeatmap(ProcessorWorkingBeatmap.FromFileOrId(Path));

            var ruleset = LegacyHelper.GetRulesetFromLegacyID(result.RulesetId);

            if (OutputJson)
            {
                string json = JsonConvert.SerializeObject(result);

                Console.WriteLine(json);
            }
            else
            {
                var document = new Document();
                document.Children.Add(new Span($"ruleset: {ruleset.ShortName}, beatmap: {result.Beatmap}"), "\n");

                Grid grid = new Grid();
                bool firstResult = true;

                foreach (var timedAttribute in result.Attributes)
                {
                    double time = timedAttribute.Time;

                    // Headers
                    var attributeValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(timedAttribute.Attributes)) ?? new Dictionary<string, object>();

                    if (firstResult)
                    {
                        grid.Columns.Add(GridLength.Auto);
                        grid.Children.Add(new Cell("Time"));

                        foreach (var column in attributeValues)
                        {
                            grid.Columns.Add(GridLength.Auto);
                            grid.Children.Add(new Cell(column.Key.Humanize()));
                        }
                    }

                    grid.Children.Add(new Cell($"{time:N0}"));

                    // Values
                    foreach (var column in attributeValues)
                        grid.Children.Add(new Cell($"{column.Value:N2}") { Align = Align.Right });

                    firstResult = false;
                }

                document.Children.Add(grid, "\n");

                OutputDocument(document);
            }
        }

        private Result processBeatmap(WorkingBeatmap beatmap)
        {
            // Get the ruleset
            var ruleset = LegacyHelper.GetRulesetFromLegacyID(Ruleset ?? beatmap.BeatmapInfo.Ruleset.OnlineID);
            var mods = ParseMods(ruleset, Mods, ModOptions);
            var attributes = ruleset.CreateDifficultyCalculator(beatmap).CalculateTimed(mods);

            return new Result
            {
                RulesetId = ruleset.RulesetInfo.OnlineID,
                BeatmapId = beatmap.BeatmapInfo.OnlineID,
                Beatmap = beatmap.BeatmapInfo.ToString(),
                Mods = mods.Select(m => new APIMod(m)).ToList(),
                Attributes = attributes
            };
        }

        private class Result
        {
            [JsonProperty("ruleset_id")]
            public required int RulesetId { get; set; }

            [JsonProperty("beatmap_id")]
            public required int BeatmapId { get; set; }

            [JsonProperty("beatmap")]
            public required string Beatmap { get; set; }

            [JsonProperty("mods")]
            public required List<APIMod> Mods { get; set; }

            [JsonProperty("timed_attributes")]
            public required IEnumerable<TimedDifficultyAttributes> Attributes { get; set; }
        }
    }
}
