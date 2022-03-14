// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using Newtonsoft.Json;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
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
using osuTK;
using PerformanceCalculatorGUI.Components;
using PerformanceCalculatorGUI.Configuration;
using PerformanceCalculatorGUI.Screens.ObjectInspection;

namespace PerformanceCalculatorGUI.Screens
{
    public class SimulateScreen : PerformanceCalculatorScreen
    {
        private ProcessorWorkingBeatmap working;

        private UserModSelectOverlay userModsSelectOverlay;

        private LabelledTextBox beatmapTextBox;
        private LimitedLabelledNumberBox missesTextBox;
        private LimitedLabelledNumberBox comboTextBox;
        private LimitedLabelledNumberBox scoreTextBox;

        private GridContainer accuracyContainer;
        private LimitedLabelledFractionalNumberBox accuracyTextBox;
        private LimitedLabelledNumberBox goodsTextBox;
        private LimitedLabelledNumberBox mehsTextBox;
        private SwitchButton fullScoreDataSwitch;

        private DifficultyAttributes difficultyAttributes;
        private FillFlowContainer difficultyAttributesContainer;
        private FillFlowContainer performanceAttributesContainer;

        private FillFlowContainer beatmapDataContainer;
        private OsuSpriteText beatmapTitle;

        private ModDisplay modDisplay;

        private ObjectInspector objectInspector;

