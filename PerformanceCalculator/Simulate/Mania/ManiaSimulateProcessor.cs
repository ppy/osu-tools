// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace PerformanceCalculator.Simulate.Mania
{
    public class ManiaSimulateProcessor : IProcessor
    {
        private readonly ManiaSimulateCommand command;

        public ManiaSimulateProcessor(ManiaSimulateCommand command)
        {
            this.command = command;
        }

        public void Execute()
        {
            var ruleset = new ManiaRuleset();

            var mods = getMods(ruleset).ToArray();

            var workingBeatmap = new ProcessorWorkingBeatmap(command.Beatmap);
            workingBeatmap.Mods.Value = mods;

            var beatmap = workingBeatmap.GetPlayableBeatmap(ruleset.RulesetInfo);

            var score = command.Score;
            var statistics = generateHitResults(beatmap);

            var scoreInfo = new ScoreInfo()
            {
                TotalScore = score,
                Statistics = statistics,
                Mods = mods
            };

            var categoryAttribs = new Dictionary<string, double>();
            double pp = ruleset.CreatePerformanceCalculator(workingBeatmap, scoreInfo).Calculate(categoryAttribs);

            command.Console.WriteLine(workingBeatmap.BeatmapInfo.ToString());

            writeAttribute("Score", score.ToString(CultureInfo.InvariantCulture));

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

        private Dictionary<HitResult, int> generateHitResults(IBeatmap beatmap)
        {
            var totalHits = beatmap.HitObjects.Count();

            // Only total number of hits is considered currently, so specifics don't matter
            return new Dictionary<HitResult, int>()
            {
                {HitResult.Perfect, totalHits},
                {HitResult.Great, 0},
                {HitResult.Ok, 0},
                {HitResult.Good, 0},
                {HitResult.Meh, 0},
                {HitResult.Miss, 0}
            };
        }

        private void writeAttribute(string name, string value) => command.Console.WriteLine($"{name.PadRight(15)}: {value}");
    }
}
