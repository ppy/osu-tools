// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
using osu.Game.Graphics.Containers;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.BeatmapSet.Scores;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Users;
using osu.Game.Utils;
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

        private Container beatmapPanelContainer;
        private BeatmapCard beatmapPanel;

        private CancellationTokenSource calculationCancellatonToken;

        [Cached]
        private OverlayColourProvider colourProvider = new(OverlayColourScheme.Orange);

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
        private ScoreManager scoreManager { get; set; }

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

                var random = false;

                if (leaderboard.Scores.Count == 0)
                {
                    leaderboard.Scores = generateRandomScores(working);
                    random = true;
                }

                foreach (var score in leaderboard.Scores)
                {
                    if (token.IsCancellationRequested)
                        return;

                    Schedule(() => loadingLayer.Text.Value = $"Calculating {score.User?.Username}");

                    var scoreInfo = score.ToScoreInfo(rulesets, working.BeatmapInfo);

                    var parsedScore = new ProcessorScoreDecoder(working).Parse(scoreInfo);

                    var difficultyCalculator = rulesetInstance.CreateDifficultyCalculator(working);

                    Mod[] mods = score.Mods.Select(x => x.ToMod(rulesetInstance)).ToArray();
                    if (!random)
                        mods = RulesetHelper.ConvertToLegacyDifficultyAdjustmentMods(rulesetInstance, mods);

                    var difficultyAttributes = difficultyCalculator.Calculate(mods);
                    var performanceCalculator = rulesetInstance.CreatePerformanceCalculator();

                    var perfAttributes = performanceCalculator?.Calculate(parsedScore.ScoreInfo, difficultyAttributes);
                    score.PP = perfAttributes?.Total ?? 0.0;

                    plays.Add(score);
                }

                var sortedScores = scoreManager.OrderByTotalScore(plays.Select(x => x.ToScoreInfo(rulesets, working.BeatmapInfo))).ToList();

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

        private List<SoloScoreInfo> generateRandomScores(ProcessorWorkingBeatmap working)
        {
            var scores = new List<SoloScoreInfo>();

            var rng = new Random();

            var rulesetInstance = ruleset.Value.CreateInstance();
            var diffCalculator = rulesetInstance.CreateDifficultyCalculator(working);

            var allowedMods = ModUtils.FlattenMods(diffCalculator.CreateDifficultyAdjustmentModCombinations())
                                      .Distinct()
                                      .Where(x => x.GetType() != typeof(ModNoMod))
                                      .ToArray();

            var difficultyAttributes = diffCalculator.Calculate();

            for (var i = 0; i < generate_score_amount; i++)
            {
                var appliedMods = new List<Mod>();

                for (var m = 0; m < rng.Next(0, generate_score_max_mod_amount); m++)
                {
                    var mod = allowedMods[rng.Next(0, allowedMods.Length)];

                    if (appliedMods.SelectMany(x => x.IncompatibleMods).Any(c => c == mod.GetType() || c.IsInstanceOfType(mod)) ||
                        appliedMods.Any(c => c.GetType() == mod.GetType()))
                    {
                        m--;
                        continue;
                    }

                    appliedMods.Add(mod);
                }

                appliedMods = appliedMods.ToList();

                const double min_count300_ratio = 0.8; // ratio of the least amount of 300s out of all objects, i.e "there should be at least 800 300s out of 1000 objects"
                const double min_count100_ratio = 0.85; // ratio of the least amount of 100s out of remaining unjudged objects, i.e "there should be at least 170 100s out of remaining 200 objects"
                const double min_count50_ratio = 0.5; // ratio of the least amount of 50s out of remaining unjudged objects, i.e "there should be at least 15 50s out of remaining 30 objects"

                var unjudgedObjects = working.Beatmap.HitObjects.Count;
                var count300 = rng.Next((int)(working.Beatmap.HitObjects.Count * min_count300_ratio), unjudgedObjects + 1);
                unjudgedObjects -= count300;

                var count100 = rng.Next((int)(unjudgedObjects * min_count100_ratio), unjudgedObjects + 1);
                unjudgedObjects -= count100;

                var count50 = rng.Next((int)(unjudgedObjects * min_count50_ratio), unjudgedObjects + 1);
                unjudgedObjects -= count50;

                var countMiss = unjudgedObjects;

                var combo = difficultyAttributes.MaxCombo;
                if (countMiss > 0)
                    combo = rng.Next((int)(0.5 * difficultyAttributes.MaxCombo) / countMiss, Math.Min(difficultyAttributes.MaxCombo, (int)(difficultyAttributes.MaxCombo / (0.1 * countMiss))));

                var statistics = new Dictionary<HitResult, int>
                {
                    { HitResult.Great, count300 },
                    { HitResult.Ok, count100 },
                    { HitResult.Meh, count50 },
                    { HitResult.Miss, countMiss }
                };

                var accuracy = RulesetHelper.GetAccuracyForRuleset(ruleset.Value, statistics);

                var scoreInfo = new ScoreInfo(working.BeatmapInfo, ruleset.Value)
                {
                    Statistics = statistics,
                    MaxCombo = combo,
                    Accuracy = accuracy
                };

                var scoreProcessor = rulesetInstance.CreateScoreProcessor();
                scoreProcessor.Mods.Value = appliedMods;
                scoreProcessor.Accuracy.Value = accuracy;

                scores.Add(new SoloScoreInfo
                {
                    Rank = scoreProcessor.Rank.Value,
                    Accuracy = accuracy,
                    TotalScore = (int)scoreProcessor.ComputeScore(ScoringMode.Standardised, scoreInfo),
                    MaxCombo = combo,
                    Mods = appliedMods.Select(x => new APIMod(x)).ToArray(),
                    Statistics = statistics,
                    User = new APIUser
                    {
                        Username = $"dummy {i}",
                        CountryCode = CountryCode.Unknown
                    }
                });
            }

            return scores;
        }
    }
}
