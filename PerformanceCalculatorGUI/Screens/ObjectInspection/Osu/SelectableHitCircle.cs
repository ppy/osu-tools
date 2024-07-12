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
using TagLib.Id3v2;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Osu
{
    public partial class SelectableHitCircle : OsuSelectableHitObject<HitCircle>
    {
        public readonly HitCirclePiece CirclePiece;

        public SelectableHitCircle()
        {
            InternalChild = CirclePiece = new HitCirclePiece();
        }
        protected override void Update()
        {
            base.Update();
            CirclePiece.UpdateFrom(HitObject);
        }
        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => CirclePiece.ReceivePositionalInputAt(screenSpacePos);
    }
}
