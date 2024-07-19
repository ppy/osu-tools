#nullable enable

using osu.Framework.Allocation;
using osu.Game.Rulesets.Catch.Objects.Drawables;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Catch.Edit.Blueprints.Components;
using osuTK;
using osu.Game.Graphics.UserInterface;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Catch
{
    public partial class CatchSelectableHitObject : DrawableCatchHitObject
    {
        private float x, scale;
        public CatchSelectableHitObject(CatchHitObject hitObject)
            : base(new CatchInspectorHitObject(hitObject))
        {
            x = hitObject.EffectiveX;
            scale = hitObject.Scale;

            if (hitObject is Droplet)
                scale *= 0.5f;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(new FruitOutline()
            {
                X = x,
                Scale = new Vector2(scale)
            });
        }

        private class CatchInspectorHitObject : CatchHitObject
        {
            public CatchInspectorHitObject(HitObject obj)
            {
                StartTime = obj.StartTime;
            }
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
        public void Select() => State = SelectionState.Selected;
        public void Deselect() => State = SelectionState.NotSelected;
        public bool IsSelected => State == SelectionState.Selected;

        #endregion
    }
}
