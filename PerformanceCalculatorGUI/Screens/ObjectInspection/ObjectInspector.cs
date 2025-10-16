// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch.Beatmaps;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.Taiko.UI;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components;
using osu.Game.Screens.Edit.Components.Timelines.Summary;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osu.Game.Skinning;
using osuTK.Input;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    [Cached(typeof(IBeatSnapProvider))]
    public partial class ObjectInspector : OsuFocusedOverlayContainer, IBeatSnapProvider
    {
        private DependencyContainer dependencies;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
            => dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

        [Cached]
        private BindableBeatDivisor beatDivisor = new BindableBeatDivisor(4);

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
        private Container rulesetContainer;

        private ObjectDifficultyValuesContainer difficultyValuesContainer;
        private EditorBeatmap editorBeatmap;
        private IReadOnlyList<HitObject> hitObjects;

        protected override bool BlockNonPositionalInput => true;

        protected override bool DimMainContent => false;

        private const int bottom_bar_height = 50;
        private const int side_bar_width = 220;
        private const int timeline_height = 50;

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
            modifiedMods.OfType<IApplicableToTrack>().ForEach(m => m.ApplyToTrack(processorBeatmap.Track));

            clock = new EditorClock(playableBeatmap, beatDivisor);
            clock.ChangeSource(processorBeatmap.Track);
            dependencies.CacheAs(clock);
            dependencies.CacheAs(processorBeatmap.Track);

            editorBeatmap = new EditorBeatmap(playableBeatmap);
            dependencies.CacheAs(editorBeatmap);

            beatmap.Value = processorBeatmap;

            hitObjects = ruleset.Value.ShortName switch
            {
                "fruits" => CatchBeatmap.GetPalpableObjects(playableBeatmap.HitObjects).Where(o => o is not (Banana or TinyDroplet)).ToList(),
                _ => playableBeatmap.HitObjects,
            };

            Timeline timeline;

            AddInternal(new Container
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
                    difficultyValuesContainer = new ObjectDifficultyValuesContainer
                    {
                        RelativeSizeAxes = Axes.Y,
                        Padding = new MarginPadding { Bottom = bottom_bar_height },
                        Width = side_bar_width
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding { Left = side_bar_width },
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                Colour = colourProvider.Background6,
                                Alpha = 0.35f,
                                RelativeSizeAxes = Axes.Both
                            },
                            timeline = new Timeline(new TimelineBlueprintContainer())
                        }
                    },
                    rulesetContainer = new RulesetSkinProvidingContainer(rulesetInstance, playableBeatmap, null)
                    {
                        Origin = Anchor.TopRight,
                        Anchor = Anchor.TopRight,
                        Padding = new MarginPadding { Left = side_bar_width, Bottom = bottom_bar_height },
                        RelativeSizeAxes = Axes.Both
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
            dependencies.CacheAs(difficultyValuesContainer);

            timeline.Height = timeline_height;
            timeline.Children.Last().Height = timeline_height; // set inner container height to 55 to fix centering

            rulesetContainer.Add(ruleset.Value.ShortName switch
            {
                "osu" => new OsuPlayfieldAdjustmentContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new PlayfieldBorder
                        {
                            RelativeSizeAxes = Axes.Both,
                            PlayfieldBorderStyle = { Value = PlayfieldBorderStyle.Corners }
                        },
                        new OsuObjectInspectorRuleset(rulesetInstance, playableBeatmap, modifiedMods, difficultyCalculator.Value as ExtendedOsuDifficultyCalculator,
                            processorBeatmap.Track.Rate)
                        {
                            RelativeSizeAxes = Axes.Both,
                            Clock = clock,
                            ProcessCustomClock = false
                        }
                    }
                },
                "taiko" => new TaikoPlayfieldAdjustmentContainer
                {
                    Child = new TaikoObjectInspectorRuleset(rulesetInstance, playableBeatmap, modifiedMods, difficultyCalculator.Value as ExtendedTaikoDifficultyCalculator,
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
                        new CatchObjectInspectorRuleset(rulesetInstance, playableBeatmap, modifiedMods, difficultyCalculator.Value as ExtendedCatchDifficultyCalculator,
                            processorBeatmap.Track.Rate)
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

            double? seekTo = null;

            if (e.Key == Key.Left)
            {
                seekTo = hitObjects
                         .LastOrDefault(x => x.StartTime < clock.CurrentTime)?
                         .StartTime;

                // slight leeway to make going back beyond just one object possible when the clock is running
                if (clock.IsRunning)
                    seekTo -= 100;
            }

            if (e.Key == Key.Right)
            {
                seekTo = hitObjects
                         .FirstOrDefault(x => x.StartTime > clock.CurrentTime)?
                         .StartTime;
            }

            if (seekTo != null)
                clock.Seek(seekTo.Value);

            return base.OnKeyDown(e);
        }

        public double SnapTime(double time, double? referenceTime) => editorBeatmap.SnapTime(time, referenceTime);

        public double GetBeatLengthAtTime(double referenceTime) => editorBeatmap.GetBeatLengthAtTime(referenceTime);

        public int BeatDivisor => beatDivisor.Value;
    }
}
