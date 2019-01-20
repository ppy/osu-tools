// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

namespace PerformanceCalculator.Profile
{
    
    /// <summary>
    /// Holds the live pp value, beatmap name, and mods for a user play.
    /// </summary>
    public class PPInfo
    {
        public double LivePP;
        public string BeatmapInfo;
        public string ModInfo;
    }
}
