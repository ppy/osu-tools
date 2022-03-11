
using osu.Framework.Graphics.Containers;

namespace PerformanceCalculatorGUI.Screens
{
    public abstract class PerformanceCalculatorScreen : CompositeDrawable
    {
        public abstract bool ShouldShowConfirmationDialogOnSwitch { get; }
    }
}
