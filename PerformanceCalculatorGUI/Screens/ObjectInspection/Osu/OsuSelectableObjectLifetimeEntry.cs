#nullable enable

using osu.Framework.Graphics.Performance;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Osu
{
    public class OsuSelectableObjectLifetimeEntry : LifetimeEntry
    {
        private const int hit_object_fade_out_extension = 200;
        public OsuHitObject HitObject { get; private set; }

        public OsuSelectableObjectLifetimeEntry(OsuHitObject hitObject)
        {
            HitObject = hitObject;
            RefreshLifetimes();
        }

        protected void RefreshLifetimes()
        {
            SetLifetimeStart(HitObject.StartTime - HitObject.TimePreempt);
            SetLifetimeEnd(HitObject.GetEndTime() + hit_object_fade_out_extension);
        }

        // The lifetime, as set by the hitobject.
        private double realLifetimeStart = double.MinValue;
        private double realLifetimeEnd = double.MaxValue;

        // This method is called even if `start == LifetimeStart` when `KeepAlive` is true (necessary to update `realLifetimeStart`).
        protected override void SetLifetimeStart(double start)
        {
            realLifetimeStart = start;
            if (!keepAlive)
                base.SetLifetimeStart(start);
        }

        protected override void SetLifetimeEnd(double end)
        {
            realLifetimeEnd = end;
            if (!keepAlive)
                base.SetLifetimeEnd(end);
        }

        private bool keepAlive;

        public bool KeepAlive
        {
            set
            {
                if (keepAlive == value)
                    return;

                keepAlive = value;
                if (keepAlive)
                    SetLifetime(double.MinValue, double.MaxValue);
                else
                    SetLifetime(realLifetimeStart, realLifetimeEnd);
            }
        }
    }
}
