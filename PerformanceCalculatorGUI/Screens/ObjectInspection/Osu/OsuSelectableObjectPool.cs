using osu.Framework.Graphics;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Osu
{
    public partial class OsuSelectableObjectPool : SelectableObjectPool
    {
        public override SelectableObjectLifetimeEntry CreateEntry(HitObject hitObject) => new OsuSelectableObjectLifetimeEntry((OsuHitObject)hitObject);

        protected override SelectableHitObject GetDrawable(SelectableObjectLifetimeEntry entry)
        {
            // Potential room for pooling here
            SelectableHitObject result = entry.HitObject switch
            {
                HitCircle circle => new SelectableHitCircle().With(o => o.HitObject = circle),
                Slider slider => new SelectableSlider().With(o => o.HitObject = slider),
                Spinner spinner => null, // Do selectable spinner even needed here?
                _ => null
            };

            return result;
        }
    }
}
