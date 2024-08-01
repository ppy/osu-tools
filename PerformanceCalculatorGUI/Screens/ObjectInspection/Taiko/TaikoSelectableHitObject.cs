#nullable enable

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Taiko;
using osu.Game.Rulesets.Taiko.Edit.Blueprints;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Taiko.Objects.Drawables;
using osu.Game.Rulesets.Taiko.UI;
using osuTK;
using osuTK.Input;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Taiko
{
    public partial class TaikoSelectableHitObject : DrawableTaikoHitObject
    {
        private HitPiece hitPiece;
        public TaikoSelectableHitObject() : base(new TaikoDummyHitObject())
        {
            Anchor = Anchor.CentreLeft;
            Origin = Anchor.CentreLeft;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(hitPiece = new HitPiece() {Size = GetObjectSize() });
            UpdateState();
        }

        public virtual void UpdateFromHitObject(TaikoHitObject hitObject)
        {
            Deselect();
            HitObject.StartTime = hitObject.StartTime;
        }

        protected virtual Vector2 GetObjectSize() => new Vector2(TaikoHitObject.DEFAULT_SIZE * TaikoPlayfield.BASE_HEIGHT);

        protected override void OnApply()
        {
            base.OnApply();
            UpdateState();
        }
        protected override bool OnClick(ClickEvent e)
        {
            if (e.Button == MouseButton.Right)
                return false;

            if (!IsHovered)
                return false;

            if (IsSelected)
            {
                Deselect();
                Selected.Invoke(null);
                return true;
            }

            Select();
            return true;
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => hitPiece.ReceivePositionalInputAt(screenSpacePos);
            
        public override bool OnPressed(KeyBindingPressEvent<TaikoAction> e) => true;

        private class TaikoDummyHitObject : TaikoHitObject
        {
        }

        #region Selection Logic
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
            Selected.Invoke(this);
        }

        public event Action<TaikoSelectableHitObject?> Selected;

        public void Select() => State = SelectionState.Selected;
        public void Deselect() => State = SelectionState.NotSelected;
        public bool IsSelected => State == SelectionState.Selected;

        #endregion
    }
}
