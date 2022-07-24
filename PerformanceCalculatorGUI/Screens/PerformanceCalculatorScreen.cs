// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Screens;

namespace PerformanceCalculatorGUI.Screens
{
    public abstract class PerformanceCalculatorScreen : Screen
    {
        public abstract bool ShouldShowConfirmationDialogOnSwitch { get; }
    }
}
