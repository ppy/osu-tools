#nullable enable

using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Taiko.UI;
using osuTK;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Taiko
{
    public partial class TaikoSelectableStrongableHitObject : TaikoSelectableHitObject
    {
        private bool isStrong;
        public TaikoSelectableStrongableHitObject() : base()
        {
        }

        public override void UpdateFromHitObject(TaikoHitObject hitObject)
        {
            base.UpdateFromHitObject(hitObject);
            isStrong = ((TaikoStrongableHitObject)hitObject).IsStrong;
        }

        protected override Vector2 GetObjectSize()
        {
            if (isStrong)
                return new Vector2(TaikoStrongableHitObject.DEFAULT_STRONG_SIZE * TaikoPlayfield.BASE_HEIGHT);

            return new Vector2(TaikoHitObject.DEFAULT_SIZE * TaikoPlayfield.BASE_HEIGHT);
        }
    }
}
