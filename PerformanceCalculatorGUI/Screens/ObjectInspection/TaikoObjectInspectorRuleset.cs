// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Edit;
using osu.Game.Rulesets.Taiko.UI;
using osu.Game.Rulesets.UI;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class TaikoObjectInspectorRuleset : DrawableTaikoEditorRuleset
    {
        private TaikoDifficultyHitObject[] difficultyHitObjects = [];

        [Resolved]
        private ObjectDifficultyValuesContainer objectDifficultyValuesContainer { get; set; } = null!;

        [Resolved]
        private Bindable<DifficultyCalculator?> difficultyCalculator { get; set; } = null!;

        public TaikoObjectInspectorRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods)
            : base(ruleset, beatmap, mods)
        {
            ShowSpeedChanges.Value = true;
        }

        protected override void LoadComplete()
        {
            var extendedDifficultyCalculator = (IExtendedDifficultyCalculator?)difficultyCalculator.Value;

            if (extendedDifficultyCalculator != null)
            {
                difficultyHitObjects = extendedDifficultyCalculator.GetDifficultyHitObjects().Cast<TaikoDifficultyHitObject>().ToArray();
            }

            base.LoadComplete();
        }

        public override bool PropagatePositionalInputSubTree => false;

        public override bool PropagateNonPositionalInputSubTree => false;

        protected override Playfield CreatePlayfield() => new TaikoObjectInspectorPlayfield();

        protected override void Update()
        {
            base.Update();
            objectDifficultyValuesContainer.CurrentDifficultyHitObject.Value = difficultyHitObjects.LastOrDefault(x => x.BaseObject.StartTime <= Clock.CurrentTime);
        }

        private partial class TaikoObjectInspectorPlayfield : TaikoPlayfield
        {
            protected override GameplayCursorContainer? CreateCursor() => null;

            public TaikoObjectInspectorPlayfield()
            {
                DisplayJudgements.Value = false;
            }
        }
    }
}
