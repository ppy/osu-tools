// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.Taiko.UI;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components;
using osu.Game.Screens.Edit.Components.Timelines.Summary;
using osu.Game.Screens.Menu;
using osuTK.Graphics.ES31;
using osuTK.Input;
using osuTK;
using SharpGen.Runtime.Win32;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using FFmpeg.AutoGen;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class DebugValueList : Container {


        protected Dictionary<string, Dictionary<string, object>> InternalDict;
        private Box bgBox;
        private TextFlowContainer flowContainer;

        public DebugValueList() {
            InternalDict = new Dictionary<string, Dictionary<string, object>>();
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colors) {
            RelativeSizeAxes = Axes.Y;
            Width = 215;
            Children = new Drawable[]{
                bgBox = new Box
                {
                    Colour = colors.Background5,
                    Alpha = 0.95f,
                    RelativeSizeAxes = Axes.Both
                },
                new OsuScrollContainer() {
                    RelativeSizeAxes = Axes.X,
                    Height = 1000,
                    ScrollbarAnchor = Anchor.TopLeft,
                    Child = flowContainer = new TextFlowContainer()
                    {
                        Masking = false,
                        Margin = new MarginPadding { Left = 15 },
                        Size = new osuTK.Vector2(200,2000),
                        Y = 2000,
                        Origin = Anchor.BottomLeft
                    },
                }
            };
        }


        public void UpdateValues()
        {
            flowContainer.Text = "";
            foreach (KeyValuePair<string,Dictionary<string,object>> GroupPair in InternalDict)
            {
                // Big text
                string groupName = GroupPair.Key;
                Dictionary<string, object> groupDict = GroupPair.Value;
                flowContainer.AddText($"- {GroupPair.Key}\n", t => {
                    t.Scale = new osuTK.Vector2(1.8f);
                    t.Font = OsuFont.Torus.With(weight: "Bold");
                    t.Colour = Colour4.Pink;
                    t.Shadow = true;
                });

                foreach (KeyValuePair<string, object> ValuePair in groupDict) {
                    flowContainer.AddText($"   {ValuePair.Key} :\n", t => {
                        t.Scale = new osuTK.Vector2(1.3f);
                        t.Font = OsuFont.TorusAlternate.With(weight: "SemiBold");
                        t.Colour = Colour4.White;
                        t.Shadow = true;
                        t.Truncate = true;
                    });
                    flowContainer.AddText($"     -> {ValuePair.Value}\n\n", t => {
                        t.Scale = new osuTK.Vector2(1.3f);
                        t.Font = OsuFont.TorusAlternate.With(weight: "SemiBold");
                        t.Colour = Colour4.White;
                        t.Shadow = true;
                    });
                }
            }
        }

        public void AddGroup(string name, string[] overrides = null) {
            overrides ??= new string[0];
            foreach (string other in overrides) {
                InternalDict.Remove(other);
            }
            InternalDict[name] =  new Dictionary<string, object>();
        }

        public void SetValue(string group, string name, object value) {
            InternalDict.TryGetValue(group, out var exists);
            if (exists == null) {
                AddGroup(group);
            }
            if (value is double val)
            {
                value = Math.Truncate(val * 1000) / 1000;
            }
            if (value is float val2)
            {
                value = Math.Truncate(val2 * 1000) / 1000;
            }
            if (value is Vector2 val3)
            {
                value = new Vector2((float)(Math.Truncate(val3.X * 100) / 100), (float)Math.Truncate(val3.Y * 100) / 100);
            }

            InternalDict[group][name] = value;
        }

        public object GetValue(string group, string name)
        {
            return InternalDict[group][name];
        }
    }

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
        private Container layout;

        private DebugValueList values;
        private Container DebugContainer;

        protected override bool BlockNonPositionalInput => true;

        protected override bool DimMainContent => false;

        private const int bottom_bar_height = 50;

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

            AddInternal(layout = new Container
            {
                Origin = Anchor.Centre,
                Anchor = Anchor.Centre,
                Masking = true,
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
                    values = new DebugValueList() {

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
                    clock
                }
            });
            dependencies.CacheAs(values);
            DrawableRuleset inspectorRuleset = null;

            layout.Add(ruleset.Value.ShortName switch
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
                        inspectorRuleset = new OsuObjectInspectorRuleset(rulesetInstance, playableBeatmap, modifiedMods, difficultyCalculator.Value as ExtendedOsuDifficultyCalculator, processorBeatmap.Track.Rate)
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
                    Margin = new MarginPadding(10) { Bottom = bottom_bar_height },
                    Child = inspectorRuleset = new TaikoObjectInspectorRuleset(rulesetInstance, playableBeatmap, modifiedMods, difficultyCalculator.Value as ExtendedTaikoDifficultyCalculator,
                        processorBeatmap.Track.Rate)
                    {
                        RelativeSizeAxes = Axes.Both,
                        Clock = clock,
                        ProcessCustomClock = false
                    }
                },
                "fruits" => new CatchPlayfieldAdjustmentContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Y = 100,
                    Children = new Drawable[]
                    {
                        inspectorRuleset = new CatchObjectInspectorRuleset(rulesetInstance, playableBeatmap, modifiedMods, difficultyCalculator.Value as ExtendedCatchDifficultyCalculator, processorBeatmap.Track.Rate)
                        {
                            RelativeSizeAxes = Axes.Both,
                            Clock = clock,
                            ProcessCustomClock = false
                        }
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

            return base.OnKeyDown(e);
        }
    }
}
