// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;

namespace PerformanceCalculatorGUI
{
    public class ExtendedCatchDifficultyCalculator : CatchDifficultyCalculator, IExtendedDifficultyCalculator
    {
        private Skill[] skills = [];
        private DifficultyHitObject[] difficultyHitObjects = [];

        public ExtendedCatchDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap)
            : base(ruleset, beatmap)
        {
        }

        public Skill[] GetSkills() => skills;
        public DifficultyHitObject[] GetDifficultyHitObjects() => difficultyHitObjects;

        protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(IBeatmap beatmap, double clockRate)
        {
            difficultyHitObjects = base.CreateDifficultyHitObjects(beatmap, clockRate).ToArray();
            return difficultyHitObjects;
        }

        protected override Skill[] CreateSkills(IBeatmap beatmap, Mod[] mods, double clockRate)
        {
            skills = base.CreateSkills(beatmap, mods, clockRate);
            return skills;
        }
    }
}
