// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osuTK;

namespace PerformanceCalculatorGUI.Components
{
    internal class StrainVisualizer : Container
    {
        private readonly Skill[] skills;

        private readonly List<BarGraph> graphs = new();
        private readonly List<Bindable<bool>> graphToggles = new();

        private ZoomableScrollContainer graphsContainer;
        private FillFlowContainer legendContainer;

        public StrainVisualizer(Skill[] skills)
        {
            this.skills = skills;
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours, OverlayColourProvider colourProvider)
        {
            ColourInfo[] skillColours =
            {
                colours.Blue,
                colours.Green,
                colours.Red,
                colours.Yellow,
                colours.Pink,
                colours.Cyan
            };

            Add(new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Masking = true,
                CornerRadius = 15,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background6,
                        Alpha = 0.6f
                    },
                    new FillFlowContainer
                    {
                        Padding = new MarginPadding(10),
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(5),
                        Children = new Drawable[]
                        {
                            graphsContainer = new ZoomableScrollContainer
                            {
                                Height = 150,
                                RelativeSizeAxes = Axes.X
                            },
                            legendContainer = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Full,
                                Spacing = new Vector2(5)
                            }
                        }
                    }
                }
            });

            var graphAlpha = Math.Min(1.5f / skills.Length, 0.9f);

            List<float[]> strains = new List<float[]>();
            foreach (var skill in skills)
                strains.Add(((StrainSkill)skill).GetCurrentStrainPeaks().Select(x => (float)x).ToArray());

            var strainMaxValue = strains.Max(x => x.Max());

            for (int i = 0; i < skills.Length; i++)
            {
                var graph = new BarGraph
                {
                    Alpha = graphAlpha,
                    RelativeSizeAxes = Axes.Both,
                    MaxValue = strainMaxValue,
                    Values = strains[i],
                    Colour = skillColours[i]
                };

                graphs.Add(graph);
                graphsContainer.AddRange(new Drawable[]
                {
                    graph,
                    new OsuSpriteText
                    {
                        Font = OsuFont.GetFont(size: 10),
                        Text = $"{strainMaxValue:0.00}"
                    }
                });

                // this is ugly, but it works
                var graphToggleBindable = new Bindable<bool>();
                var graphNumber = i;
                graphToggleBindable.BindValueChanged(state =>
                {
                    if (state.NewValue)
                    {
                        graphs[graphNumber].FadeTo(graphAlpha);
                    }
                    else
                    {
                        graphs[graphNumber].Hide();
                    }
                });
                graphToggles.Add(graphToggleBindable);

                legendContainer.Add(new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Masking = true,
                    CornerRadius = 10,
                    AutoSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = colourProvider.Background5
                        },
                        new ExtendedOsuCheckbox
                        {
                            Padding = new MarginPadding(10),
                            RelativeSizeAxes = Axes.None,
                            Width = 200,
                            Current = { BindTarget = graphToggleBindable, Default = true, Value = true },
                            LabelText = skills[i].GetType().Name,
                            TextColour = skillColours[i]
                        }
                    }
                });
            }
        }
    }
}
