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
                return generateHitResults(beatmap, Accuracy / 100, mods, Misses, Mehs, Goods, null, null);
            }
            else
            {
                return generateHitResults(beatmap, Accuracy / 100, mods, Misses, Mehs, Goods, largeTickMisses, sliderTailMisses);
            }
        }

        private static Dictionary<HitResult, int> generateHitResults(IBeatmap beatmap, double accuracy, Mod[] mods, int countMiss, int? countMeh, int? countGood, int? countLargeTickMisses, int? countSliderTailMisses)
        {
            bool usingClassicSliderAccuracy = mods.OfType<OsuModClassic>().Any(m => m.NoSliderHeadAccuracy.Value);

            int countGreat;

            int totalResultCount = beatmap.HitObjects.Count;

            int countLargeTicks = beatmap.HitObjects.Sum(obj => obj.NestedHitObjects.Count(x => x is SliderTick or SliderRepeat));
            int countSmallTicks = beatmap.HitObjects.Count(x => x is Slider);

            // Sliderheads are large ticks too if slideracc is disabled
            if (usingClassicSliderAccuracy)
                countLargeTicks += countSmallTicks;

            countLargeTickMisses = Math.Min(countLargeTickMisses ?? 0, countLargeTicks);
            countSliderTailMisses = Math.Min(countSliderTailMisses ?? 0, countSmallTicks);

            if (countMeh != null || countGood != null)
            {
                countGreat = totalResultCount - (countGood ?? 0) - (countMeh ?? 0) - countMiss;
            }
            else
            {
                // Relevant result count without misses (normal misses and slider-related misses)
                // We need to exclude them from judgement count so total value will be equal to desired after misses are accounted for
                double countSuccessfulHits;

                // If there's no classic slider accuracy - we need to weight normal judgements accordingly.
                // Normal judgements in this context are 300s, 100s, 50s and misses.
                // Slider-related judgements are large tick hits/misses and slider tail hits/misses.
                double normalJudgementWeight = 1.0;

                if (usingClassicSliderAccuracy)
                {
                    countSuccessfulHits = totalResultCount - countMiss;
                }
                else
                {
                    double maxSliderPortion = countSmallTicks * 0.5 + countLargeTicks * 0.1;
                    normalJudgementWeight = (totalResultCount + maxSliderPortion) / totalResultCount;

                    double missedSliderPortion = (double)countSliderTailMisses * 0.5 + (double)countLargeTickMisses * 0.1;
                    countSuccessfulHits = totalResultCount - (countMiss + missedSliderPortion) / normalJudgementWeight;
                }

                // Accuracy excluding countMiss. We need that because we're trying to achieve target accuracy without touching countMiss
                // So it's better to pretened that there were 0 misses in the 1st place
                double relevantAccuracy = accuracy * totalResultCount / countSuccessfulHits;

                // Clamp accuracy to account for user trying to break the algorithm by inputting impossible values
                relevantAccuracy = Math.Clamp(relevantAccuracy, 0, 1);

                // Main curve for accuracy > 25%, the closer accuracy is to 25% - the more 50s it adds
                if (relevantAccuracy >= 0.25)
                {
                    // Main curve. Zero 50s if accuracy is 100%, one 50 per 9 100s if accuracy is 75% (excluding misses), 4 50s per 9 100s if accuracy is 50%
                    double ratio50To100 = Math.Pow(1 - (relevantAccuracy - 0.25) / 0.75, 2);

                    // Derived from the formula: Accuracy = (6 * c300 + 2 * c100 + c50) / (6 * totalHits), assuming that c50 = c100 * ratio50to100
                    double count100Estimate = 6 * countSuccessfulHits * (1 - relevantAccuracy) / (5 * ratio50To100 + 4) * normalJudgementWeight;

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
                    double count100Estimate = 6 * countSuccessfulHits * relevantAccuracy - countSuccessfulHits;

                    // We only had 100s and 50s in that scenario so rest of the hits are 50s
                    double count50Estimate = countSuccessfulHits - count100Estimate;

                    // Round it to get int number of 100s
                    countGood = (int?)Math.Round(count100Estimate * normalJudgementWeight);

                    // Get number of 50s as difference between total mistimed hits and count100
                    countMeh = (int?)(Math.Round((count100Estimate + count50Estimate) * normalJudgementWeight) - countGood);
                }
                // If accuracy is less than 16.67% - it means that we have only 50s or misses
                // Assuming that we removed misses in the 1st place - that means that we need to add additional misses to achieve target accuracy
                else
                {
                    // Derived from the formula: Accuracy = (6 * c300 + 2 * c100 + c50) / (6 * totalHits), assuming that c300 = c100 = 0
                    double count50Estimate = 6 * (totalResultCount - countMiss) * relevantAccuracy;

                    // We have 0 100s, because we can't start adding 100s again after reaching "only 50s" point
                    countGood = 0;

                    // Round it to get int number of 50s
                    countMeh = (int?)Math.Round(count50Estimate);

                    // Fill the rest results with misses overwriting initial countMiss
                    countMiss = (int)(totalResultCount - countMeh);
                }

                // Clamp goods if total amount is bigger than possible
                countGood -= Math.Clamp((int)(countGood + countMeh + countMiss - totalResultCount), 0, (int)countGood);
                countMeh -= Math.Clamp((int)(countGood + countMeh + countMiss - totalResultCount), 0, (int)countMeh);

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

            result[HitResult.LargeTickHit] = countLargeTicks - (int)countLargeTickMisses;
            result[HitResult.LargeTickMiss] = (int)countLargeTickMisses;
            result[usingClassicSliderAccuracy ? HitResult.SmallTickHit : HitResult.SliderTailHit] = countSmallTicks - (int)countSliderTailMisses;

            // Only classic slider accuracy scores has small tick misses
            if (usingClassicSliderAccuracy)
                result[HitResult.SmallTickMiss] = (int)countSliderTailMisses;

            return result;
        }

        protected override double GetAccuracy(IBeatmap beatmap, Dictionary<HitResult, int> statistics, Mod[] mods)
        {
            bool usingClassicSliderAccuracy = mods.OfType<OsuModClassic>().Any(m => m.NoSliderHeadAccuracy.Value);

            int countGreat = statistics[HitResult.Great];
            int countGood = statistics[HitResult.Ok];
            int countMeh = statistics[HitResult.Meh];
            int countMiss = statistics[HitResult.Miss];

            double total = 6 * countGreat + 2 * countGood + countMeh;
            double max = 6 * (countGreat + countGood + countMeh + countMiss);

            if (!usingClassicSliderAccuracy && statistics.TryGetValue(HitResult.SliderTailHit, out int countSliderTailHit))
            {
                int countSliders = beatmap.HitObjects.Count(x => x is Slider);

                total += 3 * countSliderTailHit;
                max += 3 * countSliders;
            }

            if (!usingClassicSliderAccuracy && statistics.TryGetValue(HitResult.LargeTickMiss, out int countLargeTickMiss))
            {
                int countLargeTicks = beatmap.HitObjects.Sum(obj => obj.NestedHitObjects.Count(x => x is SliderTick or SliderRepeat));
                int countLargeTickHit = countLargeTicks - countLargeTickMiss;

                total += 0.6 * countLargeTickHit;
                max += 0.6 * countLargeTicks;
            }

            return total / max;
        }
    }
}
