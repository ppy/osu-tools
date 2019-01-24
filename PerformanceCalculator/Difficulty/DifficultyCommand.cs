// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
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
            if (Directory.Exists(Path))
            {
                foreach (string file in Directory.GetFiles(Path, "*.osu", SearchOption.AllDirectories))
                {
                    var beatmap = new ProcessorWorkingBeatmap(file);
                    Console.WriteLine(beatmap.BeatmapInfo.ToString());

                    processBeatmap(beatmap);
                }
            }
            else
                processBeatmap(new ProcessorWorkingBeatmap(Path));
        }

        private void processBeatmap(WorkingBeatmap beatmap)
        {
            // Get the ruleset
            var ruleset = LegacyHelper.GetRulesetFromLegacyID(Ruleset ?? beatmap.BeatmapInfo.RulesetID);
            var attributes = ruleset.CreateDifficultyCalculator(beatmap).Calculate(getMods(ruleset).ToArray());

            writeAttribute("Ruleset", ruleset.ShortName);
            writeAttribute("Stars", attributes.StarRating.ToString(CultureInfo.InvariantCulture));

            switch (attributes)
            {
                case OsuDifficultyAttributes osu:
                    writeAttribute("Aim", osu.AimStrain.ToString(CultureInfo.InvariantCulture));
                    writeAttribute("Speed", osu.SpeedStrain.ToString(CultureInfo.InvariantCulture));
                    writeAttribute("MaxCombo", osu.MaxCombo.ToString(CultureInfo.InvariantCulture));
                    writeAttribute("AR", osu.ApproachRate.ToString(CultureInfo.InvariantCulture));
                    writeAttribute("OD", osu.OverallDifficulty.ToString(CultureInfo.InvariantCulture));
                    break;
                case TaikoDifficultyAttributes taiko:
                    writeAttribute("HitWindow", taiko.GreatHitWindow.ToString(CultureInfo.InvariantCulture));
                    writeAttribute("MaxCombo", taiko.MaxCombo.ToString(CultureInfo.InvariantCulture));
                    break;
                case CatchDifficultyAttributes c:
                    writeAttribute("MaxCombo", c.MaxCombo.ToString(CultureInfo.InvariantCulture));
                    writeAttribute("AR", c.ApproachRate.ToString(CultureInfo.InvariantCulture));
                    break;
                case ManiaDifficultyAttributes mania:
                    writeAttribute("HitWindow", mania.GreatHitWindow.ToString(CultureInfo.InvariantCulture));
                    break;
            }

            Console.WriteLine();
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
