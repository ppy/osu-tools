// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using osu.Framework.Allocation;
using osu.Framework.Bindables;
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
        private SelectionState state;
        private readonly bool isStrong;

        public readonly Bindable<TaikoSelectableHitObject?> PlayfieldSelectedObject = new Bindable<TaikoSelectableHitObject?>();

        public TaikoSelectableHitObject(TaikoHitObject hitObject)
            : base(hitObject)
        {
            Anchor = Anchor.CentreLeft;
            Origin = Anchor.CentreLeft;

            state = SelectionState.NotSelected;
            isStrong = hitObject is TaikoStrongableHitObject;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(hitPiece = new HitPiece
            {
                Alpha = 0,
                Size = getObjectSize()
            });

            PlayfieldSelectedObject.BindValueChanged(x =>
            {
                if (x.NewValue != this)
                {
                    Deselect();
                }
            });
        }

        private Vector2 getObjectSize()
        {
            if (isStrong)
                return new Vector2(TaikoStrongableHitObject.DEFAULT_STRONG_SIZE * TaikoPlayfield.BASE_HEIGHT);

            return new Vector2(TaikoHitObject.DEFAULT_SIZE * TaikoPlayfield.BASE_HEIGHT);
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (e.Button != MouseButton.Left)
                return false;

            if (!IsHovered)
                return false;

            if (state == SelectionState.Selected)
            {
                Deselect();
                PlayfieldSelectedObject.Value = null;

                return true;
            }

            state = SelectionState.Selected;
            hitPiece.Show();
            PlayfieldSelectedObject.Value = this;

            return true;
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => hitPiece.ReceivePositionalInputAt(screenSpacePos);

        public override bool OnPressed(KeyBindingPressEvent<TaikoAction> e) => true;

        public override bool HandlePositionalInput => ShouldBeAlive || IsPresent;

        public void Deselect()
        {
            if (IsLoaded)
            {
                state = SelectionState.NotSelected;
                hitPiece.Hide();
            }
        }
    }
}
