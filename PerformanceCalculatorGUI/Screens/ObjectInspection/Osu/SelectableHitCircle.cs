// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using osu.Game.Rulesets.Osu.Edit.Blueprints.HitCircles.Components;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;
using osu.Framework.Allocation;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Osu
{
    public partial class SelectableHitCircle : OsuSelectableHitObject<HitCircle>
    {
        private HitCirclePiece circlePiece = null!;

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
