
using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using Newtonsoft.Json;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Threading;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays.Mods;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Play.HUD;
using osu.Game.Utils;
using PerformanceCalculatorGUI.Components;

namespace PerformanceCalculatorGUI.Screens
{
    public class SimulateScreen : CompositeDrawable
    {
        private ProcessorWorkingBeatmap working;

        private UserModSelectOverlay userModsSelectOverlay;

        private LabelledTextBox beatmapTextBox;
        private LabelledTextBox accuracyTextBox;
        private LabelledTextBox missesTextBox;
        private LabelledTextBox comboTextBox;
        private LabelledTextBox scoreTextBox;

        private DifficultyAttributes difficultyAttributes;
        private FillFlowContainer difficultyAttributesContainer;
        private FillFlowContainer performanceAttributesContainer;

        private FillFlowContainer beatmapDataContainer;
        private OsuSpriteText beatmapTitle;

        private ModDisplay modDisplay;

        private readonly Bindable<IReadOnlyList<Mod>> appliedMods = new Bindable<IReadOnlyList<Mod>>(Array.Empty<Mod>());

        [Resolved]
        private Bindable<RulesetInfo> ruleset { get; set; }

        public SimulateScreen()
        {
            RelativeSizeAxes = Axes.Both;

            FillMode = FillMode.Fill;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new Container
                {
                    Name = "File selection",
                    RelativeSizeAxes = Axes.X,
                    Height = 40,
                    Child = beatmapTextBox = new FileChooserLabelledTextBox(".osu")
                    {
                        Label = "Beatmap",
                        FixedLabelWidth = 160f,
                        PlaceholderText = "Click to select a background image",
                        Current = { Value = "No beatmap loaded!" }
                    },
                },
                new Container
                {
                    Name = "Beatmap title",
                    RelativeSizeAxes = Axes.X,
                    Y = 40,
                    Height = 20,
                    Children = new Drawable[]
                    {
                        beatmapTitle = new OsuSpriteText()
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Height = 20,
                            Text = "No beatmap loaded!"
                        },
                    }
                },
                beatmapDataContainer = new FillFlowContainer()
                {
                    Name = "Beatmap data",
                    Y = 60,
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Children = new Drawable[]
                    {
                        new OsuScrollContainer(Direction.Vertical)
                        {
                            Name = "Score params",
                            ScrollbarVisible = true,
                            RelativeSizeAxes = Axes.Both,
                            Width = 0.5f,
                            Height = 1.0f,
                            Child = new FillFlowContainer()
                            {
                                Padding = new MarginPadding(10.0f),
                                RelativeSizeAxes = Axes.Both,
                                Direction = FillDirection.Vertical,
                                Children = new Drawable[]
                                {
                                    new OsuSpriteText()
                                    {
                                        Margin = new MarginPadding(10.0f),
                                        Origin = Anchor.TopLeft,
                                        Height = 20,
                                        Text = "Score params"
                                    },
                                    new FillFlowContainer
                                    {
                                        Name = "Mods container",
                                        Height = 40,
                                        Direction = FillDirection.Horizontal,
                                        RelativeSizeAxes = Axes.X,
                                        Anchor = Anchor.TopLeft,
                                        AutoSizeAxes = Axes.Y,
                                        Children = new Drawable[]
                                        {
                                            new OsuButton
                                            {
                                                Width = 100,
                                                Margin = new MarginPadding(5.0f),
                                                Action = () => { userModsSelectOverlay.Show(); },
                                                Text = "Mods"
                                            },
                                            modDisplay = new ModDisplay()
                                        }
                                    },
                                    accuracyTextBox = new LabelledNumberBox()
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Anchor = Anchor.TopLeft,
                                        Label = "Accuracy",
                                        PlaceholderText = "100"
                                    },
                                    missesTextBox = new LabelledNumberBox()
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Anchor = Anchor.TopLeft,
                                        Label = "Misses",
                                        PlaceholderText = "0"
                                    },
                                    comboTextBox = new LabelledNumberBox()
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Anchor = Anchor.TopLeft,
                                        Label = "Combo",
                                        PlaceholderText = "0"
                                    },
                                    scoreTextBox = new LabelledNumberBox()
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Anchor = Anchor.TopLeft,
                                        Label = "Score",
                                        PlaceholderText = "1000000"
                                    }
                                }
                            }
                        },
                        new OsuScrollContainer(Direction.Vertical)
                        {
                            Name = "Difficulty calculation results",
                            ScrollDistance = 10f,
                            ScrollbarVisible = true,
                            RelativeSizeAxes = Axes.Both,
                            Width = 0.5f,
                            Child = new FillFlowContainer()
                            {
                                Padding = new MarginPadding(10.0f),
                                RelativeSizeAxes = Axes.Both,
                                Direction = FillDirection.Vertical,
                                Children = new Drawable[]
                                {
                                    new OsuSpriteText()
                                    {
                                        Margin = new MarginPadding(10.0f),
                                        Origin = Anchor.TopLeft,
                                        Height = 20,
                                        Text = "Difficulty Attributes"
                                    },
                                    difficultyAttributesContainer = new FillFlowContainer()
                                    {
                                        Direction = FillDirection.Vertical,
                                        RelativeSizeAxes = Axes.X,
                                        Anchor = Anchor.TopLeft,
                                        AutoSizeAxes = Axes.Y
                                    },
                                    new OsuSpriteText()
                                    {
                                        Margin = new MarginPadding(10.0f),
                                        Origin = Anchor.TopLeft,
                                        Height = 20,
                                        Text = "Performance Attributes"
                                    },
                                    performanceAttributesContainer = new FillFlowContainer()
                                    {
                                        Direction = FillDirection.Vertical,
                                        RelativeSizeAxes = Axes.X,
                                        Anchor = Anchor.TopLeft,
                                        AutoSizeAxes = Axes.Y
                                    }
                                }
                            }
                        }
                    }
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Child = userModsSelectOverlay = new UserModSelectOverlay()
                    {
                        IsValidMod = (mod) => mod.HasImplementation && ModUtils.FlattenMod(mod).All(m => m.UserPlayable),
                        SelectedMods = { BindTarget = appliedMods }
                    },
                }
            };

            beatmapDataContainer.Hide();
            userModsSelectOverlay.Hide();

            beatmapTextBox.Current.BindValueChanged(beatmapChanged);

            accuracyTextBox.Current.BindValueChanged(_ => calculatePerformance());
            missesTextBox.Current.BindValueChanged(_ => calculatePerformance());
            comboTextBox.Current.BindValueChanged(_ => calculatePerformance());
            scoreTextBox.Current.BindValueChanged(_ => calculatePerformance());

            appliedMods.BindValueChanged(modsChanged);
            modDisplay.Current.BindTo(appliedMods);

            ruleset.BindValueChanged(_ =>
            {
                appliedMods.Value = Array.Empty<Mod>();
                calculateDifficulty();
            });
        }

        private ModSettingChangeTracker modSettingChangeTracker;
        private ScheduledDelegate debouncedStatisticsUpdate;

        private void modsChanged(ValueChangedEvent<IReadOnlyList<Mod>> mods)
        {
            modSettingChangeTracker?.Dispose();

            modSettingChangeTracker = new ModSettingChangeTracker(mods.NewValue);
            modSettingChangeTracker.SettingChanged += m =>
            {
                debouncedStatisticsUpdate?.Cancel();
                debouncedStatisticsUpdate = Scheduler.AddDelayed(calculateDifficulty, 100);
            };

            calculateDifficulty();
        }

        private void beatmapChanged(ValueChangedEvent<string> filePath)
        {
            working = ProcessorWorkingBeatmap.FromFileOrId(filePath.NewValue);

            if (!working.BeatmapInfo.Ruleset.Equals(ruleset.Value))
            {
                ruleset.Value = working.BeatmapInfo.Ruleset;
                appliedMods.Value = Array.Empty<Mod>();
            }

            beatmapTitle.Text = $"[{ruleset.Value.Name}] {working.BeatmapInfo.GetDisplayTitle()}";

            calculateDifficulty();

            beatmapDataContainer.Show();
        }

        private void calculateDifficulty()
        {
            if (working == null)
                return;

            difficultyAttributes = ruleset.Value.CreateInstance().CreateDifficultyCalculator(working).Calculate(appliedMods.Value);

            comboTextBox.PlaceholderText = difficultyAttributes.MaxCombo.ToString();

            var diffAttributeValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(difficultyAttributes)) ?? new Dictionary<string, object>();
            difficultyAttributesContainer.Children = diffAttributeValues.Select(x =>
                new LabelledTextBox()
                {
                    ReadOnly = true,
                    Origin = Anchor.TopLeft,
                    Label = x.Key.Humanize(),
                    Text = FormattableString.Invariant($"{x.Value:N2}")
                }
            ).ToArray();

            calculatePerformance();
        }

        private void calculatePerformance()
        {
            if (working == null || difficultyAttributes == null)
                return;

            var accuracy = 1.0;
            if (!string.IsNullOrEmpty(accuracyTextBox.Current?.Value))
                accuracy = double.Parse(accuracyTextBox.Current.Value) / 100.0;

            if (accuracy > 1.0)
                accuracy = 1.0;

            var misses = 0;
            if (!string.IsNullOrEmpty(missesTextBox.Current?.Value))
                misses = int.Parse(missesTextBox.Current.Value);

            var combo = difficultyAttributes.MaxCombo;
            if (!string.IsNullOrEmpty(comboTextBox.Current?.Value))
                combo = int.Parse(comboTextBox.Current.Value);

            var score = 1000000;
            if (!string.IsNullOrEmpty(scoreTextBox.Current?.Value))
                score = int.Parse(scoreTextBox.Current.Value);

            var performanceCalculator = ruleset.Value.CreateInstance().CreatePerformanceCalculator(difficultyAttributes, new ScoreInfo
            {
                Accuracy = accuracy,
                MaxCombo = combo,
                Statistics = generateHitResults(accuracy, working.Beatmap, misses, null, null),
                Mods = appliedMods.Value.ToArray(),
                TotalScore = score,
                Ruleset = ruleset.Value
            });

            var ppAttributes = performanceCalculator?.Calculate();

            var perfAttributeValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(ppAttributes)) ?? new Dictionary<string, object>();
            performanceAttributesContainer.Children = perfAttributeValues.Select(x =>
                new LabelledTextBox()
                {
                    ReadOnly = true,
                    Origin = Anchor.TopLeft,
                    Label = x.Key.Humanize(),
                    Text = FormattableString.Invariant($"{x.Value:N2}")
                }
            ).ToArray();
        }

        // TODO: per-ruleset generation
        private Dictionary<HitResult, int> generateHitResults(double accuracy, IBeatmap beatmap, int countMiss, int? countMeh, int? countGood)
        {
            int countGreat;

            var totalResultCount = beatmap.HitObjects.Count;

            if (countMeh != null || countGood != null)
            {
                countGreat = totalResultCount - (countGood ?? 0) - (countMeh ?? 0) - countMiss;
            }
            else
            {
                // Let Great=6, Good=2, Meh=1, Miss=0. The total should be this.
                var targetTotal = (int)Math.Round(accuracy * totalResultCount * 6);

                // Start by assuming every non miss is a meh
                // This is how much increase is needed by greats and goods
                var delta = targetTotal - (totalResultCount - countMiss);

                // Each great increases total by 5 (great-meh=5)
                countGreat = delta / 5;
                // Each good increases total by 1 (good-meh=1). Covers remaining difference.
                countGood = delta % 5;
                // Mehs are left over. Could be negative if impossible value of amountMiss chosen
                countMeh = totalResultCount - countGreat - countGood - countMiss;
            }

            return new Dictionary<HitResult, int>
            {
                { HitResult.Great, countGreat },
                { HitResult.Ok, countGood ?? 0 },
                { HitResult.Meh, countMeh ?? 0 },
                { HitResult.Miss, countMiss }
            };
        }
    }
}
