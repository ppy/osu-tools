// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using System.Collections.Generic;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Scoring;

namespace PerformanceCalculator.Performance
{
    public class PerformanceProcessor : IProcessor
    {
        private readonly PerformanceCommand command;

        public PerformanceProcessor(PerformanceCommand command)
        {
            this.command = command;
        }

        public void Execute()
        {
            var workingBeatmap = new ProcessorWorkingBeatmap(command.Beatmap);
            var scoreParser = new ProcessorScoreParser(workingBeatmap);

            foreach (var f in command.Replays)
            {
                Score score;
                using (var stream = File.OpenRead(f))
                    score = scoreParser.Parse(stream);

                workingBeatmap.Mods.Value = score.Mods;

                // Convert + process beatmap
                IBeatmap converted = workingBeatmap.GetPlayableBeatmap(score.Ruleset);

                var categoryAttribs = new Dictionary<string, double>();
                double pp = score.Ruleset.CreateInstance().CreatePerformanceCalculator(converted, score).Calculate(categoryAttribs);

                command.Console.WriteLine(f);
                command.Console.WriteLine($"{"Player".PadRight(15)}: {score.User.Username}");
                command.Console.WriteLine(score.Mods.Length > 0
                    ? $"{"Mods".PadRight(15)}: {score.Mods.Select(m => m.ShortenedName).Aggregate((c, n) => $"{c}, {n}")}"
                    : $"{"Mods".PadRight(15)}: None");
                foreach (var kvp in categoryAttribs)
                    command.Console.WriteLine($"{kvp.Key.PadRight(15)}: {kvp.Value}");
                command.Console.WriteLine($"{"pp".PadRight(15)}: {pp}");
                command.Console.WriteLine();
            }
        }
    }
}
