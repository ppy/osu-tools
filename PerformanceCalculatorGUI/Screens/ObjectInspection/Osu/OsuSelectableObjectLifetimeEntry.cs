#nullable enable

using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using PerformanceCalculatorGUI.Screens.ObjectInspection.General;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Osu
{
    public partial class OsuSelectableObjectLifetimeEntry : SelectableObjectLifetimeEntry
    {
        private const int hit_object_fade_out_extension = 500;
        public OsuSelectableObjectLifetimeEntry(OsuHitObject hitObject) : base(hitObject, null)
        {
        }

        public new OsuHitObject HitObject => (OsuHitObject)base.HitObject;

        protected override double GetHitObjectStartTime() => HitObject.StartTime - HitObject.TimePreempt;
        protected override double GetHitObjectEndTime() => HitObject.GetEndTime() + hit_object_fade_out_extension;
    }
}
