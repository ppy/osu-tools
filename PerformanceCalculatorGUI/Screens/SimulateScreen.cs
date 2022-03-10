// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using Newtonsoft.Json;
using osu.Framework;
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
using osu.Game.Scoring;
using osu.Game.Screens.Play.HUD;
using osu.Game.Utils;
using PerformanceCalculatorGUI.Components;

namespace PerformanceCalculatorGUI.Screens
{
    public class SimulateScreen : PerformanceCalculatorScreen
    {
        private ProcessorWorkingBeatmap working;

        private UserModSelectOverlay userModsSelectOverlay;

        private LabelledTextBox beatmapTextBox;
        private LabelledFractionalNumberBox accuracyTextBox;
        private LimitedLabelledNumberBox missesTextBox;
        private LimitedLabelledNumberBox comboTextBox;
        private LimitedLabelledNumberBox scoreTextBox;

        private DifficultyAttributes difficultyAttributes;
        private FillFlowContainer difficultyAttributesContainer;
        private FillFlowContainer performanceAttributesContainer;

        private FillFlowContainer beatmapDataContainer;
        private OsuSpriteText beatmapTitle;

        private ModDisplay modDisplay;

        [Resolved]
        private Bindable<IReadOnlyList<Mod>> appliedMods { get; set; }

        [Resolved]
        private Bindable<RulesetInfo> ruleset { get; set; }

        public override bool ShouldShowConfirmationDialogOnSwitch => working != null;

        private const int file_selection_container_height = 40;
        private const int map_title_container_height = 20;

