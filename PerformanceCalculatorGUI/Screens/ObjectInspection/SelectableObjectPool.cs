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

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public abstract partial class SelectableObjectPool : PooledDrawableWithLifetimeContainer<SelectableObjectLifetimeEntry, SelectableHitObject>
    {
        public abstract SelectableObjectLifetimeEntry CreateEntry(HitObject hitObject);

        public override bool HandlePositionalInput => true;

        protected override bool OnClick(ClickEvent e)
        {
            if (e.Button == MouseButton.Right)
                return false;

            var wasSomethingSelected = false;

            foreach (var blueprint in AliveEntries.Values)
            {
                if (wasSomethingSelected || blueprint.IsSelected)
                {
                    blueprint.Deselect();
                    continue;
                }

                if (!blueprint.IsHovered)
                    continue;

                blueprint.Select();
                wasSomethingSelected = true;
            }

            return true;
        }
        public void AddSelectableObject(HitObject hitObject)
        {
            var newEntry = CreateEntry(hitObject);
            //newEntry.BindEvents();
            Add(newEntry);
        }

        public void RemoveSelectableObject(HitObject hitObject)
        {
            var entry = Entries.First(e => e.HitObject == hitObject);
            //entry.UnbindEvents();
            Remove(entry);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            //foreach (var entry in Entries)
            //    entry.UnbindEvents();
        }
    }
}
