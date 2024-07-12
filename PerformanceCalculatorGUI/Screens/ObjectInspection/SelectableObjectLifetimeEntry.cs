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
            LifetimeStart = GetHitObjectStartTime();
            LifetimeEnd = GetHitObjectEndTime();
        }
    }
}
