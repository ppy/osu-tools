// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch.Difficulty;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mania.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Taiko.Difficulty;

namespace PerformanceCalculatorGUI
{
    internal interface IExtendedDifficultyCalculator
    {
        Skill[] GetSkills();
    }

    internal class ExtendedOsuDifficultyCalculator : OsuDifficultyCalculator, IExtendedDifficultyCalculator
    {
        private Skill[] skills;

        public ExtendedOsuDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap) : base(ruleset, beatmap)
        {
        }

        public Skill[] GetSkills() => skills;

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            this.skills = skills;
            return base.CreateDifficultyAttributes(beatmap, mods, skills, clockRate);
        }
    }

    internal class ExtendedTaikoDifficultyCalculator : TaikoDifficultyCalculator, IExtendedDifficultyCalculator
    {
        private Skill[] skills;

        public ExtendedTaikoDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap) : base(ruleset, beatmap)
        {
        }

        public Skill[] GetSkills() => skills;

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            this.skills = skills;
            return base.CreateDifficultyAttributes(beatmap, mods, skills, clockRate);
        }
    }

    internal class ExtendedCatchDifficultyCalculator : CatchDifficultyCalculator, IExtendedDifficultyCalculator
    {
        private Skill[] skills;

        public ExtendedCatchDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap) : base(ruleset, beatmap)
        {
        }

        public Skill[] GetSkills() => skills;

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            this.skills = skills;
            return base.CreateDifficultyAttributes(beatmap, mods, skills, clockRate);
        }
    }

    internal class ExtendedManiaDifficultyCalculator : ManiaDifficultyCalculator, IExtendedDifficultyCalculator
    {
        private Skill[] skills;

        public ExtendedManiaDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap) : base(ruleset, beatmap)
        {
        }

        public Skill[] GetSkills() => skills;

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            this.skills = skills;
            return base.CreateDifficultyAttributes(beatmap, mods, skills, clockRate);
        }
    }
}
