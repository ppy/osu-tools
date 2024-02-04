// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.UI;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class OsuObjectInspectorRuleset : DrawableOsuRuleset
    {
        private readonly OsuDifficultyHitObject[] difficultyHitObjects;

        [Resolved]
        private ObjectDifficultyValuesContainer objectDifficultyValuesContainer { get; set; }

        public OsuObjectInspectorRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods, ExtendedOsuDifficultyCalculator difficultyCalculator, double clockRate)
            : base(ruleset, beatmap, mods)
        {
            difficultyHitObjects = difficultyCalculator.GetDifficultyHitObjects(beatmap, clockRate).Cast<OsuDifficultyHitObject>().ToArray();
        }

        protected override void Update()
        {
            base.Update();
            objectDifficultyValuesContainer.CurrentDifficultyHitObject.Value = difficultyHitObjects.LastOrDefault(x => x.StartTime < Clock.CurrentTime);
        }

        public override bool PropagatePositionalInputSubTree => false;

        public override bool PropagateNonPositionalInputSubTree => false;

        protected override Playfield CreatePlayfield() => new OsuObjectInspectorPlayfield(difficultyHitObjects);

        private partial class OsuObjectInspectorPlayfield : OsuPlayfield
        {
            private readonly IReadOnlyList<OsuDifficultyHitObject> difficultyHitObjects;
            protected override GameplayCursorContainer CreateCursor() => null;

            public OsuObjectInspectorPlayfield(IReadOnlyList<OsuDifficultyHitObject> difficultyHitObjects)
            {
                this.difficultyHitObjects = difficultyHitObjects;
                HitPolicy = new AnyOrderHitPolicy();
                DisplayJudgements.Value = false;
            }

            protected override void OnNewDrawableHitObject(DrawableHitObject d)
            {
                d.ApplyCustomUpdateState += updateState;
            }

            private void updateState(DrawableHitObject hitObject, ArmedState state)
            {
                if (state == ArmedState.Idle)
                    return;

                if (hitObject is DrawableSliderRepeat repeat)
                {
                    repeat.Arrow.ApplyTransformsAt(hitObject.StateUpdateTime, true);
                    repeat.Arrow.ClearTransformsAfter(hitObject.StateUpdateTime, true);
                }

                // adjust the visuals of top-level object types to make them stay on screen for longer than usual.
                switch (hitObject)
                {
                    case DrawableSlider:
                    case DrawableHitCircle:
                        var nextHitObject = difficultyHitObjects.FirstOrDefault(x => x.StartTime > hitObject.StartTimeBindable.Value)?.BaseObject;

                        if (nextHitObject != null)
                        {
                            // Get the existing fade out transform
                            var existing = hitObject.Transforms.LastOrDefault(t => t.TargetMember == nameof(Alpha));

                            if (existing == null)
                                return;

                            hitObject.RemoveTransform(existing);

                            using (hitObject.BeginAbsoluteSequence(hitObject.StartTimeBindable.Value))
                                hitObject.Delay(nextHitObject.StartTime - hitObject.StartTimeBindable.Value).FadeOut().Expire();
                        }

                        break;
                }
            }
        }
    }
}
