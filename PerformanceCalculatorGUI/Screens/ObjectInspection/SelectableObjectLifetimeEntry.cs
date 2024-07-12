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
using PerformanceCalculatorGUI.Screens.ObjectInspection.ObjectInspectorRulesets;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public abstract class SelectableObjectLifetimeEntry : LifetimeEntry
    {
        public event Action Invalidated;
        public HitObject HitObject { get; private set; }

        private bool wasBound = false;

        public SelectableObjectLifetimeEntry(HitObject hitObject)
        {
            HitObject = hitObject;
            RefreshLifetimes();
        }

        public void BindEvents()
        {
            UnbindEvents();
            //HitObject.DefaultsApplied += onDefaultsApplied;
            wasBound = true;
            RefreshLifetimes();
        }

        public void UnbindEvents()
        {
            if (!wasBound)
                return;

            //HitObject.DefaultsApplied -= onDefaultsApplied;
            wasBound = false;
        }

        private void onDefaultsApplied(HitObject obj) => RefreshLifetimes();

        protected abstract double GetHitObjectStartTime();

        protected abstract double GetHitObjectEndTime();

        protected void RefreshLifetimes()
        {
            LifetimeStart = GetHitObjectStartTime();
            LifetimeEnd = GetHitObjectEndTime();

            Invalidated?.Invoke();
        }
    }
}
