// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace PerformanceCalculator.Simulate.Osu
{
    public class OsuSimulateProcessor : BaseSimulateProcessor
    {
        protected override int GetMaxCombo(IBeatmap beatmap) => beatmap.HitObjects.Count + beatmap.HitObjects.OfType<Slider>().Sum(s => s.NestedHitObjects.Count - 1);

        protected override Dictionary<HitResult, int> GenerateHitResults(double accuracy, IBeatmap beatmap, int amountMiss)
        {
            var totalHitObjects = beatmap.HitObjects.Count;

            // Let Great=6, Good=2, Meh=1, Miss=0. The total should be this.
            var targetTotal = (int)Math.Round(accuracy*totalHitObjects*6);

            // Start by assuming every non miss is a meh
            // This is how much increase is needed by greats and goods
            var delta = targetTotal - (totalHitObjects - amountMiss);

            // Each great increases total by 5 (great-meh=5)
            var amountGreat = delta / 5;
            // Each good increases total by 1 (good-meh=1). Covers remaining difference.
            var amountGood = delta % 5;
            // Mehs are left over. Could be negative if impossible value of amountMiss chosen
            var amountMeh = totalHitObjects - amountGreat - amountGood - amountMiss;

            return new Dictionary<HitResult, int>
            {
                { HitResult.Great, amountGreat },
                { HitResult.Good, amountGood },
                { HitResult.Meh, amountMeh },
                { HitResult.Miss, amountMiss }
            };
        }

        protected override void WritePlayInfo(ScoreInfo scoreInfo, IBeatmap beatmap)
        {
            WriteAttribute("Accuracy", (scoreInfo.Accuracy * 100).ToString(CultureInfo.InvariantCulture) + "%");
            WriteAttribute("Combo", FormattableString.Invariant($"{scoreInfo.MaxCombo} ({Math.Round(100.0 * scoreInfo.MaxCombo / GetMaxCombo(beatmap), 2)}%)"));
            WriteAttribute("Misses", scoreInfo.Statistics[HitResult.Miss].ToString(CultureInfo.InvariantCulture));
        }

        public OsuSimulateProcessor(BaseSimulateCommand command)
            : base(command)
        {
        }
    }
}
