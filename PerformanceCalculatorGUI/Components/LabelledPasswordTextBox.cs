// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;

namespace PerformanceCalculatorGUI.Components
{
    internal class LabelledPasswordTextBox : LabelledTextBox
    {
        protected override OsuTextBox CreateTextBox() => new OsuPasswordTextBox();
    }
}
