using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PerformanceCalculatorGUI.Screens.ObjectInspection.BlueprintContainers;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public interface IDrawableInspectionRuleset
    {
        public abstract InspectBlueprintContainer CreateBindInspectBlueprintContainer();
    }
}
