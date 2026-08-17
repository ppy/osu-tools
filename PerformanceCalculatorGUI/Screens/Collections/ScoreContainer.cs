// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays.Profile.Sections;
using PerformanceCalculatorGUI.Components;
using PerformanceCalculatorGUI.Components.TextBoxes;

namespace PerformanceCalculatorGUI.Screens.Collections
{
    public partial class ScoreContainer : Container
    {
        public long ScoreId { get; }
        public ExtendedScore? Score { get; }

        private readonly IconButton deleteButton;

        public delegate void OnDeleteHandler(long scoreId);

        public event OnDeleteHandler? OnDelete;

        public ScoreContainer(long scoreId, ExtendedScore? score)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            ScoreId = scoreId;
            Score = score;
            Child = new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                ColumnDimensions = new[] { new Dimension(GridSizeMode.AutoSize), new Dimension() },
                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                Content = new[]
                {
                    new Drawable[]
                    {
                        deleteButton = new IconButton
                        {
                            Width = 0,
                            Height = 35,
                            Icon = FontAwesome.Regular.TrashAlt,
                            Action = () =>
                            {
                                OnDelete?.Invoke(scoreId);
                            }
                        },
                        Score != null ? new ExtendedProfileScore(Score, true) : new NullProfileScore(ScoreId)
                    }
                }
            };
        }

        protected override bool OnHover(HoverEvent e)
        {
            deleteButton
                .Delay(500)
                .ResizeWidthTo(35, 100, Easing.Out)
                .OnComplete(b => b.Margin = new MarginPadding { Right = 5 });

            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            deleteButton
                .ResizeWidthTo(0, 100, Easing.Out)
                .OnComplete(b => b.Margin = new MarginPadding());

            base.OnHoverLost(e);
        }

        private partial class NullProfileScore : ProfileItemContainer
        {
            public NullProfileScore(long scoreId)
            {
                RelativeSizeAxes = Axes.X;
                Height = ExtendedProfileScore.HEIGHT;

                CornerRadius = ExtendedLabelledTextBox.CORNER_RADIUS;
                AddRangeInternal(new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Font = OsuFont.GetFont(size: 14f, weight: FontWeight.Bold),
                        Text = $"Score ID {scoreId} does not exist."
                    }
                });
            }

            protected override bool OnHover(HoverEvent e)
            {
                base.OnHover(e);
                return false;
            }
        }
    }
}
