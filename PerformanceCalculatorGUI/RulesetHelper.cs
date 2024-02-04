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
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
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
                // Let Great=6, Good=2, Meh=1, Miss=0. The total should be this.
                var targetTotal = (int)Math.Round(accuracy * totalResultCount * 6);

                // Start by assuming every non miss is a meh
                // This is how much increase is needed by greats and goods
                var delta = targetTotal - (totalResultCount - countMiss);

                // Each great increases total by 5 (great-meh=5)
                countGreat = delta / 5;
                // Each good increases total by 1 (good-meh=1). Covers remaining difference.
                countGood = delta % 5;
                // Mehs are left over. Could be negative if impossible value of amountMiss chosen
                countMeh = totalResultCount - countGreat - countGood - countMiss;
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
