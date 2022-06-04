
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
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Logging;
using osu.Game.Graphics.Containers;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.BeatmapSet.Scores;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;
using osuTK;
using PerformanceCalculatorGUI.Components;
using PerformanceCalculatorGUI.Configuration;

namespace PerformanceCalculatorGUI.Screens
{
    internal class BeatmapLeaderboardScreen : PerformanceCalculatorScreen
    {
        private LimitedLabelledNumberBox beatmapIdTextBox;
        private StatefulButton calculationButton;
        private VerboseLoadingLayer loadingLayer;

        private ScoreTable scoreTable;

        private BufferedContainer background;

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

        [Resolved]
        private LargeTextureStore textures { get; set; }

        public override bool ShouldShowConfirmationDialogOnSwitch => false;

        private const int settings_height = 40;

        public BeatmapLeaderboardScreen()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    ColumnDimensions = new[] { new Dimension() },
                    RowDimensions = new[] { new Dimension(GridSizeMode.Absolute, 40), new Dimension() },
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
                            new OsuScrollContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Child = scoreTable = new ScoreTable
                                {
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                }
                            }
                        }
                    }
                },
                loadingLayer = new VerboseLoadingLayer(true)
                {
                    RelativeSizeAxes = Axes.Both
                }
            };

            beatmapIdTextBox.OnCommit += (_, _) => { calculate(); };

            if (RuntimeInfo.IsDesktop)
                HotReloadCallbackReceiver.CompilationFinished += _ => Schedule(calculate);
        }

        private void calculate()
        {
            loadingLayer.Show();
            calculationButton.State.Value = ButtonState.Loading;

            calculationCancellatonToken = new CancellationTokenSource();
            var token = calculationCancellatonToken.Token;

            Task.Run(async () =>
            {
                Schedule(() => loadingLayer.Text.Value = "Getting leaderboard...");

                var leaderboard = await apiManager.GetJsonFromApi<APIScoresCollection>($@"beatmaps/{beatmapIdTextBox.Current.Value}/scores?scope=global&mode={ruleset.Value.ShortName}");

                var plays = new List<ExtendedScore>();

                var rulesetInstance = ruleset.Value.CreateInstance();

                var working = ProcessorWorkingBeatmap.FromFileOrId(beatmapIdTextBox.Current.Value, cachePath: configManager.GetBindable<string>(Settings.CachePath).Value);

                Schedule(() => loadBackground(working.BeatmapInfo?.BeatmapSet?.OnlineID.ToString()));

                foreach (var score in leaderboard.Scores)
                {
                    if (token.IsCancellationRequested)
                        return;

                    Schedule(() => loadingLayer.Text.Value = $"Calculating {score.User.Username}");

                    var modsAcronyms = score.Mods.Select(x => x.ToString()).ToArray();
                    Mod[] mods = rulesetInstance.CreateAllMods().Where(m => modsAcronyms.Contains(m.Acronym)).ToArray();

                    var scoreInfo = new ScoreInfo(working.BeatmapInfo, ruleset.Value)
                    {
                        TotalScore = score.TotalScore,
                        MaxCombo = score.MaxCombo,
                        Mods = mods,
                        Statistics = new Dictionary<HitResult, int>()
                    };

                    scoreInfo.SetCount300(score.Statistics["count_300"]);
                    scoreInfo.SetCountGeki(score.Statistics["count_geki"]);
                    scoreInfo.SetCount100(score.Statistics["count_100"]);
                    scoreInfo.SetCountKatu(score.Statistics["count_katu"]);
                    scoreInfo.SetCount50(score.Statistics["count_50"]);
                    scoreInfo.SetCountMiss(score.Statistics["count_miss"]);

                    var parsedScore = new ProcessorScoreDecoder(working).Parse(scoreInfo);

                    var difficultyCalculator = rulesetInstance.CreateDifficultyCalculator(working);
                    var difficultyAttributes = difficultyCalculator.Calculate(RulesetHelper.ConvertToLegacyDifficultyAdjustmentMods(rulesetInstance, mods));
                    var performanceCalculator = rulesetInstance.CreatePerformanceCalculator();

                    var livePp = score.PP ?? 0.0;
                    var perfAttributes = performanceCalculator?.Calculate(parsedScore.ScoreInfo, difficultyAttributes);
                    score.PP = perfAttributes?.Total ?? 0.0;

                    var extendedScore = new ExtendedScore(score, livePp, perfAttributes);
                    plays.Add(extendedScore);
                }

                Schedule(() =>
                {
                    scoreTable.DisplayScores(plays.Select(x => x.CreateScoreInfo(rulesets, working.BeatmapInfo)).ToList(), true);
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

        private void loadBackground(string beatmapId)
        {
            if (background is not null)
            {
                RemoveInternal(background);
            }

            if (!string.IsNullOrEmpty(beatmapId))
            {
                LoadComponentAsync(background = new BufferedContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Depth = 99,
                    BlurSigma = new Vector2(6),
                    Children = new Drawable[]
                    {
                        new Sprite
                        {
                            RelativeSizeAxes = Axes.Both,
                            Texture = textures.Get($"https://assets.ppy.sh/beatmaps/{beatmapId}/covers/cover.jpg"),
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            FillMode = FillMode.Fill
                        },
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = colourProvider.Background6,
                            Alpha = 0.9f
                        },
                    }
                }).ContinueWith(_ =>
                {
                    Schedule(() =>
                    {
                        AddInternal(background);
                    });
                });
            }
        }
    }
}
