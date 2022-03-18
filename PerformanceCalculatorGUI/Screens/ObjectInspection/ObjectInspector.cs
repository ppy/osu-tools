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
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components;
using osu.Game.Screens.Edit.Components.Timelines.Summary;
using osuTK;
using osuTK.Input;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    internal class ObjectInspector : OsuFocusedOverlayContainer
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

        private readonly ProcessorWorkingBeatmap processorBeatmap;
        private EditorClock clock;
        private Container playfieldContainer;

        protected override bool BlockNonPositionalInput => true;

        protected override bool DimMainContent => false;

        public ObjectInspector(ProcessorWorkingBeatmap working)
        {
            processorBeatmap = working;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            var rulesetInstance = ruleset.Value.CreateInstance();
            var modifiedMods = mods.Value.Append(rulesetInstance.GetAutoplayMod()).ToList();

            var playableBeatmap = processorBeatmap.GetPlayableBeatmap(ruleset.Value, modifiedMods);
            processorBeatmap.LoadTrack();

            clock = new EditorClock(playableBeatmap, beatDivisor) { IsCoupled = false };
            clock.ChangeSource(processorBeatmap.Track);
            dependencies.CacheAs(clock);

            var editorBeatmap = new EditorBeatmap(playableBeatmap);
            dependencies.CacheAs(editorBeatmap);

            beatmap.Value = processorBeatmap;

            AddInternal(new Container
            {
                Origin = Anchor.Centre,
                Anchor = Anchor.Centre,
                Size = new Vector2(0.95f),
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
                    playfieldContainer = new PlayfieldAdjustmentContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(0.8f),
                        RelativeSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new PlayfieldBorder
                            {
                                RelativeSizeAxes = Axes.Both,
                                PlayfieldBorderStyle = { Value = PlayfieldBorderStyle.Corners }
                            },
                        }
                    },
                    new Container
                    {
                        Name = "Bottom bar",
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        RelativeSizeAxes = Axes.X,
                        Padding = new MarginPadding(5f),
                        Height = 50,
                        Child = new GridContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            ColumnDimensions = new[]
                            {
                                new Dimension(GridSizeMode.Absolute, 200),
                                new Dimension(),
                                new Dimension(GridSizeMode.Absolute, 200)
                            },
                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Padding = new MarginPadding { Right = 10 },
                                        Child = new TimeInfoContainer { RelativeSizeAxes = Axes.Both },
                                    },
                                    new SummaryTimeline
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                    },
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Padding = new MarginPadding { Left = 10 },
                                        Child = new PlaybackControl { RelativeSizeAxes = Axes.Both },
                                    }
                                },
                            }
                        }
                    },
                    clock
                }
            });

            playfieldContainer.Add(ruleset.Value.ShortName switch
            {
                "osu" => new OsuObjectInspectorRuleset(rulesetInstance, playableBeatmap, modifiedMods)
                {
                    RelativeSizeAxes = Axes.Both,
                    Clock = clock,
                    ProcessCustomClock = false
                },
                _ => new OsuSpriteText
                {
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                    Text = "This ruleset is not supported yet!"
                }
            });
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
