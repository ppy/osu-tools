#nullable enable

using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders.Components;
using osu.Game.Rulesets.Osu.Objects;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osuTK;
using osu.Game.Rulesets.Osu.Edit.Blueprints.HitCircles.Components;
using osu.Game.Rulesets.Osu.Edit.Blueprints;
using osu.Game.Graphics;
using osu.Game.Rulesets.Osu.Skinning.Default;
using osuTK.Graphics;
using System.Collections.Generic;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Osu
{
    public partial class SelectableSlider : OsuSelectableHitObject<Slider>
    {
        private CustomSliderBodyPiece bodyPiece;
        private HitCirclePiece headOverlay;
        private HitCirclePiece tailOverlay;

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                bodyPiece = new CustomSliderBodyPiece(),
                headOverlay = new HitCirclePiece(),
                tailOverlay = new HitCirclePiece(),
            };
            UpdateFromHitObject();
        }

        public override void UpdateFromHitObject()
        {
            if (HitObject == null) return;

            var slider = (Slider)HitObject;

            bodyPiece.UpdateFrom(slider);
            headOverlay.UpdateFrom(slider.HeadCircle);
            tailOverlay.UpdateFrom(slider.TailCircle);
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => bodyPiece.ReceivePositionalInputAt(screenSpacePos);

        private partial class CustomSliderBodyPiece : BlueprintPiece<Slider>
        {
            private readonly ManualSliderBody body;

            public CustomSliderBodyPiece()
            {
                AutoSizeAxes = Axes.Both;

                AlwaysPresent = true;

                InternalChild = body = new ManualSliderBody
                {
                    AccentColour = Color4.Transparent
                };
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                body.BorderColour = colours.Yellow;
            }

            public override void UpdateFrom(Slider hitObject)
            {
                base.UpdateFrom(hitObject);

                body.PathRadius = hitObject.Scale * OsuHitObject.OBJECT_RADIUS;

                var vertices = new List<Vector2>();
                hitObject.Path.GetPathToProgress(vertices, 0, 1);

                body.SetVertices(vertices);

                OriginPosition = body.PathOffset;
            }

            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => body.ReceivePositionalInputAt(screenSpacePos);
        }
    }
}
