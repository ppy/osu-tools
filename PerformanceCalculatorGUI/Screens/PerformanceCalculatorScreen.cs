// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Containers;

namespace PerformanceCalculatorGUI.Screens
{
    public abstract class PerformanceCalculatorScreen : CompositeDrawable
    {
        public abstract bool ShouldShowConfirmationDialogOnSwitch { get; }
    }
}
