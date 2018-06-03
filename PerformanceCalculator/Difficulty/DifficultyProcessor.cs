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
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Taiko;

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
            var ruleset = getRuleset(command.Ruleset ?? beatmap.BeatmapInfo.RulesetID);

            // Create beatmap
            beatmap.Mods.Value = getMods(ruleset);

            // Convert + process beatmap
            IBeatmap converted = beatmap.GetPlayableBeatmap(ruleset.RulesetInfo);

            var categoryAttribs = new Dictionary<string, object>();
            double stars = ruleset.CreateDifficultyCalculator(converted, beatmap.Mods.Value.ToArray()).Calculate(categoryAttribs);

            writeAttribute("Ruleset", ruleset.ShortName);

            foreach (var kvp in categoryAttribs)
            {
                switch (kvp.Value)
                {
                    case double d:
                        writeAttribute(kvp.Key, d.ToString(CultureInfo.InvariantCulture));
                        break;
                    case int i:
                        writeAttribute(kvp.Key, i.ToString(CultureInfo.InvariantCulture));
                        break;
                    default:
                        writeAttribute(kvp.Key, kvp.Value.ToString());
                        break;
                }
            }

            writeAttribute("stars", stars.ToString(CultureInfo.InvariantCulture));
            command.Console.WriteLine();
        }

        private void writeAttribute(string name, string value) => command.Console.WriteLine($"{name.PadRight(15)}: {value}");

        private Ruleset getRuleset(int rulesetId)
        {
            switch (rulesetId)
            {
                default:
                    throw new ArgumentException("Invalid ruleset id provided.");
                case 0:
                    return new OsuRuleset();
                case 1:
                    return new TaikoRuleset();
                case 2:
                    return new CatchRuleset();
                case 3:
                    return new ManiaRuleset();
            }
        }

        private List<Mod> getMods(Ruleset ruleset)
        {
            var mods = new List<Mod>();
            if (command.Mods == null)
                return mods;

            var availableMods = ruleset.GetAllMods().ToList();
            foreach (var modString in command.Mods)
            {
                Mod newMod = availableMods.FirstOrDefault(m => string.Equals(m.ShortenedName, modString, StringComparison.CurrentCultureIgnoreCase));
                if (newMod == null)
                    throw new ArgumentException($"Invalid mod provided: {modString}");
                mods.Add(newMod);
            }

            return mods;
        }
    }
}
