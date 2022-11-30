// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Graphics.UserInterface;

namespace PerformanceCalculatorGUI.Components.TextBoxes
{
    public partial class LabelledPasswordTextBox : ExtendedLabelledTextBox
    {
        protected override OsuTextBox CreateTextBox() => new OsuPasswordTextBox();
    }
}
