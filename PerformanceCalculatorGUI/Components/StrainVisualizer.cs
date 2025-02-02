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
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osuTK;
using PerformanceCalculatorGUI.Components.TextBoxes;

namespace PerformanceCalculatorGUI.Components
{
    public partial class StrainVisualizer : Container
    {
        public readonly Bindable<Skill[]> Skills = new Bindable<Skill[]>();

        private readonly List<Bindable<bool>> graphToggles = new List<Bindable<bool>>();

        public readonly Bindable<int> TimeUntilFirstStrain = new Bindable<int>();

        private ZoomableScrollContainer graphsContainer;
        private FillFlowContainer legendContainer;

        private ColourInfo[] skillColours;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; }

        public StrainVisualizer()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        private float graphAlpha;

        private void updateGraphs(ValueChangedEvent<Skill[]> val)
        {
            graphsContainer.Clear();

            var skills = val.NewValue.Where(x => x is StrainSkill or StrainDecaySkill).ToArray();

            // dont bother if there are no strain skills to draw
            if (skills.Length == 0)
            {
                legendContainer.Clear();
                graphToggles.Clear();
                return;
            }

            graphAlpha = Math.Min(1.5f / skills.Length, 0.9f);
            var strainLists = getStrainLists(skills);
            addStrainBars(skills, strainLists);
            addTooltipBars(strainLists);

            if (val.OldValue == null || !val.NewValue.All(x => val.OldValue.Any(y => y.GetType().Name == x.GetType().Name)))
            {
                // skill list changed - recreate toggles
                legendContainer.Clear();
                graphToggles.Clear();

                for (int i = 0; i < skills.Length; i++)
                {
                    // this is ugly, but it works
                    var graphToggleBindable = new Bindable<bool>();
                    var graphNum = i;
                    graphToggleBindable.BindValueChanged(state =>
                    {
                        if (state.NewValue)
                        {
                            graphsContainer[graphNum].FadeTo(graphAlpha);
                        }
                        else
                        {
                            graphsContainer[graphNum].Hide();
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
                                TextColour = skillColours[i % skillColours.Length]
                            }
                        }
                    });
                }
            }
            else
            {
                for (int i = 0; i < skills.Length; i++)
                {
                    // graphs are visible by default, we want to hide ones that were disabled before
                    if (!graphToggles[i].Value)
                        graphsContainer[i].Hide();
                }
            }
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            skillColours = new ColourInfo[]
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
                CornerRadius = ExtendedLabelledTextBox.CORNER_RADIUS,
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
                            graphsContainer = new ZoomableScrollContainer(1, 100, 1)
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

            Skills.BindValueChanged(updateGraphs);
        }

        private void addStrainBars(Skill[] skills, List<float[]> strainLists)
        {
            var strainMaxValue = strainLists.Max(list => list.Max());

            for (int i = 0; i < skills.Length; i++)
            {
                graphsContainer.AddRange(new Drawable[]
                {
                    new BufferedContainer(cachedFrameBuffer: true)
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = graphAlpha,
                        Colour = skillColours[i % skillColours.Length],
                        Child = new StrainBarGraph
                        {
                            RelativeSizeAxes = Axes.Both,
                            MaxValue = strainMaxValue,
                            Values = strainLists[i]
                        }
                    }
                });
            }

            graphsContainer.Add(new OsuSpriteText
            {
                Font = OsuFont.GetFont(size: 10),
                Text = $"{strainMaxValue:0.00}"
            });
        }

        private void addTooltipBars(List<float[]> strainLists, int nBars = 1000)
        {
            double lastStrainTime = strainLists.Max(l => l.Length) * 400;

            var tooltipList = new List<string>();

            for (int i = 0; i < nBars; i++)
            {
                var strainTime = TimeSpan.FromMilliseconds(TimeUntilFirstStrain.Value + lastStrainTime * i / nBars);
                var tooltipText = $"~{strainTime:mm\\:ss\\.ff}";
                tooltipList.Add(tooltipText);
            }

            graphsContainer.AddRange(new Drawable[]
            {
                new BufferedContainer(cachedFrameBuffer: true)
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 1,
                    Child = new TooltipBarGraph
                    {
                        RelativeSizeAxes = Axes.Both,
                        Values = tooltipList
                    }
                }
            });
        }

        private static List<float[]> getStrainLists(Skill[] skills)
        {
            List<float[]> strainLists = new List<float[]>();

            foreach (var skill in skills)
            {
                var strains = ((StrainSkill)skill).GetCurrentStrainPeaks().ToArray();

                var skillStrainList = new List<float>();

                for (int i = 0; i < strains.Length; i++)
                {
                    var strain = strains[i];
                    skillStrainList.Add(((float)strain));
                }

                strainLists.Add(skillStrainList.ToArray());
            }

            return strainLists;
        }
    }

    public partial class StrainBarGraph : FillFlowContainer<Bar>
    {
        /// <summary>
        /// Manually sets the max value, if null <see cref="Enumerable.Max(IEnumerable{float})"/> is instead used
        /// </summary>
        public float? MaxValue { get; set; }

        /// <summary>
        /// A list of floats that defines the length of each <see cref="Bar"/>
        /// </summary>
        public IEnumerable<float> Values
        {
            set
            {
                Clear();

                foreach (var val in value)
                {
                    float length = MaxValue ?? value.Max();
                    if (length != 0)
                        length = val / length;

                    float size = value.Count();
                    if (size != 0)
                        size = 1.0f / size;

                    Add(new Bar
                    {
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(size, 1),
                        Length = length,
                        Direction = BarDirection.BottomToTop
                    });
                }
            }
        }
    }

    public partial class TooltipBar : Bar, IHasTooltip
    {
        public TooltipBar(string tooltip)
        {
            TooltipText = tooltip;
        }

        public LocalisableString TooltipText { get; }
    }

    public partial class TooltipBarGraph : FillFlowContainer<TooltipBar>
    {
        /// <summary>
        /// A list of strings that defines tooltips, don't make it too big
        /// </summary>
        public IEnumerable<string> Values
        {
            set
            {
                Clear();

                foreach (var tooltip in value)
                {
                    float size = value.Count();
                    if (size != 0)
                        size = 1.0f / size;

                    Add(new TooltipBar(tooltip)
                    {
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(size, 1),
                        Direction = BarDirection.BottomToTop
                    });
                }
            }
        }
    }
}
