// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace PerformanceCalculator.Simulate
{
    public class SimulateProcessor : IProcessor
    {
        private readonly SimulateCommand command;

        public SimulateProcessor(SimulateCommand command)
        {
            this.command = command;
        }

        public void Execute()
        {
            var ruleset = new OsuRuleset();
            var workingBeatmap = new ProcessorWorkingBeatmap(command.Beatmap);
            var beatmap = workingBeatmap.GetPlayableBeatmap(ruleset.RulesetInfo);

            var accuracy = command.Accuracy/100 ?? 1.0;
            var maxCombo = command.MaxCombo ?? (beatmap.HitObjects.Count + beatmap.HitObjects.OfType<Slider>().Sum(s => s.NestedHitObjects.Count - 1));
            var statistics = generateHitResults(accuracy, beatmap);
            var mods = getMods(ruleset).ToArray();

            var scoreInfo = new ScoreInfo()
            {
                Accuracy = accuracy,
                MaxCombo = maxCombo,
                Statistics = statistics,
                Mods = mods

            };

            workingBeatmap.Mods.Value = mods;

            var categoryAttribs = new Dictionary<string, double>();
            double pp = ruleset.CreatePerformanceCalculator(workingBeatmap, scoreInfo).Calculate(categoryAttribs);

            foreach (var kvp in categoryAttribs)
                writeAttribute(kvp.Key, kvp.Value.ToString(CultureInfo.InvariantCulture));

            writeAttribute("pp", pp.ToString(CultureInfo.InvariantCulture));
        }

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

        private Dictionary<HitResult, int> generateHitResults(double accuracy, IBeatmap beatmap)
        {
            var amountHitObjects = beatmap.HitObjects.Count();
            var good = (int) Math.Round((1-accuracy) * amountHitObjects * 300/200);
            var great = amountHitObjects - good;
            return new Dictionary<HitResult, int>()
            {
                {HitResult.Great, great},
                {HitResult.Good, good},
                {HitResult.Meh, 0},
                {HitResult.Miss, 0}
            };
        }

        private void writeAttribute(string name, string value) => command.Console.WriteLine($"{name.PadRight(15)}: {value}");
    }
}
