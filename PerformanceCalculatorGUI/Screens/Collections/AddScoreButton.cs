// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;

namespace PerformanceCalculatorGUI.Screens.Collections
{
    public partial class AddScoreButton : Container
    {
        private readonly RoundedButton addButton;
        private readonly GridContainer creationContainer;
        private readonly OsuTextBox scoreIdTextBox;

        public delegate void OnAddHandler(long scoreId);

        public event OnAddHandler? OnAdd;

        private const int height = 40;
        private const int fade_duration = 200;

        public AddScoreButton()
        {
            RelativeSizeAxes = Axes.X;
            Height = height;

            Children = new Drawable[]
            {
                addButton = new RoundedButton
                {
                    RelativeSizeAxes = Axes.X,
                    Text = "+",
                    Height = height,
                    Action = () =>
                    {
                        creationContainer!.FadeIn(fade_duration);
                        addButton!.FadeOut(fade_duration);
                    }
                },
                creationContainer = new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0,
                    ColumnDimensions = new[] { new Dimension(), new Dimension(GridSizeMode.Absolute, 100) },
                    RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            scoreIdTextBox = new OsuNumberBox
                            {
                                RelativeSizeAxes = Axes.X,
                                PlaceholderText = "Score ID",
                                CornerRadius = height / 2f
                            },
                            new RoundedButton
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = height,
                                Padding = new MarginPadding { Left = 5 },
                                Text = "Add",
                                Action = addScore
                            }
                        }
                    }
                }
            };
        }

        private void addScore()
        {
            if (string.IsNullOrEmpty(scoreIdTextBox.Current.Value))
            {
                scoreIdTextBox.FlashColour(ColourInfo.SingleColour(Colour4.Red), 500);
                return;
            }

            creationContainer.FadeOut(fade_duration);
            addButton.FadeIn(fade_duration);

            OnAdd?.Invoke(long.Parse(scoreIdTextBox.Current.Value));
            scoreIdTextBox.Current.Value = string.Empty;
        }
    }
}
