// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using osu.Game.Scoring;

namespace PerformanceCalculator.Performance
{
    [Command(Name = "performance", Description = "Computes the performance (pp) of replays on a beatmap.")]
    public class PerformanceCommand : ProcessorCommand
    {
        [UsedImplicitly]
        [Required, FileExists]
        [Argument(0, Name = "beatmap", Description = "Required. A beatmap file (.osu) or beatmap ID corresponding to the replays.")]
        public string Beatmap { get; }

        [UsedImplicitly]
        [FileExists]
        [Option(Template = "-r|--replay <file>", Description = "One for each replay. The replay file.")]
        public string[] Replays { get; }

        public override void Execute()
        {
            var workingBeatmap = ProcessorWorkingBeatmap.FromFileOrId(Beatmap);
            var scoreParser = new ProcessorScoreDecoder(workingBeatmap);

            foreach (var f in Replays)
            {
                Score score;
                using (var stream = File.OpenRead(f))
                    score = scoreParser.Parse(stream);

                var ruleset = score.ScoreInfo.Ruleset.CreateInstance();
                var difficultyCalculator = ruleset.CreateDifficultyCalculator(workingBeatmap);
                var difficultyAttributes = difficultyCalculator.Calculate(LegacyHelper.TrimNonDifficultyAdjustmentMods(ruleset, score.ScoreInfo.Mods).ToArray());
                var performanceCalculator = score.ScoreInfo.Ruleset.CreateInstance().CreatePerformanceCalculator(difficultyAttributes, score.ScoreInfo);

                var categoryAttribs = new Dictionary<string, double>();
                double pp = performanceCalculator.Calculate(categoryAttribs);

                Console.WriteLine(f);
                writeAttribute("Player", score.ScoreInfo.User.Username);
                writeAttribute("Mods", score.ScoreInfo.Mods.Length > 0
                    ? score.ScoreInfo.Mods.Select(m => m.Acronym).Aggregate((c, n) => $"{c}, {n}")
                    : "None");

                foreach (var kvp in categoryAttribs)
                    writeAttribute(kvp.Key, kvp.Value.ToString(CultureInfo.InvariantCulture));

                writeAttribute("pp", pp.ToString(CultureInfo.InvariantCulture));
                Console.WriteLine();
            }
        }

        private void writeAttribute(string name, string value) => Console.WriteLine($"{name.PadRight(15)}: {value}");
    }
}
