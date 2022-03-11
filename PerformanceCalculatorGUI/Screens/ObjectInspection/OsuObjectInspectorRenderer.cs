// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Pooling;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Pooling;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    internal class OsuObjectInspectorRenderer : PooledDrawableWithLifetimeContainer<OsuObjectInspectorLifetimeEntry, OsuObjectInspectorDrawable>
    {
        private DrawablePool<OsuObjectInspectorDrawable> pool;

        private readonly List<OsuObjectInspectorLifetimeEntry> lifetimeEntries = new();
        private readonly Dictionary<HitObject, IBindable> startTimeMap = new();

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                pool = new DrawablePool<OsuObjectInspectorDrawable>(1, 200)
            };
        }

        public void AddDifficultyDataPanel(OsuHitObject hitObject, OsuDifficultyHitObject difficultyHitObject)
        {
            addEntry(hitObject, difficultyHitObject);

            var startTimeBindable = hitObject.StartTimeBindable.GetBoundCopy();
            startTimeBindable.ValueChanged += _ => onStartTimeChanged(hitObject, difficultyHitObject);
            startTimeMap[hitObject] = startTimeBindable;
        }

        public void RemoveDifficultyDataPanel(OsuHitObject hitObject)
        {
            removeEntry(hitObject);

            startTimeMap[hitObject].UnbindAll();
            startTimeMap.Remove(hitObject);
        }

        private void addEntry(OsuHitObject hitObject, OsuDifficultyHitObject difficultyHitObject)
        {
            var newEntry = new OsuObjectInspectorLifetimeEntry(hitObject, difficultyHitObject);
            lifetimeEntries.Add(newEntry);
            Add(newEntry);
        }

        private void removeEntry(OsuHitObject hitObject)
        {
            int index = lifetimeEntries.FindIndex(e => e.HitObject == hitObject);

            var entry = lifetimeEntries[index];
            entry.UnbindEvents();

            lifetimeEntries.RemoveAt(index);
            Remove(entry);
        }

        protected override OsuObjectInspectorDrawable GetDrawable(OsuObjectInspectorLifetimeEntry entry)
        {
            var connection = pool.Get();
            connection.Apply(entry);
            return connection;
        }

        private void onStartTimeChanged(OsuHitObject hitObject, OsuDifficultyHitObject difficultyHitObject)
        {
            removeEntry(hitObject);
            addEntry(hitObject, difficultyHitObject);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            foreach (var entry in lifetimeEntries)
                entry.UnbindEvents();
            lifetimeEntries.Clear();
        }
    }
}
