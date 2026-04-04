// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Beatmaps;

namespace PerformanceCalculator.Profile
{
    /// <summary>
    /// Holds the live pp value, beatmap name, and mods for a user play.
    /// </summary>
    public class UserPlayInfo
    {
        public required double LocalPP;
        public required double LivePP;
        public required double MissCount;
        public required double Accuracy;

        public required BeatmapInfo Beatmap;

        public required string[] Mods;
        public required int Combo;
        public required int MaxCombo;
    }
}
