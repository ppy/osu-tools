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
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch.Difficulty;
using osu.Game.Rulesets.Mania.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Taiko.Difficulty;

namespace PerformanceCalculator.Difficulty
{
    [Command(Name = "difficulty", Description = "Computes the difficulty of a beatmap.")]
    public class DifficultyCommand : ProcessorCommand
    {
        [UsedImplicitly]
        [Required, FileOrDirectoryExists]
        [Argument(0, Name = "path", Description = "Required. A beatmap file (.osu), or a folder containing .osu files to compute the difficulty for.")]
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
            var results = new List<Result>();
            var errors = new List<string>();

            if (Directory.Exists(Path))
            {
                foreach (string file in Directory.GetFiles(Path, "*.osu", SearchOption.AllDirectories))
                {
                    try
                    {
                        var beatmap = new ProcessorWorkingBeatmap(file);
                        results.Add(processBeatmap(beatmap));
                    }
                    catch (Exception e)
                    {
                        errors.Add($"Processing beatmap \"{file}\" failed:\n{e.Message}");
                    }
                }
            }
            else
                results.Add(processBeatmap(new ProcessorWorkingBeatmap(Path)));

            var document = new Document();

            foreach (var error in errors)
                document.Children.Add(new Span(error), "\n");

            if (errors.Any())
                document.Children.Add("\n");

            foreach (var group in results.GroupBy(r => r.RulesetId))
            {
                var ruleset = LegacyHelper.GetRulesetFromLegacyID(group.First().RulesetId);

                document.Children.Add(new Span($"Ruleset: {ruleset.ShortName}"), "\n");

                var grid = new Grid();

                grid.Columns.Add(GridLength.Auto, GridLength.Auto);
                grid.Children.Add(new Cell("beatmap"), new Cell("star rating"));

                foreach (var attribute in group.First().AttributeData)
                {
                    grid.Columns.Add(GridLength.Auto);
                    grid.Children.Add(new Cell(attribute.name));
                }

                foreach (var result in group)
                {
                    grid.Children.Add(new Cell(result.Beatmap), new Cell(result.Stars) { Align = Align.Right });
                    foreach (var attribute in result.AttributeData)
                        grid.Children.Add(new Cell(attribute.value) { Align = Align.Right });
                }

                document.Children.Add(grid);

                document.Children.Add("\n");
            }

            OutputDocument(document);
        }

        private Result processBeatmap(WorkingBeatmap beatmap)
        {
            // Get the ruleset
            var ruleset = LegacyHelper.GetRulesetFromLegacyID(Ruleset ?? beatmap.BeatmapInfo.RulesetID);
            var attributes = ruleset.CreateDifficultyCalculator(beatmap).Calculate(LegacyHelper.TrimNonDifficultyAdjustmentMods(ruleset, getMods(ruleset).ToArray()));

            var result = new Result
            {
                RulesetId = ruleset.RulesetInfo.ID ?? 0,
                Beatmap = $"{beatmap.BeatmapInfo.OnlineBeatmapID} - {beatmap.BeatmapInfo}",
                Stars = attributes.StarRating.ToString("N2")
            };

            switch (attributes)
            {
                case OsuDifficultyAttributes osu:
                    result.AttributeData = new List<(string, object)>
                    {
                        ("aim rating", osu.AimStrain.ToString("N2")),
                        ("speed rating", osu.SpeedStrain.ToString("N2")),
                        ("max combo", osu.MaxCombo),
                        ("approach rate", osu.ApproachRate.ToString("N2")),
                        ("overall difficulty", osu.OverallDifficulty.ToString("N2"))
                    };

                    break;

                case TaikoDifficultyAttributes taiko:
                    result.AttributeData = new List<(string, object)>
                    {
                        ("hit window", taiko.GreatHitWindow.ToString("N2")),
                        ("max combo", taiko.MaxCombo)
                    };

                    break;

                case CatchDifficultyAttributes @catch:
                    result.AttributeData = new List<(string, object)>
                    {
                        ("max combo", @catch.MaxCombo),
                        ("approach rate", @catch.ApproachRate.ToString("N2"))
                    };

                    break;

                case ManiaDifficultyAttributes mania:
                    result.AttributeData = new List<(string, object)>
                    {
                        ("hit window", mania.GreatHitWindow.ToString("N2"))
                    };

                    break;
            }

            return result;
        }

        private List<Mod> getMods(Ruleset ruleset)
        {
            var mods = new List<Mod>();
            if (Mods == null)
                return mods;

            var availableMods = ruleset.CreateAllMods().ToList();

            foreach (var modString in Mods)
            {
                Mod newMod = availableMods.FirstOrDefault(m => string.Equals(m.Acronym, modString, StringComparison.CurrentCultureIgnoreCase));
                if (newMod == null)
                    throw new ArgumentException($"Invalid mod provided: {modString}");

                mods.Add(newMod);
            }

            return mods;
        }

        private struct Result
        {
            public int RulesetId;
            public string Beatmap;
            public string Stars;
            public List<(string name, object value)> AttributeData;
        }
    }
}
