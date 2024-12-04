// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Taiko.UI;
using osuTK;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Taiko
{
    public partial class TaikoSelectableStrongableHitObject : TaikoSelectableHitObject
    {
        private bool isStrong;

        public TaikoSelectableStrongableHitObject()
        {
        }

        public override void UpdateFromHitObject(TaikoHitObject hitObject)
        {
            isStrong = ((TaikoStrongableHitObject)hitObject).IsStrong;
            base.UpdateFromHitObject(hitObject);
        }

        protected override Vector2 GetObjectSize()
        {
            if (isStrong)
                return new Vector2(TaikoStrongableHitObject.DEFAULT_STRONG_SIZE * TaikoPlayfield.BASE_HEIGHT);

            return new Vector2(TaikoHitObject.DEFAULT_SIZE * TaikoPlayfield.BASE_HEIGHT);
        }
    }
}
