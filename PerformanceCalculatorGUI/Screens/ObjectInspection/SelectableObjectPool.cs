#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Pooling;
using osu.Framework.Graphics;
using osu.Game.Rulesets.Objects.Pooling;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;
using osu.Framework.Graphics.Performance;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Framework.Extensions.TypeExtensions;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Framework.Input.Events;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK.Input;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Edit;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Bindables;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
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
            bool wasSelectedJustDeselected = false;

            KeyValuePair<SelectableObjectLifetimeEntry, SelectableHitObject>? newSelectedEntry = null;

            foreach (var entry in AliveEntries.OrderBy(pair => pair.Value.GetHitObject().StartTime))
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
            SelectedObject.Value = newSelectedEntry?.Value.GetHitObject();
            return true;
        }

        public abstract SelectableObjectLifetimeEntry CreateEntry(HitObject hitObject);
        public void AddSelectableObject(HitObject hitObject)
        {
            var newEntry = CreateEntry(hitObject);
            Add(newEntry);
        }

        public void RemoveSelectableObject(HitObject hitObject)
        {
            var entry = Entries.First(e => e.HitObject == hitObject);
            Remove(entry);
        }
    }
}
