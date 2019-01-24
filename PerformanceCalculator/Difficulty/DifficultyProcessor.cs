// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
    public class DifficultyProcessor : IProcessor
    {
        private readonly DifficultyCommand command;

        public DifficultyProcessor(DifficultyCommand command)
        {
            this.command = command;
        }

        public void Execute()
        {
            if (Directory.Exists(command.Path))
            {
                foreach (string file in Directory.GetFiles(command.Path, "*.osu", SearchOption.AllDirectories))
                {
                    var beatmap = new ProcessorWorkingBeatmap(file);
                    command.Console.WriteLine(beatmap.BeatmapInfo.ToString());

                    processBeatmap(beatmap);
                }
            }
            else
                processBeatmap(new ProcessorWorkingBeatmap(command.Path));
        }

        private void processBeatmap(WorkingBeatmap beatmap)
        {
            // Get the ruleset
            var ruleset = LegacyHelper.GetRulesetFromLegacyID(command.Ruleset ?? beatmap.BeatmapInfo.RulesetID);
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

            command.Console.WriteLine();
        }

        private void writeAttribute(string name, string value) => command.Console.WriteLine($"{name.PadRight(15)}: {value}");

        private List<Mod> getMods(Ruleset ruleset)
        {
            var mods = new List<Mod>();
            if (command.Mods == null)
                return mods;

            var availableMods = ruleset.GetAllMods().ToList();
            foreach (var modString in command.Mods)
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
