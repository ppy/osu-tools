// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Taiko;
using osu.Game.Scoring.Legacy;

namespace PerformanceCalculator
{
    /// <summary>
    /// A <see cref="LegacyScoreParser"/> which has a predefined beatmap and rulesets.
    /// </summary>
    public class ProcessorScoreParser : LegacyScoreParser
    {
        private readonly WorkingBeatmap beatmap;

        public ProcessorScoreParser(WorkingBeatmap beatmap)
        {
            this.beatmap = beatmap;
        }

        protected override Ruleset GetRuleset(int rulesetId)
        {
            switch (rulesetId)
            {
                case 0:
                    return new OsuRuleset();
                case 1:
                    return new TaikoRuleset();
                case 2:
                    return new CatchRuleset();
                case 3:
                    return new ManiaRuleset();
            }

            return null;
        }

        protected override WorkingBeatmap GetBeatmap(string md5Hash) => beatmap;
    }
}
