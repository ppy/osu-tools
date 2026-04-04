// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using PerformanceCalculatorGUI.Components.TextBoxes;

namespace PerformanceCalculatorGUI.Screens.Simulate
{
    public partial class AttributesTable : Container
    {
        public readonly Bindable<Dictionary<string, object>> Attributes = new Bindable<Dictionary<string, object>>();
        private const float row_height = 35;

        private FillFlowContainer backgroundFlow = null!;
        private GridContainer grid = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            CornerRadius = ExtendedLabelledTextBox.CORNER_RADIUS;
            Masking = true;

            AddRangeInternal(new Drawable[]
            {
                backgroundFlow = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical
                },
                grid = new GridContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    ColumnDimensions = new[] { new Dimension(GridSizeMode.AutoSize), new Dimension() }
                }
            });

            Attributes.BindValueChanged(onAttributesChanged);
        }

        private void onAttributesChanged(ValueChangedEvent<Dictionary<string, object>> changedEvent)
        {
            grid.RowDimensions = Enumerable.Repeat(new Dimension(GridSizeMode.Absolute, row_height), changedEvent.NewValue.Count).ToArray();
            grid.Content = changedEvent.NewValue.Select(s => createRowContent(s.Key, s.Value)).ToArray();

            backgroundFlow.Children = changedEvent.NewValue.Select((_, i) => new Box
            {
                RelativeSizeAxes = Axes.X,
                Height = row_height,
                Colour = colourProvider.Background4.Opacity(i % 2 == 0 ? 0.7f : 0.9f),
            }).ToArray();
        }

        private Drawable[] createRowContent(string label, object value) => new Drawable[]
        {
            new OsuSpriteText
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                Font = OsuFont.GetFont(size: 16, weight: FontWeight.Bold),
                Text = label.Humanize().ToLowerInvariant(),
                Margin = new MarginPadding { Left = 15, Right = 10 },
                UseFullGlyphHeight = true
            },
            new ReadonlyOsuTextBox(FormattableString.Invariant($"{value:N2}"), false)
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                Height = 1,
                RelativeSizeAxes = Axes.Both,
                SelectAllOnFocus = true,
                FontSize = 18,
                CornerRadius = ExtendedLabelledTextBox.CORNER_RADIUS
            },
        };
    }
}
