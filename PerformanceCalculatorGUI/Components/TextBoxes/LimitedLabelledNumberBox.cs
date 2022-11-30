// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;

namespace PerformanceCalculatorGUI.Components.TextBoxes
{
    public partial class LimitedLabelledNumberBox : LabelledNumberBox
    {
        private partial class LimitedNumberBox : OsuNumberBox
        {
            protected override void OnUserTextAdded(string added)
            {
                base.OnUserTextAdded(added);

                var textToParse = Text;

                if (string.IsNullOrEmpty(Text))
                {
                    textToParse = PlaceholderText.ToString();
                }

                if (int.TryParse(textToParse, out int parsed))
                {
                    if (parsed >= (MinValue ?? int.MinValue) && parsed <= (MaxValue ?? int.MaxValue))
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

                if (int.TryParse(textToParse, out int parsed))
                {
                    Value.Value = parsed;
                    return;
                }

                Value.Value = default;
            }

            public int? MaxValue { get; set; }

            public int? MinValue { get; set; }

            public Bindable<int> Value { get; } = new Bindable<int>();
        }

        protected override OsuTextBox CreateTextBox() => new LimitedNumberBox();

        public int? MaxValue
        {
            set => ((LimitedNumberBox)Component).MaxValue = value;
        }

        public int? MinValue
        {
            set => ((LimitedNumberBox)Component).MinValue = value;
        }

        public Bindable<int> Value => ((LimitedNumberBox)Component).Value;

        public bool CommitOnFocusLoss
        {
            get => Component.CommitOnFocusLost;
            set => Component.CommitOnFocusLost = value;
        }

        public LimitedLabelledNumberBox()
        {
            CornerRadius = ExtendedLabelledTextBox.CORNER_RADIUS;
        }
    }
}
