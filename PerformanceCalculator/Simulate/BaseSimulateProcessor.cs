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
using osu.Game.Scoring;

namespace PerformanceCalculator.Simulate
{
    public abstract class BaseSimulateProcessor : IProcessor
    {
        private readonly BaseSimulateCommand command;

        protected BaseSimulateProcessor(BaseSimulateCommand command)
        {
            this.command = command;
        }

        public void Execute()
        {
            var ruleset = command.Ruleset;

            var mods = getMods(ruleset).ToArray();

            var workingBeatmap = new ProcessorWorkingBeatmap(command.Beatmap);
            workingBeatmap.Mods.Value = mods;

            var beatmap = workingBeatmap.GetPlayableBeatmap(ruleset.RulesetInfo);

            var accuracy = command.Accuracy / 100;
            var beatmapMaxCombo = GetMaxCombo(beatmap);
            var maxCombo = command.Combo ?? (int)Math.Round(command.PercentCombo / 100 * beatmapMaxCombo);
            var statistics = GenerateHitResults(accuracy, beatmap, command.Misses);
            var score = command.Score;

            var scoreInfo = new ScoreInfo
            {
                Accuracy = accuracy,
                MaxCombo = maxCombo,
                Statistics = statistics,
                Mods = mods,
                TotalScore = score
            };

            var categoryAttribs = new Dictionary<string, double>();
            double pp = ruleset.CreatePerformanceCalculator(workingBeatmap, scoreInfo).Calculate(categoryAttribs);

            command.Console.WriteLine(workingBeatmap.BeatmapInfo.ToString());

            WritePlayInfo(scoreInfo, beatmap);

            WriteAttribute("Mods", mods.Length > 0
                ? mods.Select(m => m.Acronym).Aggregate((c, n) => $"{c}, {n}")
                : "None");

            foreach (var kvp in categoryAttribs)
                WriteAttribute(kvp.Key, kvp.Value.ToString(CultureInfo.InvariantCulture));

            WriteAttribute("pp", pp.ToString(CultureInfo.InvariantCulture));
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

        protected abstract void WritePlayInfo(ScoreInfo scoreInfo, IBeatmap beatmap);

        protected abstract int GetMaxCombo(IBeatmap beatmap);

        protected abstract Dictionary<HitResult, int> GenerateHitResults(double accuracy, IBeatmap beatmap, int amountMiss);

        protected void WriteAttribute(string name, string value) => command.Console.WriteLine($"{name.PadRight(15)}: {value}");
    }
}
