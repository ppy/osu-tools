#nullable enable

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Taiko;
using osu.Game.Rulesets.Taiko.Edit.Blueprints;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Taiko.Objects.Drawables;
using osu.Game.Rulesets.Taiko.UI;
using osuTK;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Taiko
{
    public partial class TaikoSelectableHitObject : DrawableTaikoHitObject
    {
        private HitPiece hitPiece;
        public TaikoSelectableHitObject(TaikoHitObject hitObject) : base(new TaikoInspectorHitObject(hitObject))
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

        protected virtual Vector2 GetObjectSize() => new Vector2(TaikoHitObject.DEFAULT_SIZE * TaikoPlayfield.BASE_HEIGHT);

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => hitPiece.ReceivePositionalInputAt(screenSpacePos);

        public override bool OnPressed(KeyBindingPressEvent<TaikoAction> e) => true;

        private class TaikoInspectorHitObject : TaikoHitObject
        {
            public TaikoInspectorHitObject(HitObject obj)
            {
                StartTime = obj.StartTime;
            }
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
        }
        public void Select() => State = SelectionState.Selected;
        public void Deselect() => State = SelectionState.NotSelected;
        public bool IsSelected => State == SelectionState.Selected;

        #endregion
    }
}
