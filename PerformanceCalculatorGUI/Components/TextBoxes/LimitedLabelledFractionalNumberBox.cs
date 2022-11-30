// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Game.Graphics.UserInterface;

namespace PerformanceCalculatorGUI.Components.TextBoxes
{
    public partial class LimitedLabelledFractionalNumberBox : ExtendedLabelledTextBox
    {
        private partial class FractionalNumberBox : OsuTextBox
        {
            protected override bool AllowIme => false;

            protected override bool CanAddCharacter(char character) => character.IsAsciiDigit() || character == CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];

            protected override void OnUserTextAdded(string added)
            {
                base.OnUserTextAdded(added);

                var textToParse = Text;

                if (string.IsNullOrEmpty(Text))
                {
                    textToParse = PlaceholderText.ToString();
                }

                if (double.TryParse(textToParse, out double parsed))
                {
                    if (parsed >= MinValue && parsed <= MaxValue)
                    {
                        Value.Value = parsed;
                        return;
                    }
                }

                DeleteBy(-1);
                NotifyInputError();
            }

            protected override void OnUserTextRemoved(string removed)
            {
                var textToParse = Text;

                if (string.IsNullOrEmpty(Text))
                {
                    textToParse = PlaceholderText.ToString();
                }

                if (double.TryParse(textToParse, out double parsed))
                {
                    Value.Value = parsed;
                    return;
                }

                Value.Value = default;
            }

            public double MaxValue { get; set; }

            public double MinValue { get; set; }

            public Bindable<double> Value { get; } = new Bindable<double>();
        }

        protected override OsuTextBox CreateTextBox() => new FractionalNumberBox();

        public double MaxValue
        {
            set => ((FractionalNumberBox)Component).MaxValue = value;
        }

        public double MinValue
        {
            set => ((FractionalNumberBox)Component).MinValue = value;
        }

        public Bindable<double> Value => ((FractionalNumberBox)Component).Value;
    }
}
