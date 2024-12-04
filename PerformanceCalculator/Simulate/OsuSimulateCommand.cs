// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;

namespace PerformanceCalculator.Simulate
{
    [Command(Name = "osu", Description = "Computes the performance (pp) of a simulated osu! play.")]
    public class OsuSimulateCommand : SimulateCommand
    {
        [UsedImplicitly]
        [Option(Template = "-M|--mehs <mehs>", Description = "Number of mehs. Will override accuracy if used. Otherwise is automatically calculated.")]
        public override int? Mehs { get; }

        [UsedImplicitly]
        [Option(Template = "-G|--goods <goods>", Description = "Number of goods. Will override accuracy if used. Otherwise is automatically calculated.")]
        public override int? Goods { get; }

        [UsedImplicitly]
        [Option(Template = "-c|--combo <combo>", Description = "Maximum combo during play. Defaults to beatmap maximum.")]
        public override int? Combo { get; }

        [UsedImplicitly]
        [Option(Template = "-C|--percent-combo <combo>", Description = "Percentage of beatmap maximum combo achieved. Alternative to combo option. Enter as decimal 0-100.")]
        public override double PercentCombo { get; } = 100;

        [UsedImplicitly]
        [Option(Template = "-L|--large-tick-misses <misses>", Description = "Number of large tick misses. Defaults to 0.")]
        private int largeTickMisses { get; }

        [UsedImplicitly]
        [Option(Template = "-S|--slider-tail-misses <misses>", Description = "Number of slider tail misses. Defaults to 0.")]
        private int sliderTailMisses { get; }

        public override Ruleset Ruleset => new OsuRuleset();

        protected override Dictionary<HitResult, int> GenerateHitResults(IBeatmap beatmap, Mod[] mods)
        {
            // Use lazer info only if score has sliderhead accuracy
            if (mods.OfType<OsuModClassic>().Any(m => m.NoSliderHeadAccuracy.Value))
            {
                return generateHitResults(beatmap, Accuracy / 100, Misses, Mehs, Goods, null, null);
            }
            else
            {
                return generateHitResults(beatmap, Accuracy / 100, Misses, Mehs, Goods, largeTickMisses, sliderTailMisses);
            }
        }

