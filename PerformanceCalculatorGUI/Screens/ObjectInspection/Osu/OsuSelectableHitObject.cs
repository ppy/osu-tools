#nullable enable

using osu.Game.Rulesets.Osu.Objects;
using PerformanceCalculatorGUI.Screens.ObjectInspection.General;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Osu
{
    public abstract partial class OsuSelectableHitObject<THitObject> : SelectableHitObject<THitObject>
        where THitObject : OsuHitObject
    {
    }
}