        [Resolved]
        private AudioManager audio { get; set; }

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
        private void load(SettingsManager configManager)
        {
            InternalChildren = new Drawable[]
            {
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    ColumnDimensions = new[] { new Dimension() },
                    RowDimensions = new[] { new Dimension(GridSizeMode.Absolute, file_selection_container_height), new Dimension(GridSizeMode.Absolute, map_title_container_height), new Dimension() },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new Container
                            {
                                Name = "File selection",
                                RelativeSizeAxes = Axes.Both,
                                Child = beatmapTextBox = new FileChooserLabelledTextBox(configManager.GetBindable<string>(Settings.DefaultPath), ".osu")
                                {
                                    Label = "Beatmap",
                                    FixedLabelWidth = 160f,
                                    PlaceholderText = "Click to select a beatmap file"
                                }
                            }
                        },
                        new Drawable[]
                        {
                            new Container
                            {
                                Name = "Beatmap title",
                                RelativeSizeAxes = Axes.Both,
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
                            }
                        },
                        new Drawable[]
                        {
                            beatmapDataContainer = new FillFlowContainer
                            {
                                Name = "Beatmap data",
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
                                                accuracyContainer = new GridContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    ColumnDimensions = new[]
                                                    {
                                                        new Dimension(),
                                                        new Dimension(GridSizeMode.Absolute),
                                                        new Dimension(GridSizeMode.Absolute),
                                                        new Dimension(GridSizeMode.AutoSize)
                                                    },
                                                    RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                                                    Content = new[]
                                                    {
                                                        new Drawable[]
                                                        {
                                                            accuracyTextBox = new LimitedLabelledFractionalNumberBox
                                                            {
                                                                RelativeSizeAxes = Axes.X,
                                                                Anchor = Anchor.TopLeft,
                                                                Label = "Accuracy",
                                                                PlaceholderText = "100",
                                                                MaxValue = 100.0,
                                                                MinValue = 0.0,
                                                                Value = { Value = 100.0 }
                                                            },
                                                            goodsTextBox = new LimitedLabelledNumberBox
                                                            {
                                                                RelativeSizeAxes = Axes.X,
                                                                Anchor = Anchor.TopLeft,
                                                                Label = "Goods",
                                                                PlaceholderText = "0",
                                                                MinValue = 0
                                                            },
                                                            mehsTextBox = new LimitedLabelledNumberBox
                                                            {
                                                                RelativeSizeAxes = Axes.X,
                                                                Anchor = Anchor.TopLeft,
                                                                Label = "Mehs",
                                                                PlaceholderText = "0",
                                                                MinValue = 0
                                                            },
                                                            fullScoreDataSwitch = new SwitchButton
                                                            {
                                                                Width = 80,
                                                                Height = 40
                                                            }
                                                        }
                                                    }
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
                                            Spacing = new Vector2(0, 5f),
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
                                                },
                                                new OsuButton()
                                                {
                                                    Anchor = Anchor.TopCentre,
                                                    Origin = Anchor.TopCentre,
                                                    Width = 200,
                                                    Text = "Inspect Object Difficulty Data",
                                                    Action = () =>
                                                    {
                                                        AddInternal(new Container
                                                        {
                                                            Name = "Object inspector overlay",
                                                            RelativeSizeAxes = Axes.Both,
                                                            Anchor = Anchor.Centre,
                                                            Origin = Anchor.Centre,
                                                            Child = objectInspector = new ObjectInspector(working)
                                                            {
                                                                RelativeSizeAxes = Axes.Both,
                                                            },
                                                        });
                                                        objectInspector.Show();
                                                    }
                                                },
                                            }
                                        }
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
                    Child = userModsSelectOverlay = new UserModSelectOverlay
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        IsValidMod = (mod) => mod.HasImplementation && ModUtils.FlattenMod(mod).All(m => m.UserPlayable),
                        SelectedMods = { BindTarget = appliedMods }
                    },
                }
            };

            beatmapDataContainer.Hide();
            userModsSelectOverlay.Hide();

            beatmapTextBox.Current.BindValueChanged(beatmapChanged);

            accuracyTextBox.Value.BindValueChanged(_ => calculatePerformance());
            goodsTextBox.Value.BindValueChanged(_ => calculatePerformance());
            mehsTextBox.Value.BindValueChanged(_ => calculatePerformance());
            missesTextBox.Value.BindValueChanged(_ => calculatePerformance());
            comboTextBox.Value.BindValueChanged(_ => calculatePerformance());
            scoreTextBox.Value.BindValueChanged(_ => calculatePerformance());

            fullScoreDataSwitch.Current.BindValueChanged(val => updateAccuracyParams(val.NewValue));

            appliedMods.BindValueChanged(modsChanged);
            modDisplay.Current.BindTo(appliedMods);

            ruleset.BindValueChanged(_ =>
            {
                appliedMods.Value = Array.Empty<Mod>();
                updateAccuracyParams(fullScoreDataSwitch.Current.Value);
                calculateDifficulty();
            });

            if (RuntimeInfo.IsDesktop)
                HotReloadCallbackReceiver.CompilationFinished += _ => Schedule(calculateDifficulty);
        }

        protected override void Dispose(bool isDisposing)
        {
            modSettingChangeTracker?.Dispose();
            appliedMods.Value = Array.Empty<Mod>();

            base.Dispose(isDisposing);
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

            working = ProcessorWorkingBeatmap.FromFileOrId(filePath.NewValue, audio);

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

            int? countGood = null, countMeh = null;

            if (fullScoreDataSwitch.Current.Value)
            {
                countGood = goodsTextBox.Value.Value;
                countMeh = mehsTextBox.Value.Value;
            }

            var score = RulesetHelper.AdjustManiaScore(scoreTextBox.Value.Value, appliedMods.Value);

            var statistics = RulesetHelper.GenerateHitResultsForRuleset(ruleset.Value, accuracyTextBox.Value.Value / 100.0, working.GetPlayableBeatmap(ruleset.Value, appliedMods.Value), missesTextBox.Value.Value, countMeh, countGood);

            var performanceCalculator = ruleset.Value.CreateInstance().CreatePerformanceCalculator(difficultyAttributes, new ScoreInfo
            {
                Accuracy = RulesetHelper.GetAccuracyForRuleset(ruleset.Value, statistics),
                MaxCombo = comboTextBox.Value.Value,
                Statistics = statistics,
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
                    Label = x.Key.Humanize(),
                    Text = FormattableString.Invariant($"{x.Value:N2}")
                }
            ).ToArray();
        }

        private void populateScoreParams()
        {
            accuracyContainer.Hide();
            comboTextBox.Hide();
            missesTextBox.Hide();
            scoreTextBox.Hide();

            // TODO: other rulesets?

            if (ruleset.Value.ShortName == "osu" || ruleset.Value.ShortName == "taiko" || ruleset.Value.ShortName == "fruits")
            {
                updateAccuracyParams(fullScoreDataSwitch.Current.Value);
                accuracyContainer.Show();

                comboTextBox.PlaceholderText = difficultyAttributes.MaxCombo.ToString();
                comboTextBox.MaxValue = comboTextBox.Value.Value = difficultyAttributes.MaxCombo;
                comboTextBox.Show();

                missesTextBox.MaxValue = difficultyAttributes.MaxCombo;
                missesTextBox.Text = string.Empty;
                missesTextBox.Show();
            }
            else if (ruleset.Value.ShortName == "mania")
            {
                scoreTextBox.Text = string.Empty;
                scoreTextBox.Show();
            }
        }

        private void updateAccuracyParams(bool useFullScoreData)
        {
            goodsTextBox.Text = string.Empty;
            mehsTextBox.Text = string.Empty;
            accuracyTextBox.Text = string.Empty;

            if (useFullScoreData)
            {
                goodsTextBox.Label = ruleset.Value.ShortName switch
                {
                    "osu" => "100s",
                    "taiko" => "Goods",
                    "fruits" => "Droplets",
                    _ => ""
                };

                mehsTextBox.Label = ruleset.Value.ShortName switch
                {
                    "osu" => "50s",
                    "fruits" => "Tiny Droplets",
                    _ => ""
                };

                accuracyContainer.ColumnDimensions = ruleset.Value.ShortName switch
                {
                    "osu" or "fruits" =>
                        new[]
                        {
                            new Dimension(GridSizeMode.Absolute),
                            new Dimension(),
                            new Dimension(),
                            new Dimension(GridSizeMode.AutoSize)
                        },
                    "taiko" =>
                        new[]
                        {
                            new Dimension(GridSizeMode.Absolute),
                            new Dimension(),
                            new Dimension(GridSizeMode.Absolute),
                            new Dimension(GridSizeMode.AutoSize)
                        },
                    _ => Array.Empty<Dimension>()
                };
            }
            else
            {
                accuracyContainer.ColumnDimensions = new[]
                {
                    new Dimension(),
                    new Dimension(GridSizeMode.Absolute),
                    new Dimension(GridSizeMode.Absolute),
                    new Dimension(GridSizeMode.AutoSize)
                };
            }
        }
    }
}