        private static Dictionary<HitResult, int> generateHitResults(IBeatmap beatmap, double accuracy, int countMiss, int? countMeh, int? countGood, int? countLargeTickMisses, int? countSliderTailMisses)
        {
            int countGreat;

            var totalResultCount = beatmap.HitObjects.Count;

            if (countMeh != null || countGood != null)
            {
                countGreat = totalResultCount - (countGood ?? 0) - (countMeh ?? 0) - countMiss;
            }
            else
            {
                // Total result count excluding countMiss
                int relevantResultCount = totalResultCount - countMiss;

                // Accuracy excluding countMiss. We need that because we're trying to achieve target accuracy without touching countMiss
                // So it's better to pretened that there were 0 misses in the 1st place
                double relevantAccuracy = accuracy * totalResultCount / relevantResultCount;

                // Clamp accuracy to account for user trying to break the algorithm by inputting impossible values
                relevantAccuracy = Math.Clamp(relevantAccuracy, 0, 1);

                // Main curve for accuracy > 25%, the closer accuracy is to 25% - the more 50s it adds
                if (relevantAccuracy >= 0.25)
                {
                    // Main curve. Zero 50s if accuracy is 100%, one 50 per 9 100s if accuracy is 75% (excluding misses), 4 50s per 9 100s if accuracy is 50%
                    double ratio50To100 = Math.Pow(1 - (relevantAccuracy - 0.25) / 0.75, 2);

                    // Derived from the formula: Accuracy = (6 * c300 + 2 * c100 + c50) / (6 * totalHits), assuming that c50 = c100 * ratio50to100
                    double count100Estimate = 6 * relevantResultCount * (1 - relevantAccuracy) / (5 * ratio50To100 + 4);

                    // Get count50 according to c50 = c100 * ratio50to100
                    double count50Estimate = count100Estimate * ratio50To100;

                    // Round it to get int number of 100s
                    countGood = (int?)Math.Round(count100Estimate);

                    // Get number of 50s as difference between total mistimed hits and count100
                    countMeh = (int?)(Math.Round(count100Estimate + count50Estimate) - countGood);
                }
                // If accuracy is between 16.67% and 25% - we assume that we have no 300s
                else if (relevantAccuracy >= 1.0 / 6)
                {
                    // Derived from the formula: Accuracy = (6 * c300 + 2 * c100 + c50) / (6 * totalHits), assuming that c300 = 0
                    double count100Estimate = 6 * relevantResultCount * relevantAccuracy - relevantResultCount;

                    // We only had 100s and 50s in that scenario so rest of the hits are 50s
                    double count50Estimate = relevantResultCount - count100Estimate;

                    // Round it to get int number of 100s
                    countGood = (int?)Math.Round(count100Estimate);

                    // Get number of 50s as difference between total mistimed hits and count100
                    countMeh = (int?)(Math.Round(count100Estimate + count50Estimate) - countGood);
                }
                // If accuracy is less than 16.67% - it means that we have only 50s or misses
                // Assuming that we removed misses in the 1st place - that means that we need to add additional misses to achieve target accuracy
                else
                {
                    // Derived from the formula: Accuracy = (6 * c300 + 2 * c100 + c50) / (6 * totalHits), assuming that c300 = c100 = 0
                    double count50Estimate = 6 * relevantResultCount * relevantAccuracy;

                    // We have 0 100s, because we can't start adding 100s again after reaching "only 50s" point
                    countGood = 0;

                    // Round it to get int number of 50s
                    countMeh = (int?)Math.Round(count50Estimate);

                    // Fill the rest results with misses overwriting initial countMiss
                    countMiss = (int)(totalResultCount - countMeh);
                }

                // Rest of the hits are 300s
                countGreat = (int)(totalResultCount - countGood - countMeh - countMiss);
            }

            var result = new Dictionary<HitResult, int>
            {
                { HitResult.Great, countGreat },
                { HitResult.Ok, countGood ?? 0 },
                { HitResult.Meh, countMeh ?? 0 },
                { HitResult.Miss, countMiss }
            };

            if (countLargeTickMisses != null)
                result[HitResult.LargeTickMiss] = countLargeTickMisses.Value;

            if (countSliderTailMisses != null)
                result[HitResult.SliderTailHit] = beatmap.HitObjects.Count(x => x is Slider) - countSliderTailMisses.Value;

            return result;
        }

        protected override double GetAccuracy(IBeatmap beatmap, Dictionary<HitResult, int> statistics)
        {
            var countGreat = statistics[HitResult.Great];
            var countGood = statistics[HitResult.Ok];
            var countMeh = statistics[HitResult.Meh];
            var countMiss = statistics[HitResult.Miss];

            double total = 6 * countGreat + 2 * countGood + countMeh;
            double max = 6 * (countGreat + countGood + countMeh + countMiss);

            if (statistics.ContainsKey(HitResult.SliderTailHit))
            {
                var countSliders = beatmap.HitObjects.Count(x => x is Slider);
                var countSliderTailHit = statistics[HitResult.SliderTailHit];

                total += 3 * countSliderTailHit;
                max += 3 * countSliders;
            }

            if (statistics.ContainsKey(HitResult.LargeTickMiss))
            {
                var countLargeTicks = beatmap.HitObjects.Sum(obj => obj.NestedHitObjects.Count(x => x is SliderTick or SliderRepeat));
                var countLargeTickHit = countLargeTicks - statistics[HitResult.LargeTickMiss];

                total += 0.6 * countLargeTickHit;
                max += 0.6 * countLargeTicks;
            }

            return total / max;
        }
    }
}
