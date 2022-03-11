// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Performance;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    internal class OsuObjectInspectorLifetimeEntry : LifetimeEntry
    {
        public event Action? Invalidated;
        public readonly OsuHitObject HitObject;
        public readonly OsuDifficultyHitObject DifficultyHitObject;

        public OsuObjectInspectorLifetimeEntry(OsuHitObject hitObject, OsuDifficultyHitObject difficultyHitObject)
        {
            HitObject = hitObject;
            DifficultyHitObject = difficultyHitObject;
            LifetimeStart = HitObject.StartTime - HitObject.TimeFadeIn;
        }

        private bool wasBound;

        private void bindEvents()
        {
            UnbindEvents();

            // Note: Positions are bound for instantaneous feedback from positional changes from the editor, before ApplyDefaults() is called on hitobjects.
            HitObject.DefaultsApplied += onDefaultsApplied;
            HitObject.PositionBindable.ValueChanged += onPositionChanged;

            wasBound = true;
        }

        public void UnbindEvents()
        {
            if (!wasBound)
                return;

            HitObject.DefaultsApplied -= onDefaultsApplied;
            HitObject.PositionBindable.ValueChanged -= onPositionChanged;

            wasBound = false;
        }

        private void onDefaultsApplied(HitObject obj) => refreshLifetimes();

        private void onPositionChanged(ValueChangedEvent<Vector2> obj) => refreshLifetimes();

        private void refreshLifetimes()
        {
            if (HitObject is Spinner)
            {
                LifetimeEnd = LifetimeStart;
                return;
            }

            LifetimeStart = HitObject.StartTime - HitObject.TimeFadeIn;
            LifetimeEnd = HitObject.GetEndTime() + DrawableOsuEditorRuleset.EDITOR_HIT_OBJECT_FADE_OUT_EXTENSION;

            Invalidated?.Invoke();
        }
    }
}
