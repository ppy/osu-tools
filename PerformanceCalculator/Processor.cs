// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Platform;

namespace PerformanceCalculator
{
    public abstract class Processor : Component
    {
        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            Execute();
            host.Exit();
        }

        protected abstract void Execute();
    }
}
