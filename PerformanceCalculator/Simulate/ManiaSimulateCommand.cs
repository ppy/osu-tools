// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;

namespace PerformanceCalculator.Simulate
{
    [Command(Name = "mania", Description = "Computes the performance (pp) of a simulated osu!mania play.")]
    public class ManiaSimulateCommand : SimulateCommand
    {
        [UsedImplicitly]
        [Option(Template = "-a|--accuracy <accuracy>", Description = "Accuracy. Enter as decimal 0-100. Defaults to 100."
                                                                     + " Scales hit results as well and is rounded to the nearest possible value for the beatmap.")]
        public override double Accuracy { get; } = 100;

        [UsedImplicitly]
        [Option(CommandOptionType.MultipleValue, Template = "-m|--mod <mod>", Description = "One for each mod. The mods to compute the performance with."
                                                                                            + " Values: hr, dt, fl, 4k, 5k, etc...")]
        public override string[] Mods { get; }

        [UsedImplicitly]
        [Option(Template = "-X|--misses <misses>", Description = "Number of misses. Defaults to 0.")]
        public override int Misses { get; }

        [UsedImplicitly]
        [Option(Template = "-M|--mehs <mehs>", Description = "Number of mehs (50). Will override accuracy if used. Otherwise is automatically calculated.")]
        public override int? Mehs { get; }

        [UsedImplicitly]
        [Option(Template = "-O|--oks <oks>", Description = "Number of oks (100).")]
        public override int? Oks { get; }

        [UsedImplicitly]
        [Option(Template = "-G|--goods <goods>", Description = "Number of goods (200).")]
        public override int? Goods { get; }

        [UsedImplicitly]
        [Option(Template = "-GR|--greats <greats>", Description = "Number of greats (300).")]
        public override int? Greats { get; }

        public override Ruleset Ruleset => new ManiaRuleset();

        protected override int GetMaxCombo(IBeatmap beatmap) => 0;

        protected override Dictionary<HitResult, int> GenerateHitResults(double accuracy, IBeatmap beatmap, int countMiss, int? countMeh, int? countOk, int? countGood, int? countGreat)
        {
            var totalHits = beatmap.HitObjects.Count;

            countMiss = Math.Clamp(countMiss, 0, totalHits);

            if (countMeh == null)
            {
                // Populate score with mehs to make this approximation more precise.
                // This value can be negative on impossible misscount.
                //
                // total = ((1/6) * meh + (1/3) * ok + (2/3) * good + great + perfect) / acc
                // total = miss + meh + ok + good + great + perfect
                //
                // miss + (5/6) * meh + (2/3) * ok + (1/3) * good = total - acc * total
                // meh = 1.2 * (total - acc * total) - 1.2 * miss - 0.8 * ok - 0.4 * good
                countMeh = (int)Math.Round((1.2 * (totalHits - totalHits * accuracy)) - (1.2 * countMiss) - (0.8 * (countOk ?? 0)) - (0.4 * (countGood ?? 0)));
            }

            // We need to clamp for all values because performance calculator's custom accuracy formula is not invariant to negative counts.
            int currentCounts = countMiss;

            countMeh = Math.Clamp(countMeh ?? 0, 0, totalHits - currentCounts);
            currentCounts += countMeh ?? 0;

            countOk = Math.Clamp(countOk ?? 0, 0, totalHits - currentCounts);
            currentCounts += countOk ?? 0;

            countGood = Math.Clamp(countGood ?? 0, 0, totalHits - currentCounts);
            currentCounts += countGood ?? 0;

            countGreat = Math.Clamp(countGreat ?? 0, 0, totalHits - currentCounts);

            int countPerfect = totalHits - (countGreat ?? 0) - (countGood ?? 0) - (countOk ?? 0) - (countMeh ?? 0) - countMiss;

            return new Dictionary<HitResult, int>
            {
                { HitResult.Perfect, countPerfect },
                { HitResult.Great, countGreat ?? 0 },
                { HitResult.Ok, countOk ?? 0 },
                { HitResult.Good, countGood ?? 0 },
                { HitResult.Meh, countMeh ?? 0 },
                { HitResult.Miss, countMiss }
            };
        }

        protected override double GetAccuracy(Dictionary<HitResult, int> statistics)
        {
            var countPerfect = statistics[HitResult.Perfect];
            var countGreat = statistics[HitResult.Great];
            var countGood = statistics[HitResult.Good];
            var countOk = statistics[HitResult.Ok];
            var countMeh = statistics[HitResult.Meh];
            var countMiss = statistics[HitResult.Miss];
            var total = countPerfect + countGreat + countGood + countOk + countMeh + countMiss;

            return (double)(((countMeh / 6.0) + (countOk / 3.0) + (countGood / 1.5) + countGreat + countPerfect) / total);
        }
    }
}
