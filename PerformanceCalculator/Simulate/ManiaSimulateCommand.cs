// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;

namespace PerformanceCalculator.Simulate
{
    [Command(Name = "mania", Description = "Computes the performance (pp) of a simulated osu!mania play.")]
    public class ManiaSimulateCommand : SimulateCommand
    {
        [UsedImplicitly]
        [Option(Template = "-M|--mehs <mehs>", Description = "Number of mehs. Will override accuracy if used. Otherwise is automatically calculated.")]
        public override int? Mehs { get; }

        [UsedImplicitly]
        [Option(Template = "-G|--goods <goods>", Description = "Number of goods. Will override accuracy if used. Otherwise is automatically calculated.")]
        public override int? Goods { get; }

        [UsedImplicitly]
        [Option(Template = "-O|--oks <oks>", Description = "Number of oks. Will override accuracy if used. Otherwise is automatically calculated.")]
        private int? oks { get; }

        [UsedImplicitly]
        [Option(Template = "-T|--greats <greats>", Description = "Number of greats. Will override accuracy if used. Otherwise is automatically calculated.")]
        private int? greats { get; }

        public override Ruleset Ruleset => new ManiaRuleset();

        protected override Dictionary<HitResult, int> GenerateHitResults(IBeatmap beatmap, Mod[] mods) => generateHitResults(beatmap, mods, Accuracy / 100, Misses, Mehs, oks, Goods, greats);

        private static Dictionary<HitResult, int> generateHitResults(IBeatmap beatmap, Mod[] mods, double accuracy, int countMiss, int? countMeh, int? countOk, int? countGood, int? countGreat)
        {
            // One judgement per normal note. Two judgements per hold note (head + tail).
            int totalHits = beatmap.HitObjects.Count;
            if (!mods.Any(m => m is ModClassic))
                totalHits += beatmap.HitObjects.Count(ho => ho is HoldNote);

            if (countMeh != null || countOk != null || countGood != null || countGreat != null)
            {
                int countPerfect = totalHits - (countMiss + (countMeh ?? 0) + (countOk ?? 0) + (countGood ?? 0) + (countGreat ?? 0));

                return new Dictionary<HitResult, int>
                {
                    [HitResult.Perfect] = countPerfect,
                    [HitResult.Great] = countGreat ?? 0,
                    [HitResult.Good] = countGood ?? 0,
                    [HitResult.Ok] = countOk ?? 0,
                    [HitResult.Meh] = countMeh ?? 0,
                    [HitResult.Miss] = countMiss
                };
            }

            int perfectValue = mods.Any(m => m is ModClassic) ? 60 : 61;

            // Let Great = 60, Good = 40, Ok = 20, Meh = 10, Miss = 0, Perfect = 61 or 60 depending on CL. The total should be this.
            int targetTotal = (int)Math.Round(accuracy * totalHits * perfectValue);

            // Start by assuming every non miss is a meh
            // This is how much increase is needed by the rest
            int remainingHits = totalHits - countMiss;
            int delta = targetTotal - 10 * remainingHits;

            // Each perfect increases total by 50 (CL) or 51 (no CL) (perfect - meh = 50 or 51)
            int perfects = Math.Min(delta / (perfectValue - 10), remainingHits);
            delta -= perfects * (perfectValue - 10);
            remainingHits -= perfects;

            // Each great increases total by 50 (great - meh = 50)
            int greats = Math.Min(delta / 50, remainingHits);
            delta -= greats * 50;
            remainingHits -= greats;

            // Each good increases total by 30 (good - meh = 30)
            countGood = Math.Min(delta / 30, remainingHits);
            delta -= countGood.Value * 30;
            remainingHits -= countGood.Value;

            // Each ok increases total by 10 (ok - meh = 10)
            int oks = delta / 10;
            remainingHits -= oks;

            // Everything else is a meh, as initially assumed
            countMeh = remainingHits;

            return new Dictionary<HitResult, int>
            {
                { HitResult.Perfect, perfects },
                { HitResult.Great, greats },
                { HitResult.Ok, oks },
                { HitResult.Good, countGood.Value },
                { HitResult.Meh, countMeh.Value },
                { HitResult.Miss, countMiss }
            };
        }

        protected override double GetAccuracy(IBeatmap beatmap, Dictionary<HitResult, int> statistics, Mod[] mods)
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
