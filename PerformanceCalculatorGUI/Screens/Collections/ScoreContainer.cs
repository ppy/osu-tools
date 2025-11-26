// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Graphics.UserInterface;
using PerformanceCalculatorGUI.Components;

namespace PerformanceCalculatorGUI.Screens.Collections
{
    public partial class ScoreContainer : Container
    {
        public ExtendedScore Score { get; }

        private IconButton deleteButton;

        public delegate void OnDeleteHandler(long scoreId);

        public event OnDeleteHandler? OnDelete;

        public ScoreContainer(ExtendedScore score)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

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
                                OnDelete?.Invoke((long)score.SoloScore.ID!);
                            }
                        },
                        new ExtendedProfileScore(score, true)
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
    }
}
