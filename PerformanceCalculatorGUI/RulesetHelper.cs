// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Mania.Mods;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Skinning;
using osu.Game.Utils;

namespace PerformanceCalculatorGUI
{
    public static class RulesetHelper
    {
        /// <summary>
        /// Transforms a given <see cref="Mod"/> combination into one which is applicable to legacy scores.
        /// This is used to match osu!stable/osu!web calculations for the time being, until such a point that these mods do get considered.
        /// </summary>
        public static Mod[] ConvertToLegacyDifficultyAdjustmentMods(Ruleset ruleset, Mod[] mods)
        {
            var beatmap = new EmptyWorkingBeatmap
            {
                BeatmapInfo =
                {
                    Ruleset = ruleset.RulesetInfo,
                    Difficulty = new BeatmapDifficulty()
                }
            };

            var allMods = ruleset.CreateAllMods().ToArray();

            var allowedMods = ModUtils.FlattenMods(
                                          ruleset.CreateDifficultyCalculator(beatmap).CreateDifficultyAdjustmentModCombinations())
                                      .Select(m => m.GetType())
                                      .Distinct()
                                      .ToHashSet();

            // Special case to allow either DT or NC.
            if (allowedMods.Any(type => type.IsSubclassOf(typeof(ModDoubleTime))) && mods.Any(m => m is ModNightcore))
                allowedMods.Add(allMods.Single(m => m is ModNightcore).GetType());

            var result = new List<Mod>();

            var classicMod = allMods.SingleOrDefault(m => m is ModClassic);
            if (classicMod != null)
                result.Add(classicMod);

            result.AddRange(mods.Where(m => allowedMods.Contains(m.GetType())));

            return result.ToArray();
        }

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

        /// <summary>
        /// Generates the unique hash of mods combo that affect difficulty calculation
        /// Needs to be updated if list of difficulty adjusting mods changes
        /// </summary>
        public static int GenerateModsHash(Mod[] mods, BeatmapDifficulty difficulty, RulesetInfo ruleset)
        {
            // Rate changing mods
            double rate = ModUtils.CalculateRateWithMods(mods);

            int hash = 0;

            if (ruleset.OnlineID == 0) // For osu we have many different things
            {
                BeatmapDifficulty d = new BeatmapDifficulty(difficulty);

                foreach (var mod in mods.OfType<IApplicableToDifficulty>())
                    mod.ApplyToDifficulty(d);

                bool isSliderAccuracy = mods.OfType<OsuModClassic>().All(m => !m.NoSliderHeadAccuracy.Value);

                byte flashlightHash = 0;

                if (mods.Any(h => h is OsuModFlashlight))
                {
                    flashlightHash = (byte)(mods.Any(h => h is OsuModHidden) ? 2 : 1);
                }

                byte mirrorHash = 0;

                if (mods.Any(m => m is OsuModHardRock))
                {
                    mirrorHash = 1 + (int)OsuModMirror.MirrorType.Vertical;
                }
                else if (mods.FirstOrDefault(m => m is OsuModMirror) is OsuModMirror mirror)
                {
                    mirrorHash = (byte)(1 + (int)mirror.Reflection.Value);
                }

                hash = HashCode.Combine(rate, d.CircleSize, d.OverallDifficulty, isSliderAccuracy, flashlightHash, mirrorHash);
            }
            else if (ruleset.OnlineID == 1) // For taiko we only have rate
            {
                hash = rate.GetHashCode();
            }
            else if (ruleset.OnlineID == 2) // For catch we have rate and CS
            {
                BeatmapDifficulty d = new BeatmapDifficulty(difficulty);

                foreach (var mod in mods.OfType<IApplicableToDifficulty>())
                    mod.ApplyToDifficulty(d);

                hash = HashCode.Combine(rate, d.CircleSize);
            }
            else if (ruleset.OnlineID == 3) // Mania is using rate, and keys data for converts
            {
                int keyCount = 0;

                if (mods.FirstOrDefault(h => h is ManiaKeyMod) is ManiaKeyMod mod)
                    keyCount = mod.KeyCount;

                bool isDualStages = mods.Any(h => h is ManiaModDualStages);

                hash = HashCode.Combine(rate, keyCount, isDualStages);
            }

            return hash;
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

        public static Dictionary<HitResult, int> GenerateHitResultsForRuleset(RulesetInfo ruleset, double accuracy, IBeatmap beatmap, int countMiss, int? countMeh, int? countGood)
        {
            return ruleset.OnlineID switch
            {
                0 => generateOsuHitResults(accuracy, beatmap, countMiss, countMeh, countGood),
                1 => generateTaikoHitResults(accuracy, beatmap, countMiss, countGood),
                2 => generateCatchHitResults(accuracy, beatmap, countMiss, countMeh, countGood),
                3 => generateManiaHitResults(accuracy, beatmap, countMiss),
                _ => throw new ArgumentException("Invalid ruleset ID provided.")
            };
        }

        private static Dictionary<HitResult, int> generateOsuHitResults(double accuracy, IBeatmap beatmap, int countMiss, int? countMeh, int? countGood)
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

            return new Dictionary<HitResult, int>
            {
                { HitResult.Great, countGreat },
                { HitResult.Ok, countGood ?? 0 },
                { HitResult.Meh, countMeh ?? 0 },
                { HitResult.Miss, countMiss }
            };
        }

