// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Performance;
using osu.Framework.Utils;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Pooling;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class OsuObjectInspectorDrawable : PoolableDrawableWithLifetime<OsuObjectInspectorLifetimeEntry>
    {
        protected override void OnApply(OsuObjectInspectorLifetimeEntry entry)
        {
            base.OnApply(entry);

            entry.Invalidated += onEntryInvalidated;
            refresh();
        }

        protected override void OnFree(OsuObjectInspectorLifetimeEntry entry)
        {
            base.OnFree(entry);

            entry.Invalidated -= onEntryInvalidated;
            ClearInternal(false);
        }

        private void onEntryInvalidated() => Scheduler.AddOnce(refresh);

        private void refresh()
        {
            ClearInternal(false);

            var entry = Entry;
            if (entry == null) return;

            var hitObject = entry.HitObject;
            double startTime = hitObject.StartTime - hitObject.TimePreempt;
            double movementTime = hitObject.GetEndTime() - hitObject.StartTime;
            double visibleTime = hitObject.GetEndTime() - startTime;

            ObjectInspectionPanel panel;
            AddInternal(panel = new ObjectInspectionPanel
            {
                Position = hitObject.StackedPosition,
                Alpha = 0
            });

            panel.AddParagraph($"Position: {entry.HitObject.StackedPosition}", 8);

            if (entry.DifficultyHitObject is not null)
            {
                panel.AddParagraph($"Strain Time: {entry.DifficultyHitObject.StrainTime:N3}");
                panel.AddParagraph($"Aim Difficulty: {AimEvaluator.EvaluateDifficultyOf(entry.DifficultyHitObject, true):N3}");
                panel.AddParagraph($"Speed Difficulty: {SpeedEvaluator.EvaluateDifficultyOf(entry.DifficultyHitObject):N3}");
                panel.AddParagraph($"Rhythm Difficulty: {RhythmEvaluator.EvaluateDifficultyOf(entry.DifficultyHitObject):N3}");
                panel.AddParagraph($"Flashlight Difficulty: {FlashlightEvaluator.EvaluateDifficultyOf(entry.DifficultyHitObject, false):N3}");

                if (entry.DifficultyHitObject.Angle is not null)
                    panel.AddParagraph($"Angle: {MathUtils.RadiansToDegrees(entry.DifficultyHitObject.Angle.Value):N3}");

                if (entry.HitObject is Slider)
                {
                    panel.AddParagraph($"Travel Time: {entry.DifficultyHitObject.TravelTime:N3}");
                    panel.AddParagraph($"Travel Distance: {entry.DifficultyHitObject.TravelDistance:N3}");
                    panel.AddParagraph($"Minimum Jump Distance: {entry.DifficultyHitObject.MinimumJumpDistance:N3}");
                    panel.AddParagraph($"Minimum Jump Time: {entry.DifficultyHitObject.MinimumJumpTime:N3}");
                }
            }

            using (panel.BeginAbsoluteSequence(startTime))
            {
                panel.FadeIn(hitObject.TimePreempt);

                if (entry.HitObject is Slider)
                {
                    panel.Delay(hitObject.TimePreempt).MoveTo(hitObject.StackedEndPosition, movementTime);
                }

                panel.Delay(visibleTime).FadeOut(OsuObjectInspectorRuleset.HIT_OBJECT_FADE_OUT_EXTENSION).Expire();
            }

            entry.LifetimeEnd = panel.LifetimeEnd;
        }
    }

    public class OsuObjectInspectorLifetimeEntry : LifetimeEntry
    {
        public event Action Invalidated;
        public readonly OsuHitObject HitObject;
        public readonly OsuDifficultyHitObject DifficultyHitObject;

        public OsuObjectInspectorLifetimeEntry(OsuHitObject hitObject, OsuDifficultyHitObject difficultyHitObject)
        {
            HitObject = hitObject;
            DifficultyHitObject = difficultyHitObject;
            LifetimeStart = HitObject.StartTime - HitObject.TimePreempt;

            bindEvents();
            refreshLifetimes();
        }

        private bool wasBound;

        private void bindEvents()
        {
            UnbindEvents();
            HitObject.DefaultsApplied += onDefaultsApplied;
            wasBound = true;
        }

        public void UnbindEvents()
        {
            if (!wasBound)
                return;

            HitObject.DefaultsApplied -= onDefaultsApplied;

            wasBound = false;
        }

        private void onDefaultsApplied(HitObject obj) => refreshLifetimes();

        private void refreshLifetimes()
        {
            if (HitObject is Spinner)
            {
                LifetimeEnd = LifetimeStart;
                return;
            }

            LifetimeStart = HitObject.StartTime - HitObject.TimePreempt;
            LifetimeEnd = HitObject.GetEndTime() + OsuObjectInspectorRuleset.HIT_OBJECT_FADE_OUT_EXTENSION;

            Invalidated?.Invoke();
        }
    }
}
