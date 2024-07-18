#nullable enable

using osu.Framework.Graphics.Performance;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.General
{
    public abstract class SelectableObjectLifetimeEntry : LifetimeEntry
    {
        public DrawableHitObject? DrawableHitObject { get; private set; }
        public HitObject HitObject { get; private set; }

        public SelectableObjectLifetimeEntry(HitObject hitObject, DrawableHitObject? drawableHitObject)
        {
            DrawableHitObject = drawableHitObject;
            HitObject = hitObject;
            RefreshLifetimes();
        }

        protected abstract double GetHitObjectStartTime();

        protected abstract double GetHitObjectEndTime();

        protected void RefreshLifetimes()
        {
            SetLifetimeStart(GetHitObjectStartTime());
            SetLifetimeEnd(GetHitObjectEndTime());
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

        /// <summary>
        /// Whether the <see cref="HitObject"/> should be kept always alive.
        /// </summary>
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
