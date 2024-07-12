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

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.BlueprintContainers
{
    public abstract partial class SelectableHitObject : PoolableDrawableWithLifetime<SelectableObjectLifetimeEntry>
    {
        //public DrawableHitObject DrawableObject;

        /// <summary>
        /// Invoked when this <see cref="SelectionBlueprint{T}"/> has been selected.
        /// </summary>
        //public event Action<SelectionBlueprint<T>> Selected;

        /// <summary>
        /// Invoked when this <see cref="SelectionBlueprint{T}"/> has been deselected.
        /// </summary>
        //public event Action<SelectionBlueprint<T>> Deselected;

        public override bool HandlePositionalInput => IsSelectable;
        //public override bool RemoveWhenNotAlive => false;

        public SelectableHitObject()
        {
            //updateState();
            //AutoSizeAxes = Axes.Both;
            //AlwaysPresent = true;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            updateState();
        }

        private SelectionState state;

        //[CanBeNull]
        //public event Action<SelectionState> StateChanged;

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

                //StateChanged?.Invoke(state);
            }
        }

        private void updateState()
        {
            switch (state)
            {
                case SelectionState.Selected:
                    OnSelected();
                    //Selected?.Invoke(this);
                    break;

                case SelectionState.NotSelected:
                    OnDeselected();
                    //Deselected?.Invoke(this);
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

        // When not selected, input is only required for the blueprint itself to receive IsHovering
        protected override bool ShouldBeConsideredForInput(Drawable child) => State == SelectionState.Selected;

        public void Select() => State = SelectionState.Selected;
        public void Deselect() => State = SelectionState.NotSelected;

        /// <summary>
        /// Toggles the selection state of this <see cref="HitObjectSelectionBlueprint"/>.
        /// </summary>
        public void ToggleSelection() => State = IsSelected ? SelectionState.NotSelected : SelectionState.Selected;

        public bool IsSelected => State == SelectionState.Selected;

        public virtual bool IsSelectable => ShouldBeAlive && IsPresent;

        public virtual Vector2 ScreenSpaceSelectionPoint => ScreenSpaceDrawQuad.Centre;

        //protected virtual Vector2[] ScreenSpaceAdditionalNodes => Array.Empty<Vector2>();

        //public Vector2[] ScreenSpaceSnapPoints => ScreenSpaceAdditionalNodes.Prepend(ScreenSpaceSelectionPoint).ToArray();

        /// <summary>
        /// The screen-space quad that outlines this <see cref="HitObjectSelectionBlueprint"/> for selections.
        /// </summary>
        public virtual Quad SelectionQuad => ScreenSpaceDrawQuad;

        /// <summary>
        /// Handle to perform a partial deletion when the user requests a quick delete (Shift+Right Click).
        /// </summary>
        /// <returns>True if the deletion operation was handled by this blueprint. Returning false will delete the full blueprint.</returns>
        //public virtual bool HandleQuickDeletion() => false;

        //protected override void OnApply(SelectableObjectLifetimeEntry entry)
        //{
        //    base.OnApply(entry);
        //    Deselect();
        //}
        //protected override void OnFree(SelectableObjectLifetimeEntry entry)
        //{
        //    base.OnFree(entry);
        //    Deselect();
        //}
    }
    public abstract partial class SelectableHitObject<THitObject> : SelectableHitObject
        where THitObject : HitObject
    {
        public THitObject HitObject;
    }
}
