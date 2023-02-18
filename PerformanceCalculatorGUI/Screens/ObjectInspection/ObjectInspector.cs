// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.Taiko.UI;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components;
using osu.Game.Screens.Edit.Components.Timelines.Summary;
using osuTK;
using osuTK.Input;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class ObjectInspector : OsuFocusedOverlayContainer
    {
        private DependencyContainer dependencies;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
            => dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

        [Cached]
        private BindableBeatDivisor beatDivisor = new();

        [Resolved]
        private Bindable<WorkingBeatmap> beatmap { get; set; }

        [Resolved]
        private Bindable<IReadOnlyList<Mod>> mods { get; set; }

        [Resolved]
        private Bindable<RulesetInfo> ruleset { get; set; }

        [Resolved]
        private Bindable<DifficultyCalculator> difficultyCalculator { get; set; }

        private readonly ProcessorWorkingBeatmap processorBeatmap;
        private EditorClock clock;
        private Container inspectContainer;

        private ObjectDifficultyValuesContainer values;

        private Bindable<DifficultyHitObject> focusedDiffHitBind;

        protected override bool BlockNonPositionalInput => true;

        protected override bool DimMainContent => false;

        private const int bottom_bar_height = 50;

        private const int side_bar_width = 215;

        public ObjectInspector(ProcessorWorkingBeatmap working)
        {
            processorBeatmap = working;
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            var rulesetInstance = ruleset.Value.CreateInstance();
            var modifiedMods = mods.Value.Append(rulesetInstance.GetAutoplayMod()).ToList();

            var playableBeatmap = processorBeatmap.GetPlayableBeatmap(ruleset.Value, modifiedMods);
            processorBeatmap.LoadTrack();

            clock = new EditorClock(playableBeatmap, beatDivisor);
            clock.ChangeSource(processorBeatmap.Track);
            dependencies.CacheAs(clock);

            var editorBeatmap = new EditorBeatmap(playableBeatmap);
            dependencies.CacheAs(editorBeatmap);

            beatmap.Value = processorBeatmap;
            focusedDiffHitBind = new Bindable<DifficultyHitObject>();

            // Background
            AddRange(new Drawable[]
            {
                new Container
                {
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                    CornerRadius = 15f,
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Colour = Colour4.Black,
                            Alpha = 0.95f,
                            RelativeSizeAxes = Axes.Both
                        },
                    }
                },

                // Object Inspector Container
                inspectContainer = new Container
                {
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                    Masking = true,
                    CornerRadius = 15f,
                    Padding = new MarginPadding() { Left = side_bar_width },
                    RelativeSizeAxes = Axes.Both,
                    Child = clock,
                },

                // layout
                new Container
                {
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                    Masking = true,
                    CornerRadius = 15f,
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        values = new ObjectDifficultyValuesContainer(focusedDiffHitBind)
                        {
                            Padding = new MarginPadding { Bottom = 5 },
                        },

                        new Container
                        {
                            Name = "Bottom bar",
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            RelativeSizeAxes = Axes.X,
                            Height = bottom_bar_height,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = colourProvider.Background4,
                                },
                                new GridContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    ColumnDimensions = new[]
                                    {
                                        new Dimension(GridSizeMode.Absolute, 170),
                                        new Dimension(),
                                        new Dimension(GridSizeMode.Absolute, 220)
                                    },
                                    Content = new[]
                                    {
                                        new Drawable[]
                                        {
                                            new TimeInfoContainer { RelativeSizeAxes = Axes.Both },
                                            new SummaryTimeline { RelativeSizeAxes = Axes.Both },
                                            new PlaybackControl { RelativeSizeAxes = Axes.Both },
                                        },
                                    }
                                }
                            }
                        },
                    }
                }
            });

            dependencies.CacheAs(values);
            DrawableRuleset inspectorRuleset = null;

            inspectContainer.Add(ruleset.Value.ShortName switch
            {
                "osu" => new OsuPlayfieldAdjustmentContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Margin = new MarginPadding(10) { Bottom = bottom_bar_height },
                    Children = new Drawable[]
                    {
                        new PlayfieldBorder
                        {
                            RelativeSizeAxes = Axes.Both,
                            PlayfieldBorderStyle = { Value = PlayfieldBorderStyle.Corners }
                        },
                        inspectorRuleset = new OsuObjectInspectorRuleset(rulesetInstance, playableBeatmap, modifiedMods, difficultyCalculator.Value as ExtendedOsuDifficultyCalculator,
                            processorBeatmap.Track.Rate, focusedDiffHitBind)
                        {
                            RelativeSizeAxes = Axes.Both,
                            Clock = clock,
                            ProcessCustomClock = false
                        }
                    }
                },
                "taiko" => new TaikoPlayfieldAdjustmentContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Margin = new MarginPadding(10) { Left = 0, Bottom = bottom_bar_height },
                    Child = inspectorRuleset = new TaikoObjectInspectorRuleset(rulesetInstance, playableBeatmap, modifiedMods, difficultyCalculator.Value as ExtendedTaikoDifficultyCalculator,
                        processorBeatmap.Track.Rate, focusedDiffHitBind)
                    {
                        RelativeSizeAxes = Axes.Both,
                        Clock = clock,
                        ProcessCustomClock = false
                    }
                },
                "fruits" => new CatchPlayfieldAdjustmentContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Margin = new MarginPadding(10) { Bottom = bottom_bar_height },
                    Y = 100,
                    Children = new Drawable[]
                    {
                        // guideline bar
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 3,
                            Y = 300,
                            Colour = Colour4.Red,
                            Children = new Drawable[]
                            {
                                // shadow
                                new Circle { Colour = Colour4.Gray, Size = new Vector2(10), Y = -3.3f + 2, X = -5 },
                                new Box { Colour = Colour4.Gray, Size = new Vector2(5, 20), Y = -8.3f + 2, X = 508 },
                                new Box { Colour = Colour4.Gray, RelativeSizeAxes = Axes.Both, Y = 2 },

                                // main
                                new Circle { Size = new Vector2(10), Y = -3.3f, X = -5 },
                                new Box { Size = new Vector2(5, 20), Y = -8.3f, X = 508 },
                                new Box { RelativeSizeAxes = Axes.Both },
                            }
                        },
                        inspectorRuleset = new CatchObjectInspectorRuleset(rulesetInstance, playableBeatmap, modifiedMods, difficultyCalculator.Value as ExtendedCatchDifficultyCalculator,
                            processorBeatmap.Track.Rate, focusedDiffHitBind)
                        {
                            RelativeSizeAxes = Axes.Both,
                            Clock = clock,
                            ProcessCustomClock = false
                        },
                    }
                },
                _ => new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new OsuSpriteText
                    {
                        Origin = Anchor.Centre,
                        Anchor = Anchor.Centre,
                        Text = "This ruleset is not supported yet!"
                    }
                }
            });

            ruleset.BindValueChanged(_ => PopOut());
            beatmap.BindValueChanged(_ => PopOut());
        }

        protected override void Update()
        {
            base.Update();
            clock.ProcessFrame();
        }

        protected override void PopIn()
        {
            base.PopIn();
            this.FadeIn();
        }

        protected override void PopOut()
        {
            base.PopOut();
            this.FadeOut();
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == Key.Space)
            {
                if (clock.IsRunning)
                    clock.Stop();
                else
                    clock.Start();
            }

            var amt = 0.25;
            amt = e.ControlPressed ? amt * 2 : amt;
            amt = e.ShiftPressed ? amt * 0.5 : amt;

            if (e.Key == Key.Q)
            {
                clock.SeekBackward(amount: amt);
            }

            if (e.Key == Key.E)
            {
                clock.SeekForward(amount: amt);
            }

            return base.OnKeyDown(e);
        }
    }
}
