// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
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

        private Bindable<DifficultyHitObject> focusedDiffHitBind;

        public OsuObjectInspectorRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods, ExtendedOsuDifficultyCalculator difficultyCalculator, double clockRate, Bindable<DifficultyHitObject> diffHitBind)
            : base(ruleset, beatmap, mods)
        {
            difficultyHitObjects = difficultyCalculator.GetDifficultyHitObjects(beatmap, clockRate).Cast<OsuDifficultyHitObject>().ToArray();
            focusedDiffHitBind = diffHitBind;
            focusedDiffHitBind.ValueChanged += (ValueChangedEvent<DifficultyHitObject> newHit) => UpdateDebugList(debugValueList, newHit.NewValue);
        }

        protected override void Update()
        {
            base.Update();
            var hitList = difficultyHitObjects.Where(hit => hit.StartTime < Clock.CurrentTime);
            if (hitList.Any() && hitList.Last() != lasthit)
            {
                lasthit = hitList.Last();
                focusedDiffHitBind.Value = lasthit;
            }
            focusedDiffHitBind.Value = null;
        }

        public override bool PropagatePositionalInputSubTree => false;

        public override bool PropagateNonPositionalInputSubTree => false;

        protected override Playfield CreatePlayfield() => new OsuObjectInspectorPlayfield(difficultyHitObjects);

        public void UpdateDebugList(ObjectDifficultyValuesContainer valueList, DifficultyHitObject curDiffHit)
        {
            if (curDiffHit == null) return;

            OsuDifficultyHitObject osuDiffHit = (OsuDifficultyHitObject)curDiffHit;
            OsuHitObject baseHit = (OsuHitObject)osuDiffHit.BaseObject;

            string groupName = osuDiffHit.BaseObject.GetType().Name;
            Dictionary<string, Dictionary<string, object>> infoDict = valueList.InfoDictionary.Value;

            valueList.AddGroup(groupName, new string[] { "Slider", "HitCircle", "Spinner" });
            infoDict[groupName] = new Dictionary<string, object> {
                { "Position", baseHit.StackedPosition },
                { "Strain Time", osuDiffHit.StrainTime },
                { "Aim Difficulty", AimEvaluator.EvaluateDifficultyOf(osuDiffHit, true) },
                { "Speed Difficulty", SpeedEvaluator.EvaluateDifficultyOf(osuDiffHit) },
                { "Rhythm Diff",SpeedEvaluator.EvaluateDifficultyOf(osuDiffHit) },
                { "Flashlight Diff", SpeedEvaluator.EvaluateDifficultyOf(osuDiffHit)},
            };

            if (osuDiffHit.Angle is not null)
                infoDict[groupName].Add("Angle", MathUtils.RadiansToDegrees(osuDiffHit.Angle.Value));

            if (osuDiffHit.BaseObject is Slider)
            {
                infoDict[groupName].Add("FL Travel Time", FlashlightEvaluator.EvaluateDifficultyOf(osuDiffHit, false));
                infoDict[groupName].Add("Travel Time", osuDiffHit.TravelTime);
                infoDict[groupName].Add("Travel Distance", osuDiffHit.TravelDistance);
                infoDict[groupName].Add("Min Jump Dist", osuDiffHit.MinimumJumpDistance);
                infoDict[groupName].Add("Min Jump Time", osuDiffHit.MinimumJumpTime);
            }
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