        public SimulateScreen()
        {
            RelativeSizeAxes = Axes.Both;
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
                    Height = file_selection_container_height,
                    Child = beatmapTextBox = new FileChooserLabelledTextBox(".osu")
                    {
                        Label = "Beatmap",
                        FixedLabelWidth = 160f,
                        PlaceholderText = "Click to select a beatmap file"
                    },
                },
                new Container
                {
                    Name = "Beatmap title",
                    RelativeSizeAxes = Axes.X,
                    Y = file_selection_container_height,
                    Height = map_title_container_height,
                    Children = new Drawable[]
                    {
                        beatmapTitle = new OsuSpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Height = map_title_container_height,
                            Text = "No beatmap loaded!"
                        },
                    }
                },
                beatmapDataContainer = new FillFlowContainer
                {
                    Name = "Beatmap data",
                    Y = file_selection_container_height + map_title_container_height,
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Children = new Drawable[]
                    {
                        new OsuScrollContainer(Direction.Vertical)
                        {
                            Name = "Score params",
                            RelativeSizeAxes = Axes.Both,
                            Width = 0.5f,
                            Child = new FillFlowContainer
                            {
                                Padding = new MarginPadding(10.0f),
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Children = new Drawable[]
                                {
                                    new OsuSpriteText
                                    {
                                        Margin = new MarginPadding(10.0f),
                                        Origin = Anchor.TopLeft,
                                        Height = 20,
                                        Text = "Score params"
                                    },
                                    accuracyTextBox = new LabelledFractionalNumberBox
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Anchor = Anchor.TopLeft,
                                        Label = "Accuracy",
                                        PlaceholderText = "100",
                                        MaxValue = 100.0,
                                        MinValue = 0.0,
                                        Value = { Value = 100.0 }
                                    },
                                    missesTextBox = new LimitedLabelledNumberBox
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Anchor = Anchor.TopLeft,
                                        Label = "Misses",
                                        PlaceholderText = "0",
                                        MinValue = 0
                                    },
                                    comboTextBox = new LimitedLabelledNumberBox
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Anchor = Anchor.TopLeft,
                                        Label = "Combo",
                                        PlaceholderText = "0",
                                        MinValue = 0
                                    },
                                    scoreTextBox = new LimitedLabelledNumberBox
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Anchor = Anchor.TopLeft,
                                        Label = "Score",
                                        PlaceholderText = "1000000",
                                        MinValue = 0,
                                        MaxValue = 1000000,
                                        Value = { Value = 1000000 }
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
                                    }
                                }
                            }
                        },
                        new OsuScrollContainer(Direction.Vertical)
                        {
                            Name = "Difficulty calculation results",
                            RelativeSizeAxes = Axes.Both,
                            Width = 0.5f,
                            Child = new FillFlowContainer
                            {
                                Padding = new MarginPadding(10.0f),
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Children = new Drawable[]
                                {
                                    new OsuSpriteText
                                    {
                                        Margin = new MarginPadding(10.0f),
                                        Origin = Anchor.TopLeft,
                                        Height = 20,
                                        Text = "Difficulty Attributes"
                                    },
                                    difficultyAttributesContainer = new FillFlowContainer
                                    {
                                        Direction = FillDirection.Vertical,
                                        RelativeSizeAxes = Axes.X,
                                        Anchor = Anchor.TopLeft,
                                        AutoSizeAxes = Axes.Y
                                    },
                                    new OsuSpriteText
                                    {
                                        Margin = new MarginPadding(10.0f),
                                        Origin = Anchor.TopLeft,
                                        Height = 20,
                                        Text = "Performance Attributes"
                                    },
                                    performanceAttributesContainer = new FillFlowContainer
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
                    Name = "Mod selection overlay",
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Child = userModsSelectOverlay = new UserModSelectOverlay
                    {
                        IsValidMod = (mod) => mod.HasImplementation && ModUtils.FlattenMod(mod).All(m => m.UserPlayable),
                        SelectedMods = { BindTarget = appliedMods }
                    },
                }
            };

            beatmapDataContainer.Hide();
            userModsSelectOverlay.Hide();

            beatmapTextBox.Current.BindValueChanged(beatmapChanged);

            accuracyTextBox.Value.BindValueChanged(_ => calculatePerformance());
            missesTextBox.Value.BindValueChanged(_ => calculatePerformance());
            comboTextBox.Value.BindValueChanged(_ => calculatePerformance());
            scoreTextBox.Value.BindValueChanged(_ => calculatePerformance());

            appliedMods.BindValueChanged(modsChanged);
            modDisplay.Current.BindTo(appliedMods);

            ruleset.BindValueChanged(_ =>
            {
                appliedMods.Value = Array.Empty<Mod>();
                calculateDifficulty();
            });

            if (RuntimeInfo.IsDesktop)
                HotReloadCallbackReceiver.CompilationFinished += _ => Schedule(calculateDifficulty);
        }

        public override void Hide()
        {
            userModsSelectOverlay.Hide();
            beatmapTextBox.Current.Value = string.Empty;
            base.Hide();
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
            if (string.IsNullOrEmpty(filePath.NewValue))
            {
                working = null;
                beatmapTitle.Text = "No beatmap loaded!";
                beatmapDataContainer.Hide();
                appliedMods.Value = Array.Empty<Mod>();
                return;
            }

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

            populateScoreParams();

            var diffAttributeValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(difficultyAttributes)) ?? new Dictionary<string, object>();
            difficultyAttributesContainer.Children = diffAttributeValues.Select(x =>
                new LabelledTextBox
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

            var accuracy = accuracyTextBox.Value.Value / 100.0;

            var score = LegacyHelper.AdjustManiaScore(scoreTextBox.Value.Value, appliedMods.Value);

            var performanceCalculator = ruleset.Value.CreateInstance().CreatePerformanceCalculator(difficultyAttributes, new ScoreInfo
            {
                Accuracy = accuracy,
                MaxCombo = comboTextBox.Value.Value,
                Statistics = LegacyHelper.GenerateHitResultsForRuleset(ruleset.Value, accuracy, working.GetPlayableBeatmap(ruleset.Value, appliedMods.Value), missesTextBox.Value.Value, null, null),
                Mods = appliedMods.Value.ToArray(),
                TotalScore = score,
                Ruleset = ruleset.Value
            });

            var ppAttributes = performanceCalculator?.Calculate();

            var perfAttributeValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(ppAttributes)) ?? new Dictionary<string, object>();
            performanceAttributesContainer.Children = perfAttributeValues.Select(x =>
                new LabelledTextBox
                {
                    ReadOnly = true,
                    Origin = Anchor.TopLeft,
                    Label = x.Key.Humanize(),
                    Text = FormattableString.Invariant($"{x.Value:N2}")
                }
            ).ToArray();
        }

        private void populateScoreParams()
        {
            accuracyTextBox.Hide();
            comboTextBox.Hide();
            missesTextBox.Hide();
            scoreTextBox.Hide();

            // TODO: other rulesets?

            if (ruleset.Value.ShortName == "osu" || ruleset.Value.ShortName == "taiko" || ruleset.Value.ShortName == "fruits")
            {
                accuracyTextBox.Text = string.Empty;
                accuracyTextBox.Show();
            }

            if (ruleset.Value.ShortName == "osu" || ruleset.Value.ShortName == "taiko" || ruleset.Value.ShortName == "fruits")
            {
                comboTextBox.PlaceholderText = difficultyAttributes.MaxCombo.ToString();
                comboTextBox.MaxValue = comboTextBox.Value.Value = difficultyAttributes.MaxCombo;
                comboTextBox.Show();
            }

            if (ruleset.Value.ShortName == "osu" || ruleset.Value.ShortName == "taiko" || ruleset.Value.ShortName == "fruits")
            {
                missesTextBox.MaxValue = difficultyAttributes.MaxCombo;
                missesTextBox.Text = string.Empty;
                missesTextBox.Show();
            }

            if (ruleset.Value.ShortName == "mania")
            {
                scoreTextBox.Text = string.Empty;
                scoreTextBox.Show();
            }
        }
    }
}
