// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Scoring;

namespace PerformanceCalculator.Simulate.Taiko
{
    public class TaikoSimulateProcessor : IProcessor
    {
        private readonly TaikoSimulateCommand command;

        public TaikoSimulateProcessor(TaikoSimulateCommand command)
        {
            this.command = command;
        }

        public void Execute()
        {
            var ruleset = new TaikoRuleset();

            var mods = getMods(ruleset).ToArray();

            var workingBeatmap = new ProcessorWorkingBeatmap(command.Beatmap);
            workingBeatmap.Mods.Value = mods;

            var beatmap = workingBeatmap.GetPlayableBeatmap(ruleset.RulesetInfo);

            var accuracy = command.Accuracy/100 ?? 1.0;
            var beatmapMaxCombo = beatmap.HitObjects.OfType<Hit>().Count();
            var maxCombo = command.Combo ??
                           (int) Math.Round((command.PercentCombo ?? 100)/100 * beatmapMaxCombo);
            var statistics = generateHitResults(beatmap, command.Misses ?? 0);

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

            writeAttribute("Accuracy", (accuracy*100).ToString(CultureInfo.InvariantCulture) + "%");
            writeAttribute("Combo", FormattableString.Invariant($"{maxCombo}/{beatmapMaxCombo} ({Math.Round(100.0 * maxCombo/beatmapMaxCombo, 2)}%)"));
            writeAttribute("Misses", statistics[HitResult.Miss].ToString(CultureInfo.InvariantCulture));

            writeAttribute("Mods", mods.Length > 0
                ? mods.Select(m => m.Acronym).Aggregate((c, n) => $"{c}, {n}")
                : "None");

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

        private Dictionary<HitResult, int> generateHitResults(IBeatmap beatmap, int amountMiss)
        {
            var totalHitObjects = beatmap.HitObjects.Count();

            // Does not need to match acc currently since only total and miss count matters
            var amountGreat = totalHitObjects - amountMiss;

            return new Dictionary<HitResult, int>()
            {
                {HitResult.Great, amountGreat},
                {HitResult.Good, 0},
                {HitResult.Meh, 0},
                {HitResult.Miss, amountMiss}
            };
        }

        private void writeAttribute(string name, string value) => command.Console.WriteLine($"{name.PadRight(15)}: {value}");
    }
}
