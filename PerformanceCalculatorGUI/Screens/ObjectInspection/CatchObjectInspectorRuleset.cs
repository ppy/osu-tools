// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Catch.Edit;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class CatchObjectInspectorRuleset : DrawableCatchEditorRuleset
    {
        private CatchDifficultyHitObject[] difficultyHitObjects = [];

        [Resolved]
        private ObjectDifficultyValuesContainer objectDifficultyValuesContainer { get; set; } = null!;

        [Resolved]
        private Bindable<DifficultyCalculator?> difficultyCalculator { get; set; } = null!;

        public CatchObjectInspectorRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods)
            : base(ruleset, beatmap, mods)
        {
        }

        protected override void LoadComplete()
        {
            var extendedDifficultyCalculator = (IExtendedDifficultyCalculator?)difficultyCalculator.Value;

            if (extendedDifficultyCalculator != null)
            {
                difficultyHitObjects = extendedDifficultyCalculator.GetDifficultyHitObjects().Cast<CatchDifficultyHitObject>().ToArray();
            }

            base.LoadComplete();
        }

        public override bool PropagatePositionalInputSubTree => false;

        public override bool PropagateNonPositionalInputSubTree => false;

        protected override void Update()
        {
            base.Update();
            objectDifficultyValuesContainer.CurrentDifficultyHitObject.Value = difficultyHitObjects.LastOrDefault(x => x.BaseObject.StartTime <= Clock.CurrentTime);
        }
    }
}
