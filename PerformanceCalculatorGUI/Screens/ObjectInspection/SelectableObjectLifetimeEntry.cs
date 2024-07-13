using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu.Framework.Graphics.Performance;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public abstract class SelectableObjectLifetimeEntry : LifetimeEntry
    {
        public HitObject HitObject { get; private set; }

        public SelectableObjectLifetimeEntry(HitObject hitObject)
        {
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
        internal bool KeepAlive
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
