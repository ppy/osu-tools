#nullable enable

using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders.Components;
using osu.Game.Rulesets.Osu.Objects;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osuTK;
using osu.Game.Rulesets.Osu.Edit.Blueprints.HitCircles.Components;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Osu
{
    public partial class SelectableSlider : OsuSelectableHitObject<Slider>
    {
        private SliderBodyPiece bodyPiece;
        private HitCirclePiece headOverlay;
        private HitCirclePiece tailOverlay;

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                bodyPiece = new SliderBodyPiece(),
                headOverlay = new HitCirclePiece(),
                tailOverlay = new HitCirclePiece(),
            };
        }

        protected override void Update()
        {
            base.Update();

            if (HitObject == null) return;

            var slider = (Slider)HitObject;
            bodyPiece.UpdateFrom(slider);
            headOverlay.UpdateFrom(slider.HeadCircle);
            tailOverlay.UpdateFrom(slider.TailCircle);
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => bodyPiece.ReceivePositionalInputAt(screenSpacePos);
    }
}
