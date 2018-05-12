// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Scoring;

namespace PerformanceCalculator
{
    public abstract class Processor : Component
    {
        [BackgroundDependencyLoader]
        private void load(BeatmapManager beatmaps, ScoreStore scores, GameHost host)
        {
            Execute(beatmaps, scores);
            host.Exit();
        }
        
        protected abstract void Execute(BeatmapManager beatmaps, ScoreStore scores);
    }
}
