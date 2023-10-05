// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Logging;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.BeatmapSet.Scores;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Scoring;
using PerformanceCalculatorGUI.Components;
using PerformanceCalculatorGUI.Components.TextBoxes;
using PerformanceCalculatorGUI.Configuration;

namespace PerformanceCalculatorGUI.Screens
{
    public partial class BeatmapLeaderboardScreen : PerformanceCalculatorScreen
    {
        private LimitedLabelledNumberBox beatmapIdTextBox;
        private StatefulButton calculationButton;
        private VerboseLoadingLayer loadingLayer;

        private GridContainer layout;
        private ScoreTable scoreTable;
        private OsuSpriteText noScoresPlaceholder;

        private Container beatmapPanelContainer;
        private BeatmapCard beatmapPanel;

        private CancellationTokenSource calculationCancellatonToken;

        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Orange);

        [Resolved]
        private NotificationDisplay notificationDisplay { get; set; }

        [Resolved]
        private APIManager apiManager { get; set; }

        [Resolved]
        private Bindable<RulesetInfo> ruleset { get; set; }

        [Resolved]
        private RulesetStore rulesets { get; set; }

        [Resolved]
        private SettingsManager configManager { get; set; }

        public override bool ShouldShowConfirmationDialogOnSwitch => false;

        private const int settings_height = 40;
        private const int generate_score_amount = 50;
        private const int generate_score_max_mod_amount = 4;

        public BeatmapLeaderboardScreen()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                layout = new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    ColumnDimensions = new[] { new Dimension() },
                    RowDimensions = new[] { new Dimension(GridSizeMode.Absolute, 40), new Dimension(GridSizeMode.Absolute), new Dimension() },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new GridContainer
                            {
                                Name = "Settings",
                                Height = settings_height,
                                RelativeSizeAxes = Axes.X,
                                ColumnDimensions = new[]
                                {
                                    new Dimension(),
                                    new Dimension(GridSizeMode.AutoSize)
                                },
                                RowDimensions = new[]
                                {
                                    new Dimension(GridSizeMode.AutoSize)
                                },
                                Content = new[]
                                {
                                    new Drawable[]
                                    {
                                        beatmapIdTextBox = new LimitedLabelledNumberBox
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Anchor = Anchor.TopLeft,
                                            Label = "Beatmap ID",
                                            PlaceholderText = "Enter beatmap ID",
                                            MinValue = 1,
                                            CommitOnFocusLoss = false
                                        },
                                        calculationButton = new StatefulButton("Start calculation")
                                        {
                                            Width = 150,
                                            Height = settings_height,
                                            Action = calculate
                                        }
                                    }
                                }
                            }
                        },
                        new Drawable[]
                        {
                            beatmapPanelContainer = new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y
                            }
                        },
                        new Drawable[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new OsuScrollContainer
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Children = new Drawable[]
                                        {
                                            new Box
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Colour = colourProvider.Background5
                                            },
                                            scoreTable = new ScoreTable
                                            {
                                                Anchor = Anchor.TopCentre,
                                                Origin = Anchor.TopCentre,
                                            }
                                        }
                                    },
                                    noScoresPlaceholder = new OsuSpriteText
                                    {
                                        Text = "No scores available :(",
                                        Font = OsuFont.Default.With(size: 24, weight: FontWeight.Bold),
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Alpha = 0
                                    }
                                },
                            }
                        }
                    }
                },
                loadingLayer = new VerboseLoadingLayer(true)
                {
                    RelativeSizeAxes = Axes.Both
                }
            };

            ruleset.BindValueChanged(_ => { calculate(); });
            beatmapIdTextBox.OnCommit += (_, _) => { calculate(); };

            if (RuntimeInfo.IsDesktop)
                HotReloadCallbackReceiver.CompilationFinished += _ => Schedule(calculate);
        }

        private void calculate()
        {
            calculationCancellatonToken?.Cancel();
            calculationCancellatonToken?.Dispose();

            scoreTable.Hide();
            loadingLayer.Show();
            calculationButton.State.Value = ButtonState.Loading;

            calculationCancellatonToken = new CancellationTokenSource();
            var token = calculationCancellatonToken.Token;

            Task.Run(async () =>
            {
                Schedule(() => loadingLayer.Text.Value = "Getting leaderboard...");

                var leaderboard = await apiManager.GetJsonFromApi<APIScoresCollection>($@"beatmaps/{beatmapIdTextBox.Current.Value}/scores?scope=global&mode={ruleset.Value.ShortName}");

                var plays = new List<SoloScoreInfo>();

                var rulesetInstance = ruleset.Value.CreateInstance();

                var working = ProcessorWorkingBeatmap.FromFileOrId(beatmapIdTextBox.Current.Value, cachePath: configManager.GetBindable<string>(Settings.CachePath).Value);

                Schedule(() =>
                {
                    if (beatmapPanel != null)
                        beatmapPanelContainer.Remove(beatmapPanel, true);

                    beatmapPanelContainer.Add(beatmapPanel = new BeatmapCard(working));

                    layout.RowDimensions = new[] { new Dimension(GridSizeMode.Absolute, 40), new Dimension(GridSizeMode.AutoSize), new Dimension() };
                });

                noScoresPlaceholder.Alpha = leaderboard.Scores.Count > 0 ? 0 : 1;

                if (leaderboard.Scores.Count == 0)
                    return;

                foreach (var score in leaderboard.Scores)
                {
                    if (token.IsCancellationRequested)
                        return;

                    Schedule(() => loadingLayer.Text.Value = $"Calculating {score.User?.Username}");

                    var scoreInfo = score.ToScoreInfo(rulesets, working.BeatmapInfo);

                    var parsedScore = new ProcessorScoreDecoder(working).Parse(scoreInfo);

                    var difficultyCalculator = rulesetInstance.CreateDifficultyCalculator(working);

                    Mod[] mods = score.Mods.Select(x => x.ToMod(rulesetInstance)).ToArray();
                    mods = RulesetHelper.ConvertToLegacyDifficultyAdjustmentMods(rulesetInstance, mods);

                    var difficultyAttributes = difficultyCalculator.Calculate(mods);
                    var performanceCalculator = rulesetInstance.CreatePerformanceCalculator();

                    var perfAttributes = performanceCalculator?.Calculate(parsedScore.ScoreInfo, difficultyAttributes);
                    score.PP = perfAttributes?.Total ?? 0.0;

                    plays.Add(score);
                }

                var sortedScores = plays.Select(x => x.ToScoreInfo(rulesets, working.BeatmapInfo)).OrderByTotalScore().ToList();

                Schedule(() =>
                {
                    scoreTable.DisplayScores(sortedScores, true);
                    scoreTable.Show();
                });
            }, token).ContinueWith(t =>
            {
                Logger.Log(t.Exception?.ToString(), level: LogLevel.Error);
                notificationDisplay.Display(new Notification(t.Exception?.Flatten().Message));
            }, TaskContinuationOptions.OnlyOnFaulted).ContinueWith(t =>
            {
                Schedule(() =>
                {
                    loadingLayer.Hide();
                    calculationButton.State.Value = ButtonState.Done;
                });
            }, token);
        }
    }
}
