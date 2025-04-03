// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.Color4Extensions;
using osu.Game.Graphics.UserInterface;

namespace PerformanceCalculatorGUI.Components.TextBoxes
{
    public partial class ReadonlyOsuTextBox : OsuTextBox
    {
        private readonly string text;
        private readonly bool hasBackground;

        public ReadonlyOsuTextBox(string text, bool hasBackground = true)
        {
            this.text = text;
            this.hasBackground = hasBackground;

            Text = text;
        }

        protected override void LoadComplete()
        {
            if (!hasBackground)
                BackgroundUnfocused = BackgroundUnfocused.Opacity(0);

            base.LoadComplete();
        }

        protected override void OnUserTextAdded(string added)
        {
            NotifyInputError();
            Text = text;
        }

        protected override void OnUserTextRemoved(string removed)
        {
            NotifyInputError();
            Text = text;
        }
    }
}
