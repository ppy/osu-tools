// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Graphics.UserInterfaceV2;

namespace PerformanceCalculatorGUI.Components.TextBoxes
{
    public partial class ExtendedLabelledTextBox : LabelledTextBox
    {
        public bool CommitOnFocusLoss
        {
            get => Component.CommitOnFocusLost;
            set => Component.CommitOnFocusLost = value;
        }
    }
}
