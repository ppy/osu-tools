using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit.Blueprints.HitCircles.Components;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osuTK;
using osu.Framework.Allocation;
using osu.Game.Configuration;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders.Components;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders;
using osu.Game.Screens.Play;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Osu
{
    public partial class SelectableHitCircle : OsuSelectableHitObject<HitCircle>
    {
        private HitCirclePiece circlePiece;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            InternalChild = circlePiece = new HitCirclePiece();
        }

        protected override void Update()
        {
            base.Update();
            circlePiece.UpdateFrom(HitObject);
        }
        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => circlePiece.ReceivePositionalInputAt(screenSpacePos);
    }
}
