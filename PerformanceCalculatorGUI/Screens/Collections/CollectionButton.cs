// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;

namespace PerformanceCalculatorGUI.Screens.Collections
{
    public partial class CollectionButton : Container, IHasTooltip
    {
        public Collection Collection { get; }

        private readonly RoundedButton deleteButton;

        public delegate void OnDeleteHandler(Collection collection);

        public event OnDeleteHandler? OnDelete;

        private const int height = 30;

        public CollectionButton(Collection collection, Bindable<Collection?> currentCollection)
        {
            Collection = collection;
            RelativeSizeAxes = Axes.X;
            Height = height;
            Margin = new MarginPadding { Bottom = 5 };

            Child = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                ColumnDimensions = new[] { new Dimension(), new Dimension(GridSizeMode.AutoSize) },
                RowDimensions = new[] { new Dimension(GridSizeMode.Absolute, height) },
                Content = new[]
                {
                    new Drawable[]
                    {
                        new RoundedButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = height,
                            Text = collection.Name,
                            Action = () =>
                            {
                                currentCollection.Value = collection;
                            }
                        },
                        deleteButton = new DangerousRoundedButton
                        {
                            RelativeSizeAxes = Axes.None,
                            Height = height,
                            Text = "Delete",
                            Action = () =>
                            {
                                OnDelete?.Invoke(collection);
                            }
                        }
                    }
                }
            };
        }

        protected override bool OnHover(HoverEvent e)
        {
            deleteButton
                .Delay(500)
                .ResizeWidthTo(60, 100, Easing.Out)
                .OnComplete(b => b.Margin = new MarginPadding { Left = 3 });

            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            deleteButton
                .ResizeWidthTo(0, 100, Easing.Out)
                .Finally(b => b.Margin = new MarginPadding());

            base.OnHoverLost(e);
        }

        public LocalisableString TooltipText => Collection.FileName;
    }
}
