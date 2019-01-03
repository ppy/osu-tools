// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using System.Collections.Generic;
using System.Globalization;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace PerformanceCalculator.Simulate.Mania
{
    public class ManiaSimulateProcessor : BaseSimulateProcessor
    {
        protected override int GetMaxCombo(IBeatmap beatmap) => 0;

        protected override Dictionary<HitResult, int> GenerateHitResults(double accuracy, IBeatmap beatmap, int amountMiss)
        {
            var totalHits = beatmap.HitObjects.Count;

            // Only total number of hits is considered currently, so specifics don't matter
            return new Dictionary<HitResult, int>
            {
                { HitResult.Perfect, totalHits },
                { HitResult.Great, 0 },
                { HitResult.Ok, 0 },
                { HitResult.Good, 0 },
                { HitResult.Meh, 0 },
                { HitResult.Miss, 0 }
            };
        }

        protected override void WritePlayInfo(ScoreInfo scoreInfo, IBeatmap beatmap)
        {
            WriteAttribute("Score", scoreInfo.TotalScore.ToString(CultureInfo.InvariantCulture));
        }

        public ManiaSimulateProcessor(BaseSimulateCommand command)
            : base(command)
        {
        }
    }
}
