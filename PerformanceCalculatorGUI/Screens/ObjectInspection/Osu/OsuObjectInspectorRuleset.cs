// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.UI;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Osu
{
    public partial class OsuObjectInspectorRuleset : DrawableOsuEditorRuleset
    {
        private readonly OsuDifficultyHitObject[] difficultyHitObjects;

        [Resolved]
        private ObjectDifficultyValuesContainer difficultyValuesContainer { get; set; }

        public OsuObjectInspectorRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods, ExtendedOsuDifficultyCalculator difficultyCalculator, double clockRate)
            : base(ruleset, beatmap, mods)
        {
            difficultyHitObjects = difficultyCalculator.GetDifficultyHitObjects(beatmap, clockRate).Cast<OsuDifficultyHitObject>().ToArray();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            KeyBindingInputManager.AllowGameplayInputs = false;
            ((OsuObjectInspectorPlayfield)Playfield).Pool.SelectedObject.BindValueChanged(value =>
                difficultyValuesContainer.CurrentDifficultyHitObject.Value = difficultyHitObjects.FirstOrDefault(x => x.BaseObject == value.NewValue));
        }

        public override bool PropagatePositionalInputSubTree => true;
        public override bool PropagateNonPositionalInputSubTree => false;

        public override bool AllowBackwardsSeeks => true;

        protected override Playfield CreatePlayfield() => new OsuObjectInspectorPlayfield(difficultyHitObjects);

        private partial class OsuObjectInspectorPlayfield : OsuPlayfield
        {
            private readonly IReadOnlyList<OsuDifficultyHitObject> difficultyHitObjects;

            public OsuSelectableObjectPool Pool { get; private set; }

            public OsuObjectInspectorPlayfield(IReadOnlyList<OsuDifficultyHitObject> difficultyHitObjects)
            {
                this.difficultyHitObjects = difficultyHitObjects;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                AddInternal(Pool = new OsuSelectableObjectPool { RelativeSizeAxes = Axes.Both });
                HitPolicy = new AnyOrderHitPolicy();
                DisplayJudgements.Value = false;
            }

            protected override GameplayCursorContainer CreateCursor() => null;

            protected override void OnHitObjectAdded(HitObject hitObject)
            {
                base.OnHitObjectAdded(hitObject);

                if (hitObject is Spinner) return;

                Pool.AddSelectableObject((OsuHitObject)hitObject);
            }

            protected override void OnHitObjectRemoved(HitObject hitObject)
            {
                base.OnHitObjectRemoved(hitObject);

                if (hitObject is Spinner) return;

                Pool.RemoveSelectableObject((OsuHitObject)hitObject);
            }

            protected override void OnNewDrawableHitObject(DrawableHitObject d)
            {
                base.OnNewDrawableHitObject(d);
                d.ApplyCustomUpdateState += updateState;
            }

            private void updateState(DrawableHitObject hitObject, ArmedState state)
            {
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
                            {
                                var hitObjectDuration = hitObject.HitObject.GetEndTime() - hitObject.StartTimeBindable.Value;

                                hitObject.Delay(hitObjectDuration)
                                         .FadeTo(0.25f, 200f, Easing.Out)
                                         .Delay(nextHitObject.StartTime - hitObject.StartTimeBindable.Value - hitObjectDuration)
                                         .FadeOut(100f, Easing.Out)
                                         .Expire();
                            }
                        }

                        break;
                }
            }
        }
    }
}
