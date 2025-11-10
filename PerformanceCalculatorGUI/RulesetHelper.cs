// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko;
using osu.Game.Rulesets.Taiko.Objects;

namespace PerformanceCalculatorGUI
{
    public static class RulesetHelper
    {
        public static DifficultyCalculator GetExtendedDifficultyCalculator(RulesetInfo ruleset, IWorkingBeatmap working)
        {
            return ruleset.OnlineID switch
            {
                0 => new ExtendedOsuDifficultyCalculator(ruleset, working),
                1 => new ExtendedTaikoDifficultyCalculator(ruleset, working),
                2 => new ExtendedCatchDifficultyCalculator(ruleset, working),
                3 => new ExtendedManiaDifficultyCalculator(ruleset, working),
                _ => ruleset.CreateInstance().CreateDifficultyCalculator(working)
            };
        }

        public static Ruleset GetRulesetFromLegacyID(int id)
        {
            return id switch
            {
                0 => new OsuRuleset(),
                1 => new TaikoRuleset(),
                2 => new CatchRuleset(),
                3 => new ManiaRuleset(),
                _ => throw new ArgumentException("Invalid ruleset ID provided.")
            };
        }

        public static int AdjustManiaScore(int score, IReadOnlyList<Mod> mods)
        {
            if (score != 1000000) return score;

            double scoreMultiplier = 1;

            // Cap score depending on difficulty adjustment mods (matters for mania).
            foreach (var mod in mods)
            {
                if (mod.Type == ModType.DifficultyReduction)
                    scoreMultiplier *= mod.ScoreMultiplier;
            }

            return (int)Math.Round(1000000 * scoreMultiplier);
        }

        public static Dictionary<HitResult, int> GenerateHitResultsForRuleset(RulesetInfo ruleset, double accuracy, IBeatmap beatmap, Mod[] mods, int countMiss, int? countMeh, int? countGood, int? countLargeTickMisses, int? countSliderTailMisses)
        {
            return ruleset.OnlineID switch
            {
                0 => generateOsuHitResults(accuracy, beatmap, mods, countMiss, countMeh, countGood, countLargeTickMisses, countSliderTailMisses),
                1 => generateTaikoHitResults(accuracy, beatmap, countMiss, countGood),
                2 => generateCatchHitResults(accuracy, beatmap, countMiss, countMeh, countGood),
                3 => generateManiaHitResults(accuracy, beatmap, mods, countMiss),
                _ => throw new ArgumentException("Invalid ruleset ID provided.")
            };
        }

        private static Dictionary<HitResult, int> generateOsuHitResults(double accuracy, IBeatmap beatmap, Mod[] mods, int countMiss, int? countMeh, int? countGood, int? countLargeTickMisses, int? countSliderTailMisses)
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

        private static Dictionary<HitResult, int> generateTaikoHitResults(double accuracy, IBeatmap beatmap, int countMiss, int? countGood)
        {
            int totalResultCount = beatmap.HitObjects.OfType<Hit>().Count();

            int countGreat;

            if (countGood != null)
            {
                countGreat = (int)(totalResultCount - countGood - countMiss);
            }
            else
            {
                // Let Great=2, Good=1, Miss=0. The total should be this.
                int targetTotal = (int)Math.Round(accuracy * totalResultCount * 2);

                countGreat = targetTotal - (totalResultCount - countMiss);
                countGood = totalResultCount - countGreat - countMiss;
            }

            return new Dictionary<HitResult, int>
            {
                { HitResult.Great, countGreat },
                { HitResult.Ok, (int)countGood },
                { HitResult.Meh, 0 },
                { HitResult.Miss, countMiss }
            };
        }

        private static Dictionary<HitResult, int> generateCatchHitResults(double accuracy, IBeatmap beatmap, int countMiss, int? countMeh, int? countGood)
        {
            int maxCombo = beatmap.HitObjects.Count(h => h is Fruit) + beatmap.HitObjects.OfType<JuiceStream>().SelectMany(j => j.NestedHitObjects).Count(h => !(h is TinyDroplet));

            int maxTinyDroplets = beatmap.HitObjects.OfType<JuiceStream>().Sum(s => s.NestedHitObjects.OfType<TinyDroplet>().Count());
            int maxDroplets = beatmap.HitObjects.OfType<JuiceStream>().Sum(s => s.NestedHitObjects.OfType<Droplet>().Count()) - maxTinyDroplets;
            int maxFruits = beatmap.HitObjects.OfType<Fruit>().Count() + 2 * beatmap.HitObjects.OfType<JuiceStream>().Count() + beatmap.HitObjects.OfType<JuiceStream>().Sum(s => s.RepeatCount);

            // Either given or max value minus misses
            int countDroplets = countGood ?? Math.Max(0, maxDroplets - countMiss);

            // Max value minus whatever misses are left. Negative if impossible missCount
            int countFruits = maxFruits - (countMiss - (maxDroplets - countDroplets));

            // Either given or the max amount of hit objects with respect to accuracy minus the already calculated fruits and drops.
            // Negative if accuracy not feasable with missCount.
            int countTinyDroplets = countMeh ?? (int)Math.Round(accuracy * (maxCombo + maxTinyDroplets)) - countFruits - countDroplets;

            // Whatever droplets are left
            int countTinyMisses = maxTinyDroplets - countTinyDroplets;

            return new Dictionary<HitResult, int>
            {
                { HitResult.Great, countFruits },
                { HitResult.LargeTickHit, countDroplets },
                { HitResult.SmallTickHit, countTinyDroplets },
                { HitResult.SmallTickMiss, countTinyMisses },
                { HitResult.Miss, countMiss }
            };
        }

