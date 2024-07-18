#nullable enable

using System.Diagnostics;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Objects;
using PerformanceCalculatorGUI.Screens.ObjectInspection.General;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Osu
{
    public partial class OsuSelectableObjectPool : SelectableObjectPool
    {
        public override SelectableObjectLifetimeEntry CreateEntry(HitObject hitObject, DrawableHitObject _) => new OsuSelectableObjectLifetimeEntry((OsuHitObject)hitObject);

        protected override SelectableHitObject GetDrawable(SelectableObjectLifetimeEntry entry)
        {
            // Potential room for pooling here
            SelectableHitObject? result = entry.HitObject switch
            {
                HitCircle => new SelectableHitCircle(),
                Slider => new SelectableSlider(),
                Spinner => null, // Do selectable spinner even needed here?
                _ => null
            };

            // Entry shouldn't be create for not supported hitobject types
            Debug.Assert(result != null);

            result.Apply(entry);
            return result;
        }
    }
}
