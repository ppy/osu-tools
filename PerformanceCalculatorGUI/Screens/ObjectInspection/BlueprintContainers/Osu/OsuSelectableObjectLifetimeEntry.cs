using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Edit.Blueprints.HitCircles.Components;
using osu.Game.Rulesets.Osu.Objects;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.BlueprintContainers.Osu
{
    public partial class OsuSelectableObjectLifetimeEntry : SelectableObjectLifetimeEntry
    {
        public OsuSelectableObjectLifetimeEntry(OsuHitObject hitObject) : base(hitObject)
        {
        }

        public new OsuHitObject HitObject => (OsuHitObject)base.HitObject;

        protected override double GetHitObjectStartTime() => HitObject.StartTime - HitObject.TimePreempt;
        protected override double GetHitObjectEndTime() => HitObject.GetEndTime() + HitCircleOverlapMarker.FADE_OUT_EXTENSION;
    }
}
