// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Humanizer;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using osu.Game.Rulesets.Mods;
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

                Mod[] mods = score.ScoreInfo.Mods;
                if (score.ScoreInfo.IsLegacyScore)
                    mods = LegacyHelper.ConvertToLegacyDifficultyAdjustmentMods(ruleset, mods);

                var difficultyAttributes = difficultyCalculator.Calculate(mods);
                var performanceCalculator = score.ScoreInfo.Ruleset.CreateInstance().CreatePerformanceCalculator();

                var ppAttributes = performanceCalculator?.Calculate(score.ScoreInfo, difficultyAttributes);

                Console.WriteLine(f);
                writeAttribute("Player", score.ScoreInfo.User.Username);
                writeAttribute("Mods", score.ScoreInfo.Mods.Length > 0
                    ? score.ScoreInfo.Mods.Select(m => m.Acronym).Aggregate((c, n) => $"{c}, {n}")
                    : "None");

                var ppAttributeValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(ppAttributes)) ?? new Dictionary<string, object>();
                foreach (var attrib in ppAttributeValues)
                    writeAttribute(attrib.Key.Humanize(), FormattableString.Invariant($"{attrib.Value:N2}"));

                Console.WriteLine();
            }
        }

        private void writeAttribute(string name, string value) => Console.WriteLine($"{name,-15}: {value}");
    }
}
