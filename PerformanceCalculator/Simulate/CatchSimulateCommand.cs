// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace PerformanceCalculator.Simulate
{
    [Command(Name = "catch", Description = "Computes the performance (pp) of a simulated osu!catch play.")]
    public class CatchSimulateCommand : SimulateCommand
    {
        [UsedImplicitly]
        [Required, FileExists]
        [Argument(0, Name = "beatmap", Description = "Required. The beatmap file (.osu).")]
        public override string Beatmap { get; }

        [UsedImplicitly]
        [Option(Template = "-a|--accuracy <accuracy>", Description = "Accuracy. Enter as decimal 0-100. Defaults to 100."
                                                                     + " Scales hit results as well and is rounded to the nearest possible value for the beatmap.")]
        public override double Accuracy { get; } = 100;

        [UsedImplicitly]
        [Option(Template = "-c|--combo <combo>", Description = "Maximum combo during play. Defaults to beatmap maximum.")]
        public override int? Combo { get; }

        [UsedImplicitly]
        [Option(Template = "-C|--percent-combo <combo>", Description = "Percentage of beatmap maximum combo achieved. Alternative to combo option."
                                                                       + " Enter as decimal 0-100.")]
        public override double PercentCombo { get; } = 100;

        [UsedImplicitly]
        [Option(CommandOptionType.MultipleValue, Template = "-m|--mod <mod>", Description = "One for each mod. The mods to compute the performance with."
                                                                                            + " Values: hr, dt, hd, fl, ez, etc...")]
        public override string[] Mods { get; }

        [UsedImplicitly]
        [Option(Template = "-X|--misses <misses>", Description = "Number of misses. Defaults to 0.")]
        public override int Misses { get; }

        [UsedImplicitly]
        [Option(Template = "-M|--mehs <mehs>", Description = "Number of mehs. Will override accuracy if used. Otherwise is automatically calculated.")]
        public override int? Mehs { get; }

        [UsedImplicitly]
        [Option(Template = "-G|--goods <goods>", Description = "Number of goods. Will override accuracy if used. Otherwise is automatically calculated.")]
        public override int? Goods { get; }

        public override Ruleset Ruleset => new CatchRuleset();

        protected override int GetMaxCombo(IBeatmap beatmap) => beatmap.HitObjects.Count(h => h is Fruit) + beatmap.HitObjects.OfType<JuiceStream>().SelectMany(j => j.NestedHitObjects).Count(h => !(h is TinyDroplet));

        protected override Dictionary<HitResult, int> GenerateHitResults(double accuracy, IBeatmap beatmap, int countMiss, int? countMeh, int? countGood)
        {
            var maxCombo = GetMaxCombo(beatmap);
            int maxDroplets = beatmap.HitObjects.OfType<JuiceStream>().Sum(s => s.NestedHitObjects.OfType<TinyDroplet>().Count());
            int maxDrops = beatmap.HitObjects.OfType<JuiceStream>().Sum(s => s.NestedHitObjects.OfType<Droplet>().Count()) - maxDroplets;

            int fruits = beatmap.HitObjects.OfType<Fruit>().Count();
            int repeatCounts = beatmap.HitObjects.OfType<JuiceStream>().Sum(s => s.RepeatCount);
            int juiceStreams = 2 * beatmap.HitObjects.OfType<JuiceStream>().Count();

            int maxFruits = fruits + repeatCounts + juiceStreams;

            // Either given or max value minus misses
            int countDrops = countGood ?? Math.Max(0, maxDrops - countMiss);

            // Max value minus whatever misses are left. Negative if impossible missCount
            int countFruits = maxFruits - (countMiss - (maxDrops - countDrops));

            // Either given or the max amount of hit objects with respect to accuracy minus the already calculated fruits and drops.
            // Negative if accuracy not feasable with missCount.
            int countDroplets = countMeh ?? (int)Math.Round(accuracy * (maxCombo + maxDroplets)) - countFruits - countDrops;

            // Whatever droplets are left
            int dropMisses = maxDroplets - countDroplets;

            return new Dictionary<HitResult, int>
            {
                { HitResult.Great, countFruits },
                { HitResult.Good, countDrops },
                { HitResult.Ok, dropMisses },
                { HitResult.Meh, countDroplets },
                { HitResult.Miss, countMiss }
            };
        }

        protected override double GetAccuracy(Dictionary<HitResult, int> statistics)
        {
            double hits = statistics[HitResult.Great] + statistics[HitResult.Good] + statistics[HitResult.Meh];
            double total = hits + statistics[HitResult.Miss] + statistics[HitResult.Ok];

            return hits / total;
        }

        protected override void WritePlayInfo(ScoreInfo scoreInfo, IBeatmap beatmap)
        {
            WriteAttribute("ApproachRate", FormattableString.Invariant($"{beatmap.BeatmapInfo.BaseDifficulty.ApproachRate}"));
            WriteAttribute("MaxCombo", FormattableString.Invariant($"{scoreInfo.MaxCombo}"));

            foreach (var statistic in scoreInfo.Statistics)
            {
                WriteAttribute(Enum.GetName(typeof(HitResult), statistic.Key), statistic.Value.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
