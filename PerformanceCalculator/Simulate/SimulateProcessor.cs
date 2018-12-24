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
            var statistics = generateHitResults(accuracy, beatmap, command.Misses ?? 0);
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

            command.Console.WriteLine(workingBeatmap.BeatmapInfo.ToString());

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

        private Dictionary<HitResult, int> generateHitResults(double accuracy, IBeatmap beatmap, int amountMiss)
        {
            var totalHitObjects = beatmap.HitObjects.Count();

            // Let Great=6, Good=2, Meh=1, Miss=0. The total should be this.
            var targetTotal = (int) Math.Round(accuracy*totalHitObjects*6);

            // Start by assuming every non miss is a meh
            // This is how much increase is needed by greats and goods
            var delta = targetTotal - (totalHitObjects - amountMiss);

            // Each great increases total by 5 (great-meh=5)
            var amountGreat = delta / 5;
            // Each good increases total by 1 (good-meh=1). Covers remaining difference.
            var amountGood = delta % 5;
            // Mehs are left over. Could be negative if impossible value of amountMiss chosen
            var amountMeh = totalHitObjects - amountGreat - amountGood - amountMiss;


            return new Dictionary<HitResult, int>()
            {
                {HitResult.Great, amountGreat},
                {HitResult.Good, amountGood},
                {HitResult.Meh, amountMeh},
                {HitResult.Miss, amountMiss}
            };
        }

        private void writeAttribute(string name, string value) => command.Console.WriteLine($"{name.PadRight(15)}: {value}");
    }
}
