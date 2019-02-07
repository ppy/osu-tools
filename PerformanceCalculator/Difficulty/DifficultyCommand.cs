// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
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
            var document = new Document();
            if (Directory.Exists(Path))
            {
                foreach (string file in Directory.GetFiles(Path, "*.osu", SearchOption.AllDirectories))
                {
                    var beatmap = new ProcessorWorkingBeatmap(file);
                    document.Children.Add(new Span(beatmap.BeatmapInfo.ToString()), "\n");

                    processBeatmap(beatmap, document);
                }
            }
            else
            {
                var beatmap = new ProcessorWorkingBeatmap(Path);
                document.Children.Add(new Span(beatmap.BeatmapInfo.ToString()), "\n");

                processBeatmap(beatmap, document);
            }
            OutputDocument(document);
        }

        private void processBeatmap(WorkingBeatmap beatmap, Document document)
        {
            // Get the ruleset
            var ruleset = LegacyHelper.GetRulesetFromLegacyID(Ruleset ?? beatmap.BeatmapInfo.RulesetID);
            var attributes = ruleset.CreateDifficultyCalculator(beatmap).Calculate(getMods(ruleset).ToArray());

            document.Children.Add(new Span("Ruleset".PadRight(15) + $": {ruleset.ShortName}"), "\n");
            document.Children.Add(new Span("Stars".PadRight(15) + $": {attributes.StarRating.ToString(CultureInfo.InvariantCulture)}"), "\n");

            switch (attributes)
            {
                case OsuDifficultyAttributes osu:
                    document.Children.Add(new Span("Aim".PadRight(15) + $": {osu.AimStrain.ToString(CultureInfo.InvariantCulture)}"), "\n");
                    document.Children.Add(new Span("Speed".PadRight(15) + $": {osu.SpeedStrain.ToString(CultureInfo.InvariantCulture)}"), "\n");
                    document.Children.Add(new Span("MaxCombo".PadRight(15) + $": {osu.MaxCombo.ToString(CultureInfo.InvariantCulture)}"), "\n");
                    document.Children.Add(new Span("AR".PadRight(15) + $": {osu.ApproachRate.ToString(CultureInfo.InvariantCulture)}"), "\n");
                    document.Children.Add(new Span("OD".PadRight(15) + $": {osu.OverallDifficulty.ToString(CultureInfo.InvariantCulture)}"), "\n");
                    break;
                case TaikoDifficultyAttributes taiko:
                    document.Children.Add(new Span("HitWindow".PadRight(15) + $": {taiko.GreatHitWindow.ToString(CultureInfo.InvariantCulture)}"), "\n");
                    document.Children.Add(new Span("MaxCombo".PadRight(15) + $": {taiko.MaxCombo.ToString(CultureInfo.InvariantCulture)}"), "\n");
                    break;
                case CatchDifficultyAttributes c:
                    document.Children.Add(new Span("MaxCombo".PadRight(15) + $": {c.MaxCombo.ToString(CultureInfo.InvariantCulture)}"), "\n");
                    document.Children.Add(new Span("AR".PadRight(15) + $": {c.ApproachRate.ToString(CultureInfo.InvariantCulture)}"), "\n");
                    break;
                case ManiaDifficultyAttributes mania:
                    document.Children.Add(new Span("HitWindow".PadRight(15) + $": {mania.GreatHitWindow.ToString(CultureInfo.InvariantCulture)}"), "\n");
                    break;
            }
        }

        private void writeAttribute(string name, string value) => Console.WriteLine($"{name.PadRight(15)}: {value}");

        private List<Mod> getMods(Ruleset ruleset)
        {
            var mods = new List<Mod>();
            if (Mods == null)
                return mods;

            var availableMods = ruleset.GetAllMods().ToList();
            foreach (var modString in Mods)
            {
                Mod newMod = availableMods.FirstOrDefault(m => string.Equals(m.Acronym, modString, StringComparison.CurrentCultureIgnoreCase));
                if (newMod == null)
                    throw new ArgumentException($"Invalid mod provided: {modString}");
                mods.Add(newMod);
            }

            return mods;
        }
    }
}
