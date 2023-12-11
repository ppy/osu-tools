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
        [Option(Template = "-X|--misses <misses>", Description = "Number of misses. Defaults to 0.")]
        public override int Misses { get; }

        [UsedImplicitly]
        [Option(Template = "-M|--mehs <mehs>", Description = "Number of mehs. Will override accuracy if used. Otherwise is automatically calculated.")]
        public override int? Mehs { get; }

        [UsedImplicitly]
        [Option(Template = "-O|--oks <oks>", Description = "Number of oks. Will override accuracy if used. Otherwise is automatically calculated.")]
        private int? oks { get; set; }

        [UsedImplicitly]
        [Option(Template = "-G|--goods <goods>", Description = "Number of goods. Will override accuracy if used. Otherwise is automatically calculated.")]
        public override int? Goods { get; }

        [UsedImplicitly]
        [Option(Template = "-T|--greats <greats>", Description = "Number of greats. Will override accuracy if used. Otherwise is automatically calculated.")]
        private int? greats { get; set; }

        [UsedImplicitly]
        [Option(CommandOptionType.MultipleValue, Template = "-m|--mod <mod>", Description = "One for each mod. The mods to compute the performance with."
                                                                                            + " Values: hr, dt, fl, 4k, 5k, etc...")]
        public override string[] Mods { get; }

        public override Ruleset Ruleset => new ManiaRuleset();

        protected override int GetMaxCombo(IBeatmap beatmap) => 0;

        protected override Dictionary<HitResult, int> GenerateHitResults(double accuracy, IBeatmap beatmap, int countMiss, int? countMeh, int? countGood)
        {
            // One judgement per normal note. Two judgements per hold note (head + tail).
            var totalHits = beatmap.HitObjects.Count + beatmap.HitObjects.Count(ho => ho is HoldNote);

            if (countMeh != null || oks != null || countGood != null || greats != null)
            {
                int countPerfect = totalHits - (countMiss + (countMeh ?? 0) + (oks ?? 0) + (countGood ?? 0) + (greats ?? 0));

                return new Dictionary<HitResult, int>
                {
                    [HitResult.Perfect] = countPerfect,
                    [HitResult.Great] = greats ?? 0,
                    [HitResult.Good] = countGood ?? 0,
                    [HitResult.Ok] = oks ?? 0,
                    [HitResult.Meh] = countMeh ?? 0,
                    [HitResult.Miss] = countMiss
                };
            }

            // Let Great=Perfect=6, Good=4, Ok=2, Meh=1, Miss=0. The total should be this.
            var targetTotal = (int)Math.Round(accuracy * totalHits * 6);

            // Start by assuming every non miss is a meh
            // This is how much increase is needed by the rest
            int remainingHits = totalHits - countMiss;
            int delta = targetTotal - remainingHits;

            // Each great and perfect increases total by 5 (great-meh=5)
            // There is no difference in accuracy between them, so just halve arbitrarily.
            greats = Math.Min(delta / 5, remainingHits) / 2;
            int perfects = greats.Value;
            delta -= (greats.Value + perfects) * 5;
            remainingHits -= (greats.Value + perfects);

            // Each good increases total by 3 (good-meh=3).
            countGood = Math.Min(delta / 3, remainingHits);
            delta -= countGood.Value * 3;
            remainingHits -= countGood.Value;

            // Each ok increases total by 1 (ok-meh=1).
            oks = delta;

            // Everything else is a meh, as initially assumed.
            countMeh = remainingHits;

            return new Dictionary<HitResult, int>
            {
                { HitResult.Perfect, perfects },
                { HitResult.Great, greats.Value },
                { HitResult.Ok, oks.Value },
                { HitResult.Good, countGood.Value },
                { HitResult.Meh, countMeh.Value },
                { HitResult.Miss, countMiss }
            };
        }
    }
}
