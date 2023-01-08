// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Allocation;
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
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using SharpCompress.Common;
using osu.Framework.Utils;
using Remotion.Linq.Clauses.ResultOperators;
using osu.Game.Rulesets.Taiko.Objects;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class OsuObjectInspectorRuleset : DrawableOsuRuleset, IDebugListUpdater
    {
        public const int HIT_OBJECT_FADE_OUT_EXTENSION = 600;

        public readonly OsuDifficultyHitObject[] DifficultyHitObjects;

        public OsuObjectInspectorRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods, ExtendedOsuDifficultyCalculator difficultyCalculator, double clockRate)
            : base(ruleset, beatmap, mods)
        {
            DifficultyHitObjects = difficultyCalculator.GetDifficultyHitObjects(beatmap, clockRate).Select(x => (OsuDifficultyHitObject)x).ToArray();
        }

        [Resolved]
        private DebugValueList debugValueList { get; set; }

        private DifficultyHitObject lasthit;

        protected override void Update()
        {
            base.Update();
            var hitList = DifficultyHitObjects.Where(hit => { return hit.StartTime < Clock.CurrentTime; });
            if (hitList.Count() > 0 && !(hitList.Last() == lasthit))
            {
                var drawHitList = Playfield.AllHitObjects.Where(hit => { return hit.HitObject.StartTime < Clock.CurrentTime; });
                Console.WriteLine(Clock.CurrentTime);
                lasthit = hitList.Last();
                if (drawHitList.Count() > 0)
                {
                    drawHitList.Last().Colour = Colour4.Red;
                }
                UpdateDebugList(debugValueList, lasthit);
            }
        }

        public override bool PropagatePositionalInputSubTree => false;

        public override bool PropagateNonPositionalInputSubTree => false;

        protected override Playfield CreatePlayfield() => new OsuObjectInspectorPlayfield(DifficultyHitObjects);

        public void UpdateDebugList(DebugValueList valueList, DifficultyHitObject curDiffHit)
        {
            Console.WriteLine(curDiffHit.BaseObject.GetType());
            OsuDifficultyHitObject osuDiffHit = (OsuDifficultyHitObject)curDiffHit;
            OsuHitObject baseHit = (OsuHitObject)osuDiffHit.BaseObject;

            string groupName = osuDiffHit.BaseObject.GetType().Name;
            valueList.AddGroup(groupName,new string[] { "Slider", "HitCircle","Spinner" });
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
            valueList.AddGroup("Test");
            valueList.UpdateValues();
        }

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
