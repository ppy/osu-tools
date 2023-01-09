// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Performance;
using osu.Framework.Utils;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Pooling;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class OsuObjectInspectorDrawable : PoolableDrawableWithLifetime<OsuObjectInspectorLifetimeEntry>
    {
        [Resolved]
        private DebugValueList valueList { get; set; }
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
