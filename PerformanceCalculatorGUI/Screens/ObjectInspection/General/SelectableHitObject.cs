#nullable enable

using osu.Framework.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Objects.Pooling;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.General
{
    public abstract partial class SelectableHitObject : PoolableDrawableWithLifetime<SelectableObjectLifetimeEntry>
    {
        public abstract HitObject? HitObject { get; protected set; }

        protected DrawableHitObject? DrawableObject;

        protected override void LoadComplete()
        {
            base.LoadComplete();
            updateState();
        }

        protected override bool ShouldBeAlive => base.ShouldBeAlive || IsSelected;
        public override bool RemoveCompletedTransforms => true; // To prevent selecting when rewinding back
        public override bool HandlePositionalInput => IsSelectable;


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

        protected override void OnApply(SelectableObjectLifetimeEntry entry)
        {
            HitObject = entry.HitObject;
            DrawableObject = entry.DrawableHitObject;
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
        private THitObject? hitObject;
        public override HitObject? HitObject
        {
            get => hitObject;
            protected set => hitObject = (THitObject)value;
        }
    }
}
