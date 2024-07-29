#nullable enable

using osu.Game.Rulesets.Osu.Edit.Blueprints.HitCircles.Components;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;
using osu.Framework.Allocation;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Osu
{
    public partial class SelectableHitCircle : OsuSelectableHitObject<HitCircle>
    {
        private HitCirclePiece circlePiece;

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = circlePiece = new HitCirclePiece();
            UpdateFromHitObject();
        }

        public override void UpdateFromHitObject()
        {
            if (HitObject != null)
                circlePiece.UpdateFrom((HitCircle)HitObject);
        }


        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => circlePiece.ReceivePositionalInputAt(screenSpacePos);
    }
}
