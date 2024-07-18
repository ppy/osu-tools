#nullable enable

using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Objects.Pooling;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Framework.Input.Events;
using osuTK.Input;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Bindables;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.General
{
    public abstract partial class SelectableObjectPool : PooledDrawableWithLifetimeContainer<SelectableObjectLifetimeEntry, SelectableHitObject>
    {
        public readonly Bindable<HitObject?> SelectedObject = new();
        public override bool HandlePositionalInput => true;

        protected override bool OnClick(ClickEvent e)
        {
            if (e.Button == MouseButton.Right)
                return false;

            // Variable for handling selection of desired object in the stack (otherwise it will iterate between 2)
            var wasSelectedJustDeselected = false;

            KeyValuePair<SelectableObjectLifetimeEntry, SelectableHitObject>? newSelectedEntry = null;

            foreach (var entry in AliveEntries.OrderBy(pair => pair.Value.HitObject.StartTime))
            {
                var lifetimeEntry = entry.Key;
                var blueprint = entry.Value;

                if (blueprint.IsSelected)
                {
                    lifetimeEntry.KeepAlive = false;
                    blueprint.Deselect();
                    wasSelectedJustDeselected = true;
                    continue;
                }

                if (newSelectedEntry != null && !wasSelectedJustDeselected)
                    continue;

                if (!blueprint.IsHovered)
                    continue;

                newSelectedEntry?.Value.Deselect();
                blueprint.Select();

                newSelectedEntry = entry;


                wasSelectedJustDeselected = false;
            }

            if (newSelectedEntry.IsNotNull()) newSelectedEntry.Value.Key.KeepAlive = true;
            SelectedObject.Value = newSelectedEntry?.Value.HitObject;
            return true;
        }

        public abstract SelectableObjectLifetimeEntry CreateEntry(HitObject hitObject, DrawableHitObject drawableHitObject);
        public void AddSelectableObject(HitObject hitObject, DrawableHitObject drawableHitObject)
        {
            var newEntry = CreateEntry(hitObject, drawableHitObject);
            Add(newEntry);
        }

        public void RemoveSelectableObject(HitObject hitObject)
        {
            var entry = Entries.First(e => e.HitObject == hitObject);
            Remove(entry);
        }
    }
}
