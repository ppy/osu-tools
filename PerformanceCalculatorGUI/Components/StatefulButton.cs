// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;

namespace PerformanceCalculatorGUI.Components
{
    public enum ButtonState
    {
        Initial,
        Loading,
        Done
    }

    public partial class StatefulButton : RoundedButton
    {
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; }

        [Resolved]
        private OsuColour colours { get; set; }

        public readonly Bindable<ButtonState> State = new Bindable<ButtonState>();

        private readonly string initialText;

        public StatefulButton(string initialText)
        {
            this.initialText = initialText;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Background.Colour = colourProvider.Background1;
            State.BindValueChanged(updateState, true);
        }

        private void updateState(ValueChangedEvent<ButtonState> state)
        {
            switch (state.NewValue)
            {
                case ButtonState.Initial:
                    Background.FadeColour(colourProvider.Background1, 500, Easing.InOutExpo);
                    Text = initialText;
                    break;

                case ButtonState.Loading:
                    Background.FadeColour(colours.Gray4, 500, Easing.InOutExpo);
                    Text = "Loading...";
                    break;

                case ButtonState.Done:
                    Background.FadeColour(colours.Green, 500, Easing.InOutExpo);
                    Text = "Done!";
                    Scheduler.AddDelayed(() => { State.Value = ButtonState.Initial; }, 1500);
                    break;
            }
        }
    }
}