        private static Dictionary<HitResult, int> generateManiaHitResults(double accuracy, IBeatmap beatmap, Mod[] mods, int countMiss)
        {
            int totalHits = beatmap.HitObjects.Count;
            if (!mods.Any(m => m is ModClassic))
                totalHits += beatmap.HitObjects.Count(ho => ho is HoldNote);

            int perfectValue = mods.Any(m => m is ModClassic) ? 60 : 61;

            // Let Great = 60, Good = 40, Ok = 20, Meh = 10, Miss = 0, Perfect = 61 or 60 depending on CL. The total should be this.
            int targetTotal = (int)Math.Round(accuracy * totalHits * perfectValue);

            // Start by assuming every non miss is a meh
            // This is how much increase is needed by the rest
            int remainingHits = totalHits - countMiss;
            int delta = Math.Max(targetTotal - (10 * remainingHits), 0);

            // Each perfect increases total by 50 (CL) or 51 (no CL) (perfect - meh = 50 or 51)
            int perfects = Math.Min(delta / (perfectValue - 10), remainingHits);
            delta -= perfects * (perfectValue - 10);
            remainingHits -= perfects;

            // Each great increases total by 50 (great - meh = 50)
            int greats = Math.Min(delta / 50, remainingHits);
            delta -= greats * 50;
            remainingHits -= greats;

            // Each good increases total by 30 (good - meh = 30)
            int goods = Math.Min(delta / 30, remainingHits);
            delta -= goods * 30;
            remainingHits -= goods;

            // Each ok increases total by 10 (ok - meh = 10)
            int oks = Math.Min(delta / 10, remainingHits);
            remainingHits -= oks;

            // Everything else is a meh, as initially assumed
            int mehs = remainingHits;

            return new Dictionary<HitResult, int>
            {
                { HitResult.Perfect, perfects },
                { HitResult.Great, greats },
                { HitResult.Ok, oks },
                { HitResult.Good, goods },
                { HitResult.Meh, mehs },
                { HitResult.Miss, countMiss }
            };
        }

        public static double GetAccuracyForRuleset(RulesetInfo ruleset, IBeatmap beatmap, Dictionary<HitResult, int> statistics, Mod[] mods)
        {
            return ruleset.OnlineID switch
            {
                0 => getOsuAccuracy(beatmap, statistics, mods),
                1 => getTaikoAccuracy(statistics),
                2 => getCatchAccuracy(statistics),
                3 => getManiaAccuracy(statistics, mods),
                _ => 0.0
            };
        }

        private static double getOsuAccuracy(IBeatmap beatmap, Dictionary<HitResult, int> statistics, Mod[] mods)
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

            if (!usingClassicSliderAccuracy && statistics.TryGetValue(HitResult.LargeTickMiss, out int countLargeTicksMiss))
            {
                int countLargeTicks = beatmap.HitObjects.Sum(obj => obj.NestedHitObjects.Count(x => x is SliderTick or SliderRepeat));
                int countLargeTickHit = countLargeTicks - countLargeTicksMiss;

                total += 0.6 * countLargeTickHit;
                max += 0.6 * countLargeTicks;
            }

            return total / max;
        }

        private static double getTaikoAccuracy(Dictionary<HitResult, int> statistics)
        {
            int countGreat = statistics[HitResult.Great];
            int countGood = statistics[HitResult.Ok];
            int countMiss = statistics[HitResult.Miss];
            int total = countGreat + countGood + countMiss;

            return (double)((2 * countGreat) + countGood) / (2 * total);
        }

        private static double getCatchAccuracy(Dictionary<HitResult, int> statistics)
        {
            double hits = statistics[HitResult.Great] + statistics[HitResult.LargeTickHit] + statistics[HitResult.SmallTickHit];
            double total = hits + statistics[HitResult.Miss] + statistics[HitResult.SmallTickMiss];

            return hits / total;
        }

        private static double getManiaAccuracy(Dictionary<HitResult, int> statistics, Mod[] mods)
        {
            int countPerfect = statistics[HitResult.Perfect];
            int countGreat = statistics[HitResult.Great];
            int countGood = statistics[HitResult.Good];
            int countOk = statistics[HitResult.Ok];
            int countMeh = statistics[HitResult.Meh];
            int countMiss = statistics[HitResult.Miss];

            int perfectWeight = mods.Any(m => m is ModClassic) ? 300 : 305;

            double total = (perfectWeight * countPerfect) + (300 * countGreat) + (200 * countGood) + (100 * countOk) + (50 * countMeh);
            double max = perfectWeight * (countPerfect + countGreat + countGood + countOk + countMeh + countMiss);

            return total / max;
        }
    }
}
