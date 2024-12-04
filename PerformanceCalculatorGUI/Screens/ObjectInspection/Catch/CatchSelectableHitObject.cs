// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using osu.Framework.Allocation;
using osu.Game.Rulesets.Catch.Objects.Drawables;
using osu.Game.Rulesets.Catch.Objects;
using osuTK;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Osu.Edit.Blueprints.HitCircles.Components;
using osu.Framework.Input.Events;
using osuTK.Input;
using osu.Framework.Bindables;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Catch
{
    public partial class CatchSelectableHitObject : DrawableCatchHitObject
    {
        // This is HitCirclePiece instead of FruitOutline because FruitOutline doesn't register input for some reason
        private HitCirclePiece outline = null!;
        private SelectionState state;

        public readonly Bindable<CatchSelectableHitObject?> PlayfieldSelectedObject = new Bindable<CatchSelectableHitObject?>();

        public CatchSelectableHitObject(CatchHitObject hitObject)
            : base(hitObject)
        {
            X = hitObject.EffectiveX;
            state = SelectionState.NotSelected;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(outline = new HitCirclePiece
            {
                Alpha = 0,
                Scale = HitObject is Droplet ? new Vector2(HitObject.Scale) * 0.5f : new Vector2(HitObject.Scale)
            });

            PlayfieldSelectedObject.BindValueChanged(x =>
            {
                if (x.NewValue != this)
                {
                    Deselect();
                }
            });
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
            outline.Show();
            PlayfieldSelectedObject.Value = this;

            return true;
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => outline.ReceivePositionalInputAt(screenSpacePos);

        public override bool HandlePositionalInput => ShouldBeAlive || IsPresent;

        public void Deselect()
        {
            if (IsLoaded)
            {
                state = SelectionState.NotSelected;
                outline.Hide();
            }
        }
    }
}
