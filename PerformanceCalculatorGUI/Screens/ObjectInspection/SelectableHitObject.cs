using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Objects.Pooling;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;
using TagLib.Ape;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public abstract partial class SelectableHitObject : PoolableDrawableWithLifetime<SelectableObjectLifetimeEntry>
    {
        public abstract HitObject GetHitObject();
        public override bool HandlePositionalInput => IsSelectable;

        protected override void LoadComplete()
        {
            base.LoadComplete();
            updateState();
        }

        protected override bool ShouldBeAlive => base.ShouldBeAlive || IsSelected;
        public override bool RemoveCompletedTransforms => true; // To prevent selecting when rewinding back

        private SelectionState state;

        public SelectionState State
        {
            get => state;
            set
            {
                if (state == value)
                    return;

                state = value;

                if (IsLoaded)
                    updateState();
            }
        }

        private void updateState()
        {
            switch (state)
            {
                case SelectionState.Selected:
                    OnSelected();
                    break;

                case SelectionState.NotSelected:
                    OnDeselected();
                    break;
            }
        }

        protected virtual void OnDeselected()
        {
            // selection blueprints are AlwaysPresent while the related item is visible
            // set the body piece's alpha directly to avoid arbitrarily rendering frame buffers etc. of children.
            foreach (var d in InternalChildren)
                d.Hide();
        }

        protected virtual void OnSelected()
        {
            foreach (var d in InternalChildren)
                d.Show();
        }

        protected override bool ShouldBeConsideredForInput(Drawable child) => State == SelectionState.Selected;
        public void Select() => State = SelectionState.Selected;
        public void Deselect() => State = SelectionState.NotSelected;
        public void ToggleSelection() => State = IsSelected ? SelectionState.NotSelected : SelectionState.Selected;
        public bool IsSelected => State == SelectionState.Selected;
        public virtual bool IsSelectable => ShouldBeAlive && IsPresent;
    }
    public abstract partial class SelectableHitObject<THitObject> : SelectableHitObject
        where THitObject : HitObject
    {
        public THitObject HitObject;
        public override HitObject GetHitObject() => HitObject;
    }
}
