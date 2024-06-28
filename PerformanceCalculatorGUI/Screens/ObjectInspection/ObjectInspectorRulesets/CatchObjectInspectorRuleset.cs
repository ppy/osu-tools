// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Catch.Edit;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;
using osu.Game.Scoring;
using PerformanceCalculatorGUI.Screens.ObjectInspection.BlueprintContainers;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.ObjectInspectorRulesets
{
    public partial class CatchObjectInspectorRuleset : DrawableCatchEditorRuleset, IDrawableInspectionRuleset
    {
        private readonly CatchDifficultyHitObject[] difficultyHitObjects;

        [Resolved]
        private ObjectDifficultyValuesContainer difficultyValuesContainer { get; set; }

        public CatchObjectInspectorRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods, ExtendedCatchDifficultyCalculator difficultyCalculator, double clockRate)
            : base(ruleset, beatmap, mods)
        {
            difficultyHitObjects = difficultyCalculator.GetDifficultyHitObjects(beatmap, clockRate)
                                                       .Cast<CatchDifficultyHitObject>().ToArray();
        }

        public override bool PropagatePositionalInputSubTree => false;

        public override bool PropagateNonPositionalInputSubTree => false;

        public override bool AllowBackwardsSeeks => true;

        public InspectBlueprintContainer CreateBindInspectBlueprintContainer()
        {
            var result = new CatchInspectBlueprintContainer(Playfield);
            result.SelectedItem.BindValueChanged(value =>
                difficultyValuesContainer.CurrentDifficultyHitObject.Value = difficultyHitObjects.FirstOrDefault(x => x.BaseObject == value.NewValue));
            return result;
        }

        protected override void Update()
        {
            base.Update();
            // difficultyValuesContainer.CurrentDifficultyHitObject.Value = difficultyHitObjects.LastOrDefault(x => x.StartTime < Clock.CurrentTime);
        }

        private partial class CatchObjectInspectorPlayfield : CatchEditorPlayfield
        {
            protected override GameplayCursorContainer CreateCursor() => null!;

            public CatchObjectInspectorPlayfield(IBeatmapDifficultyInfo difficulty)
                : base(difficulty)
            {
                DisplayJudgements.Value = false;
                //AddInternal(new Container
                //{
                //    RelativeSizeAxes = Axes.X,
                //    Y = 440,
                //    Height = 6.0f,
                //    CornerRadius = 4.0f,
                //    Masking = true,
                //    Child = new Box
                //    {
                //        Colour = OsuColour.Gray(0.5f),
                //        RelativeSizeAxes = Axes.Both
                //    }
                //});
            }
        }
    }
}
