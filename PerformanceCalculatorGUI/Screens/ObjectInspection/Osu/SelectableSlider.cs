using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders.Components;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders;
using osu.Game.Rulesets.Osu.Objects;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Configuration;
using osuTK;
using osu.Framework.Graphics.Containers;
using osu.Game.Rulesets.Osu.Edit.Blueprints.HitCircles.Components;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Osu
{
    public partial class SelectableSlider : OsuSelectableHitObject<Slider>
    {
        private Slider slider => HitObject;

        private SliderBodyPiece bodyPiece;
        private HitCirclePiece headOverlay;
        private HitCirclePiece tailOverlay;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
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
            bodyPiece.UpdateFrom(HitObject);
            headOverlay.UpdateFrom(slider.HeadCircle);
            tailOverlay.UpdateFrom(slider.TailCircle);
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => bodyPiece.ReceivePositionalInputAt(screenSpacePos);
    }
}
