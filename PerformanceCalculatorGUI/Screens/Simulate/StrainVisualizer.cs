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
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osuTK;
using osuTK.Graphics;
using PerformanceCalculatorGUI.Components;
using PerformanceCalculatorGUI.Components.TextBoxes;

namespace PerformanceCalculatorGUI.Screens.Simulate
{
    public partial class StrainVisualizer : Container
    {
        private readonly List<Bindable<bool>> graphToggles = new List<Bindable<bool>>();

        public readonly Bindable<int> TimeUntilFirstStrain = new Bindable<int>();

        private ZoomableScrollContainer graphsContainer = null!;
        private FillFlowContainer legendContainer = null!;

        private ColourInfo[] skillColours = [];

        [Resolved]
        private OverlayColourProvider? colourProvider { get; set; }

        [Resolved]
        private Bindable<DifficultyCalculator?> difficultyCalculator { get; set; } = null!;

        private const int strain_length = 400;

        public StrainVisualizer()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        private float graphAlpha;

        private void updateGraphs(ValueChangedEvent<DifficultyCalculator?> val)
        {
            graphsContainer.Clear();

            if (val.NewValue is not IExtendedDifficultyCalculator extendedDifficultyCalculator)
                return;

            var skills = extendedDifficultyCalculator.GetSkills();

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

            var oldSkills = (val.OldValue as IExtendedDifficultyCalculator)?.GetSkills();

            if (oldSkills == null || oldSkills.Length == 0 || !skills.All(x => oldSkills.Any(y => y.GetType().Name == x.GetType().Name)))
            {
                // skill list changed - recreate toggles
                legendContainer.Clear();
                graphToggles.Clear();

                for (int i = 0; i < skills.Length; i++)
                {
                    // this is ugly, but it works
                    var graphToggleBindable = new Bindable<bool>();
                    int graphNum = i;
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
                                Colour = colourProvider?.Background4 ?? Color4.Gray
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
                        Colour = colourProvider?.Background5 ?? Color4.Gray,
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

            difficultyCalculator.BindValueChanged(updateGraphs);
        }

        private void addStrainBars(Skill[] skills, List<Strain[]> strainLists)
        {
            double strainMaxValue = strainLists.SelectMany(x => x).MaxBy(x => x.Difficulty)!.Difficulty;

            for (int i = 0; i < skills.Length; i++)
            {
                var strainGraph = new StrainBarGraph
                {
                    RelativeSizeAxes = Axes.Both,
                    MaxValue = (float)strainMaxValue
                };
                strainGraph.CreateBars(strainLists[i]);

                graphsContainer.AddRange(new Drawable[]
                {
                    new BufferedContainer(cachedFrameBuffer: true)
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = graphAlpha,
                        Colour = skillColours[i % skillColours.Length],
                        Child = strainGraph
                    }
                });
            }

            graphsContainer.Add(new OsuSpriteText
            {
                Font = OsuFont.GetFont(size: 10),
                Text = $"{strainMaxValue:0.00}"
            });
        }

        private void addTooltipBars(List<Strain[]> strainLists, int nBars = 1000)
        {
            double lastStrainTime = strainLists.SelectMany(x => x).MaxBy(x => x.StartTime)!.StartTime;

            var tooltipList = new List<string>();

            for (int i = 0; i < nBars; i++)
            {
                var strainTime = TimeSpan.FromMilliseconds(TimeUntilFirstStrain.Value + lastStrainTime * i / nBars);
                string tooltipText = $"~{strainTime:mm\\:ss\\.ff}";
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

        private List<Strain[]> getStrainLists(Skill[] skills)
        {
            var strainLists = new List<Strain[]>();

            foreach (var skill in skills)
            {
                switch (skill)
                {
                    case StrainSkill strainSkill:
                        strainLists.Add(getStrainSkillStrainList(strainSkill));
                        break;

                    default:
                        strainLists.Add(getStrainList(skill));
                        break;
                }
            }

            return strainLists;
        }

        private Strain[] getStrainSkillStrainList(StrainSkill strainSkill)
        {
            double[] strains = strainSkill.GetCurrentStrainPeaks().ToArray();

            var skillStrainList = new List<Strain>();

            for (int i = 0; i < strains.Length; i++)
            {
                double strain = strains[i];
                skillStrainList.Add(new Strain
                {
                    Difficulty = strain,
                    StartTime = strain_length * i, // todo: use actual strain length
                    EndTime = (strain_length * i) + strain_length
                });
            }

            return skillStrainList.ToArray();
        }

        private Strain[] getStrainList(Skill skill)
        {
            var difficultyObjects = (difficultyCalculator.Value as IExtendedDifficultyCalculator)!.GetDifficultyHitObjects();

            var difficulties = skill.GetObjectDifficulties();

            var skillStrainList = new List<Strain>();

            for (int i = 0; i < difficulties.Count - 1; i++)
            {
                double strain = difficulties[i];
                var difficultyObject = difficultyObjects[i];
                var nextDifficultyObject = i < difficulties.Count - 1 ? difficultyObjects[i + 1] : null;

                double startTime = difficultyObject.StartTime;
                double endTime = difficultyObject.EndTime;

                if (nextDifficultyObject != null)
                {
                    // cap length to object_length + strain_length to make map breaks display 0 difficulty instead of the last-object-before-break difficulty
                    endTime = Math.Min(endTime + strain_length, nextDifficultyObject.StartTime);
                }

                skillStrainList.Add(new Strain
                {
                    Difficulty = strain,
                    StartTime = startTime,
                    EndTime = endTime
                });

                // add blank bars between objects to make the graph consistent timescale-wise
                if (nextDifficultyObject != null && nextDifficultyObject.StartTime - endTime > 0)
                {
                    skillStrainList.Add(new Strain
                    {
                        Difficulty = 0,
                        StartTime = endTime,
                        EndTime = nextDifficultyObject.StartTime
                    });
                }

                // add blank strain_length bar in the end to make the object difficulties graph consistent with strain-based graphs
                if (nextDifficultyObject == null)
                {
                    skillStrainList.Add(new Strain
                    {
                        Difficulty = 0,
                        StartTime = endTime,
                        EndTime = endTime + strain_length
                    });
                }
            }

            return skillStrainList.ToArray();
        }
    }

    public partial class StrainBarGraph : FillFlowContainer<Bar>
    {
        /// <summary>
        /// Manually sets the max value, if null <see cref="Enumerable.Max(IEnumerable{float})"/> is instead used
        /// </summary>
        public float? MaxValue { get; set; }

        public void CreateBars(Strain[] values)
        {
            Clear();

            double maxLength = MaxValue ?? values.MaxBy(x => x.Difficulty)!.Difficulty;
            double totalWidth = values.Sum(x => x.Length);

            foreach (Strain val in values)
            {
                double length = 0;
                if (maxLength != 0)
                    length = val.Difficulty / maxLength;

                float size = (float)(val.Length / totalWidth);

                Add(new Bar
                {
                    RelativeSizeAxes = Axes.Both,
                    Size = new Vector2(size, 1),
                    Length = (float)length,
                    Direction = BarDirection.BottomToTop
                });
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

                foreach (string tooltip in value)
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

    public class Strain
    {
        public double Difficulty { get; set; }
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public double Length => EndTime - StartTime;
    }
}
