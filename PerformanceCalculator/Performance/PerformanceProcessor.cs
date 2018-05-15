// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using System.Collections.Generic;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Scoring;

namespace PerformanceCalculator.Performance
{
    public class PerformanceProcessor : Processor
    {
        private readonly PerformanceCommand command;

        public PerformanceProcessor(PerformanceCommand command)
        {
            this.command = command;
        }

        private Ruleset ruleset;

        protected override void Execute()
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

                if (ruleset == null)
                    ruleset = score.Ruleset.CreateInstance();

                var categoryAttribs = new Dictionary<string, double>();
                double pp = ruleset.CreatePerformanceCalculator(converted, score).Calculate(categoryAttribs);

                command.Console.WriteLine(f);
                command.Console.WriteLine($"{"Player".PadRight(15)}: {score.User.Username}");
                command.Console.WriteLine($"{"Mods".PadRight(15)}: {score.Mods.Select(m => m.ShortenedName).Aggregate((c, n) => $"{c}, {n}")}");
                foreach (var kvp in categoryAttribs)
                    command.Console.WriteLine($"{kvp.Key.PadRight(15)}: {kvp.Value}");
                command.Console.WriteLine($"{"pp".PadRight(15)}: {pp}");
                command.Console.WriteLine();
            }
        }
    }
}
