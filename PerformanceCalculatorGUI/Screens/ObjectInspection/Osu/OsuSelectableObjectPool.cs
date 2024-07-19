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
using osu.Game.Rulesets.Osu.Objects;
using System.Diagnostics;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Osu
{
    public partial class OsuSelectableObjectPool : PooledDrawableWithLifetimeContainer<OsuSelectableObjectLifetimeEntry, OsuSelectableHitObject>
    {
        public readonly Bindable<OsuHitObject?> SelectedObject = new();
        public override bool HandlePositionalInput => true;

        protected override bool OnClick(ClickEvent e)
        {
            if (e.Button == MouseButton.Right)
                return false;

            // Variable for handling selection of desired object in the stack (otherwise it will iterate between 2)
            var wasSelectedJustDeselected = false;

            KeyValuePair<OsuSelectableObjectLifetimeEntry, OsuSelectableHitObject>? newSelectedEntry = null;

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

        protected override OsuSelectableHitObject GetDrawable(OsuSelectableObjectLifetimeEntry entry)
        {
            // Potential room for pooling here
            OsuSelectableHitObject? result = entry.HitObject switch
            {
                HitCircle => new SelectableHitCircle(),
                Slider => new SelectableSlider(),
                _ => null
            };

            // Entry shouldn't be create for not supported hitobject types
            Debug.Assert(result != null);

            result.Apply(entry);
            return result;
        }

        public OsuSelectableObjectLifetimeEntry CreateEntry(OsuHitObject hitObject) => new OsuSelectableObjectLifetimeEntry(hitObject);
        public void AddSelectableObject(OsuHitObject hitObject)
        {
            var newEntry = CreateEntry(hitObject);
            Add(newEntry);
        }

        public void RemoveSelectableObject(OsuHitObject hitObject)
        {
            var entry = Entries.First(e => e.HitObject == hitObject);
            Remove(entry);
        }
    }
}