        private static Dictionary<HitResult, int> generateTaikoHitResults(double accuracy, IBeatmap beatmap, int countMiss, int? countGood)
        {
            var totalResultCount = beatmap.HitObjects.OfType<Hit>().Count();

            int countGreat;

            if (countGood != null)
            {
                countGreat = (int)(totalResultCount - countGood - countMiss);
            }
            else
            {
                // Let Great=2, Good=1, Miss=0. The total should be this.
                var targetTotal = (int)Math.Round(accuracy * totalResultCount * 2);

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
            var maxCombo = beatmap.HitObjects.Count(h => h is Fruit) + beatmap.HitObjects.OfType<JuiceStream>().SelectMany(j => j.NestedHitObjects).Count(h => !(h is TinyDroplet));

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

        private static Dictionary<HitResult, int> generateManiaHitResults(double accuracy, IBeatmap beatmap, int countMiss)
        {
            var totalResultCount = beatmap.HitObjects.Count;

            // Let Great=6, Good=2, Meh=1, Miss=0. The total should be this.
            var targetTotal = (int)Math.Round(accuracy * totalResultCount * 6);

            // Start by assuming every non miss is a meh
            // This is how much increase is needed by greats and goods
            var delta = targetTotal - (totalResultCount - countMiss);

            // Each great increases total by 5 (great-meh=5)
            int countGreat = delta / 5;
            // Each good increases total by 1 (good-meh=1). Covers remaining difference.
            int countGood = delta % 5;
            // Mehs are left over. Could be negative if impossible value of amountMiss chosen
            int countMeh = totalResultCount - countGreat - countGood - countMiss;

            return new Dictionary<HitResult, int>
            {
                { HitResult.Perfect, countGreat },
                { HitResult.Great, 0 },
                { HitResult.Good, countGood },
                { HitResult.Ok, 0 },
                { HitResult.Meh, countMeh },
                { HitResult.Miss, countMiss }
            };
        }

        public static double GetAccuracyForRuleset(RulesetInfo ruleset, Dictionary<HitResult, int> statistics)
        {
            return ruleset.OnlineID switch
            {
                0 => getOsuAccuracy(statistics),
                1 => getTaikoAccuracy(statistics),
                2 => getCatchAccuracy(statistics),
                3 => getManiaAccuracy(statistics),
                _ => 0.0
            };
        }

        private static double getOsuAccuracy(Dictionary<HitResult, int> statistics)
        {
            var countGreat = statistics[HitResult.Great];
            var countGood = statistics[HitResult.Ok];
            var countMeh = statistics[HitResult.Meh];
            var countMiss = statistics[HitResult.Miss];
            var total = countGreat + countGood + countMeh + countMiss;

            return (double)((6 * countGreat) + (2 * countGood) + countMeh) / (6 * total);
        }

        private static double getTaikoAccuracy(Dictionary<HitResult, int> statistics)
        {
            var countGreat = statistics[HitResult.Great];
            var countGood = statistics[HitResult.Ok];
            var countMiss = statistics[HitResult.Miss];
            var total = countGreat + countGood + countMiss;

            return (double)((2 * countGreat) + countGood) / (2 * total);
        }

        private static double getCatchAccuracy(Dictionary<HitResult, int> statistics)
        {
            double hits = statistics[HitResult.Great] + statistics[HitResult.LargeTickHit] + statistics[HitResult.SmallTickHit];
            double total = hits + statistics[HitResult.Miss] + statistics[HitResult.SmallTickMiss];

            return hits / total;
        }

        private static double getManiaAccuracy(Dictionary<HitResult, int> statistics)
        {
            var countPerfect = statistics[HitResult.Perfect];
            var countGreat = statistics[HitResult.Great];
            var countGood = statistics[HitResult.Good];
            var countOk = statistics[HitResult.Ok];
            var countMeh = statistics[HitResult.Meh];
            var countMiss = statistics[HitResult.Miss];
            var total = countPerfect + countGreat + countGood + countOk + countMeh + countMiss;

            return (double)
                   ((6 * (countPerfect + countGreat)) + (4 * countGood) + (2 * countOk) + countMeh) /
                   (6 * total);
        }

        private class EmptyWorkingBeatmap : WorkingBeatmap
        {
            public EmptyWorkingBeatmap()
                : base(new BeatmapInfo(), null)
            {
            }

            protected override IBeatmap GetBeatmap() => throw new NotImplementedException();

            public override Texture GetBackground() => throw new NotImplementedException();

            protected override Track GetBeatmapTrack() => throw new NotImplementedException();

            protected override ISkin GetSkin() => throw new NotImplementedException();

            public override Stream GetStream(string storagePath) => throw new NotImplementedException();
        }
    }
}
