// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.UI;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class OsuObjectInspectorRuleset : DrawableOsuRuleset
    {
        public const int HIT_OBJECT_FADE_OUT_EXTENSION = 600;

        private readonly OsuDifficultyHitObject[] difficultyHitObjects;

        public OsuObjectInspectorRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods, ExtendedOsuDifficultyCalculator difficultyCalculator, double clockRate)
            : base(ruleset, beatmap, mods)
        {
            difficultyHitObjects = difficultyCalculator.GetDifficultyHitObjects(beatmap, clockRate).Select(x => (OsuDifficultyHitObject)x).ToArray();
        }

        public override bool PropagatePositionalInputSubTree => false;

        public override bool PropagateNonPositionalInputSubTree => false;

        protected override Playfield CreatePlayfield() => new OsuObjectInspectorPlayfield(difficultyHitObjects);

        private partial class OsuObjectInspectorPlayfield : OsuPlayfield
        {
            private readonly OsuObjectInspectorRenderer objectRenderer;
            private readonly IReadOnlyList<OsuDifficultyHitObject> difficultyHitObjects;

            protected override GameplayCursorContainer CreateCursor() => null;

            public OsuObjectInspectorPlayfield(IReadOnlyList<OsuDifficultyHitObject> difficultyHitObjects)
            {
                this.difficultyHitObjects = difficultyHitObjects;
                HitPolicy = new AnyOrderHitPolicy();
                AddInternal(objectRenderer = new OsuObjectInspectorRenderer { RelativeSizeAxes = Axes.Both });
                DisplayJudgements.Value = false;
            }

            protected override void OnHitObjectAdded(HitObject hitObject)
            {
                base.OnHitObjectAdded(hitObject);
                objectRenderer.AddDifficultyDataPanel((OsuHitObject)hitObject, difficultyHitObjects.FirstOrDefault(x => x.StartTime == hitObject.StartTime));
            }

            protected override void OnHitObjectRemoved(HitObject hitObject)
            {
                base.OnHitObjectRemoved(hitObject);
                objectRenderer.RemoveDifficultyDataPanel((OsuHitObject)hitObject);
            }

            protected override void OnNewDrawableHitObject(DrawableHitObject d)
            {
                d.ApplyCustomUpdateState += updateState;
            }

            private void updateState(DrawableHitObject hitObject, ArmedState state)
            {
                if (state == ArmedState.Idle)
                    return;

                if (hitObject is DrawableHitCircle circle)
                {
                    using (circle.BeginAbsoluteSequence(circle.HitStateUpdateTime))
                    {
                        circle.ApproachCircle
                              .FadeOutFromOne(HIT_OBJECT_FADE_OUT_EXTENSION * 4)
                              .Expire();

                        circle.ApproachCircle.ScaleTo(1.1f, 300, Easing.OutQuint);
                    }
                }

                if (hitObject is DrawableSliderRepeat repeat)
                {
                    repeat.Arrow.ApplyTransformsAt(hitObject.StateUpdateTime, true);
                    repeat.Arrow.ClearTransformsAfter(hitObject.StateUpdateTime, true);
                }

                // adjust the visuals of top-level object types to make them stay on screen for longer than usual.
                switch (hitObject)
                {
                    case DrawableSlider _:
                    case DrawableHitCircle _:
                        // Get the existing fade out transform
                        var existing = hitObject.Transforms.LastOrDefault(t => t.TargetMember == nameof(Alpha));

                        if (existing == null)
                            return;

                        hitObject.RemoveTransform(existing);

                        using (hitObject.BeginAbsoluteSequence(hitObject.HitStateUpdateTime))
                            hitObject.FadeOut(HIT_OBJECT_FADE_OUT_EXTENSION).Expire();
                        break;
                }
            }
        }
    }
}
