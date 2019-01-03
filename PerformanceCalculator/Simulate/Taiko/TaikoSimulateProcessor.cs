// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Scoring;

namespace PerformanceCalculator.Simulate.Taiko
{
    public class TaikoSimulateProcessor : BaseSimulateProcessor
    {
        protected override int GetMaxCombo(IBeatmap beatmap) => beatmap.HitObjects.OfType<Hit>().Count();

        protected override Dictionary<HitResult, int> GenerateHitResults(double accuracy, IBeatmap beatmap, int amountMiss)
        {
            var totalHitObjects = beatmap.HitObjects.Count;

            // Does not need to match acc currently since only total and miss count matters
            var amountGreat = totalHitObjects - amountMiss;

            return new Dictionary<HitResult, int>
            {
                { HitResult.Great, amountGreat },
                { HitResult.Good, 0 },
                { HitResult.Meh, 0 },
                { HitResult.Miss, amountMiss }
            };
        }

        protected override void WritePlayInfo(ScoreInfo scoreInfo, IBeatmap beatmap)
        {
            WriteAttribute("Accuracy", (scoreInfo.Accuracy * 100).ToString(CultureInfo.InvariantCulture) + "%");
            WriteAttribute("Combo", FormattableString.Invariant($"{scoreInfo.MaxCombo} ({Math.Round(100.0 * scoreInfo.MaxCombo / GetMaxCombo(beatmap), 2)}%)"));
            WriteAttribute("Misses", scoreInfo.Statistics[HitResult.Miss].ToString(CultureInfo.InvariantCulture));
        }

        public TaikoSimulateProcessor(BaseSimulateCommand command)
            : base(command)
        {
        }
    }
}
