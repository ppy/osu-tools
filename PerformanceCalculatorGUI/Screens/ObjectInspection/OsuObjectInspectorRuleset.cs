// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.UI;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class OsuObjectInspectorRuleset : DrawableOsuRuleset
    {
        private readonly OsuDifficultyHitObject[] difficultyHitObjects;

        [Resolved]
        private ObjectDifficultyValuesContainer debugValueList { get; set; }

        private DifficultyHitObject lasthit;

        public OsuObjectInspectorRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods, ExtendedOsuDifficultyCalculator difficultyCalculator, double clockRate)
            : base(ruleset, beatmap, mods)
        {
            difficultyHitObjects = difficultyCalculator.GetDifficultyHitObjects(beatmap, clockRate).Select(x => (OsuDifficultyHitObject)x).ToArray();
        }

        protected override void Update()
        {
            base.Update();
            var returnedhit = ObjectInspector.GetCurrentHit(Playfield, difficultyHitObjects, lasthit, Clock);
            if (returnedhit != null)
            {
                lasthit = returnedhit;
                UpdateDebugList(debugValueList, lasthit);
            }
        }

        public override bool PropagatePositionalInputSubTree => false;

        public override bool PropagateNonPositionalInputSubTree => false;

        protected override Playfield CreatePlayfield() => new OsuObjectInspectorPlayfield(difficultyHitObjects);

        public void UpdateDebugList(ObjectDifficultyValuesContainer valueList, DifficultyHitObject curDiffHit)
        {
            OsuDifficultyHitObject osuDiffHit = (OsuDifficultyHitObject)curDiffHit;
            OsuHitObject baseHit = (OsuHitObject)osuDiffHit.BaseObject;

            string groupName = osuDiffHit.BaseObject.GetType().Name;
            valueList.AddGroup(groupName, new string[] { "Slider", "HitCircle", "Spinner" });
            valueList.SetValue(groupName, "Position", baseHit.StackedPosition);
            valueList.SetValue(groupName, "Strain Time", osuDiffHit.StrainTime);
            valueList.SetValue(groupName, "Aim Difficulty", AimEvaluator.EvaluateDifficultyOf(osuDiffHit, true));
            valueList.SetValue(groupName, "Speed Difficulty", SpeedEvaluator.EvaluateDifficultyOf(osuDiffHit));
            valueList.SetValue(groupName, "Rhythm Diff", RhythmEvaluator.EvaluateDifficultyOf(osuDiffHit));
            valueList.SetValue(groupName, "Flashlight Diff", FlashlightEvaluator.EvaluateDifficultyOf(osuDiffHit, false));

            if (osuDiffHit.Angle is not null)
                valueList.SetValue(groupName, "Angle", MathUtils.RadiansToDegrees(osuDiffHit.Angle.Value));

            if (osuDiffHit.BaseObject is Slider)
            {
                valueList.SetValue(groupName, "FL Travel Time", FlashlightEvaluator.EvaluateDifficultyOf(osuDiffHit, false));
                valueList.SetValue(groupName, "Travel Time", osuDiffHit.TravelTime);
                valueList.SetValue(groupName, "Travel Distance", osuDiffHit.TravelDistance);
                valueList.SetValue(groupName, "Min Jump Dist", osuDiffHit.MinimumJumpDistance);
                valueList.SetValue(groupName, "Min Jump Time", osuDiffHit.MinimumJumpTime);
            }

            valueList.UpdateValues();
        }

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

            protected override void OnHitObjectAdded(HitObject hitObject)
            {
                base.OnHitObjectAdded(hitObject);
            }

            protected override void OnHitObjectRemoved(HitObject hitObject)
            {
                base.OnHitObjectRemoved(hitObject);
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
                              .FadeOutFromOne()
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
                            hitObject.FadeOut().Expire();
                        break;
                }
            }
        }
    }
}
