// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Objects.Pooling;
using osu.Game.Rulesets.Osu.Objects;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Osu
{
    public abstract partial class OsuSelectableHitObject : PoolableDrawableWithLifetime<OsuSelectableObjectLifetimeEntry>
    {
        public abstract OsuHitObject? HitObject { get; protected set; }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            UpdateState();
        }

        public abstract void UpdateFromHitObject();

        protected override void OnApply(OsuSelectableObjectLifetimeEntry entry)
        {
            HitObject = entry.HitObject;
            UpdateFromHitObject();
        }

        #region Selection Logic

        protected override bool ShouldBeAlive => base.ShouldBeAlive || IsSelected;
        public override bool RemoveCompletedTransforms => true; // To prevent selecting when rewinding back
        public override bool HandlePositionalInput => ShouldBeAlive || IsPresent;

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
                    UpdateState();
            }
        }

        public void UpdateState()
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

        protected void OnDeselected()
        {
            foreach (var d in InternalChildren)
                d.Hide();
        }

        protected void OnSelected()
        {
            foreach (var d in InternalChildren)
                d.Show();
        }

        //protected override bool ShouldBeConsideredForInput(Drawable child) => State == SelectionState.Selected;
        public void Select() => State = SelectionState.Selected;
        public void Deselect() => State = SelectionState.NotSelected;
        public bool IsSelected => State == SelectionState.Selected;

        #endregion
    }

    public abstract partial class OsuSelectableHitObject<THitObject> : OsuSelectableHitObject
        where THitObject : OsuHitObject
    {
        private THitObject? hitObject;

        public override OsuHitObject? HitObject
        {
            get => hitObject;
            protected set => hitObject = (THitObject?)value;
        }
    }
}
