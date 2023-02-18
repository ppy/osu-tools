// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Catch.Edit;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class CatchObjectInspectorRuleset : DrawableCatchRuleset
    {
        private readonly CatchDifficultyHitObject[] difficultyHitObjects;

        [Resolved]
        private ObjectDifficultyValuesContainer objectDifficultyValuesContainer { get; set; }

        public CatchObjectInspectorRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods, ExtendedCatchDifficultyCalculator difficultyCalculator, double clockRate)
            : base(ruleset, beatmap, mods)
        {
            difficultyHitObjects = difficultyCalculator.GetDifficultyHitObjects(beatmap, clockRate)
                                                       .Cast<CatchDifficultyHitObject>().ToArray();
        }

        public override bool PropagatePositionalInputSubTree => false;

        public override bool PropagateNonPositionalInputSubTree => false;

        protected override Playfield CreatePlayfield() => new CatchObjectInspectorPlayfield(Beatmap.Difficulty);

        protected override void Update()
        {
            base.Update();
            objectDifficultyValuesContainer.CurrentDifficultyHitObject.Value = difficultyHitObjects.LastOrDefault(x => x.StartTime < Clock.CurrentTime);
        }

        private partial class CatchObjectInspectorPlayfield : CatchEditorPlayfield
        {
            protected override GameplayCursorContainer CreateCursor() => null;

            public CatchObjectInspectorPlayfield(IBeatmapDifficultyInfo difficulty)
                : base(difficulty)
            {
                DisplayJudgements.Value = false;
                AddInternal(new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Y = 440,
                    Height = 6.0f,
                    CornerRadius = 4.0f,
                    Masking = true,
                    Child = new Box
                    {
                        Colour = OsuColour.Gray(0.5f),
                        RelativeSizeAxes = Axes.Both
                    }
                });
            }
        }
    }
}
