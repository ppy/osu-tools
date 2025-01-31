// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osuTK.Graphics;
using PerformanceCalculatorGUI.Components;
using PerformanceCalculatorGUI.Components.TextBoxes;
using PerformanceCalculatorGUI.Configuration;

namespace PerformanceCalculatorGUI.Screens
{
    public partial class CombinedProfileScreen : ProfileScreen
    {
        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Plum);

        [Resolved]
        private NotificationDisplay notificationDisplay { get; set; }

        [Resolved]
        private APIManager apiManager { get; set; }

        [Resolved]
        private Bindable<RulesetInfo> ruleset { get; set; }

        [Resolved]
        private SettingsManager configManager { get; set; }

        [Resolved]
        private RulesetStore rulesets { get; set; }

        public CombinedProfileScreen()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            usernameTextBox.Label = "Usernames";
            usernameTextBox.PlaceholderText = "user1, user2, user3";
        }

        protected readonly struct ScoreWithUser
        {
            public readonly ExtendedScore Score;
            public readonly APIUser User;

            public ScoreWithUser(ExtendedScore score, APIUser user)
            {
                Score = score;
                User = user;
            }
        }

        protected async Task<List<ScoreWithUser>> GetPlaysWithUser(APIUser player, CancellationToken token)
        {
            var plays = await base.GetPlays(player, token);
            var playsWithUser = new List<ScoreWithUser>();

            foreach (ExtendedScore play in plays)
            {
                playsWithUser.Add(new ScoreWithUser(play, player));
            }

            return playsWithUser;
        }

        protected override void calculateProfile(string usernames)
        {
            currentUser = "";

            string[] Usernames = usernames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (Usernames.Count() < 1)
            {
                usernameTextBox.FlashColour(Color4.Red, 1);
                return;
            }

            calculationCancellatonToken?.Cancel();
            calculationCancellatonToken?.Dispose();

            loadingLayer.Show();
            calculationButton.State.Value = ButtonState.Loading;

            scores.Clear();

            calculationCancellatonToken = new CancellationTokenSource();
            var token = calculationCancellatonToken.Token;

            Task.Run(async () =>
            {
                Schedule(() =>
                {
                    sortingTabControl.Alpha = 1.0f;
                    sortingTabControl.Current.Value = ProfileSortCriteria.Local;

                    layout.RowDimensions = new[]
                    {
                        new Dimension(GridSizeMode.Absolute, username_container_height),
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension()
                    };
                });

                if (token.IsCancellationRequested)
                    return;

                var allPlays = new List<ScoreWithUser>();

                Schedule(() => loadingLayer.Text.Value = "Getting user data...");

                foreach (string username in Usernames)
                {
                    var player = await apiManager.GetJsonFromApi<APIUser>($"users/{username}/{ruleset.Value.ShortName}");

                    // Append player username to current user string
                    currentUser += (currentUser == "") ? player.Username : (", " + player.Username);
                    System.Console.WriteLine(currentUser);

                    var plays = await GetPlaysWithUser(player, token);
                    allPlays = allPlays.Concat(plays).ToList();
                }

                if (token.IsCancellationRequested)
                    return;

                Schedule(() => loadingLayer.Text.Value = "Filtering scores");

                var filteredPlays = new List<ScoreWithUser>();

                // List of every beatmap ID in combined plays without duplicates
                List<int> beatmapIDs = allPlays.Select(x => x.Score.SoloScore.BeatmapID).Distinct().ToList();

                foreach (int ID in beatmapIDs)
                {
                    List<ScoreWithUser> playsOnBeatmap = allPlays.Where(x => x.Score.SoloScore.BeatmapID == ID).OrderByDescending(x => x.Score.SoloScore.PP).ToList();
                    ScoreWithUser bestPlayOnBeatmap = playsOnBeatmap.First();

                    filteredPlays.Add(bestPlayOnBeatmap);
                    Schedule(() => scores.Add(new ExtendedCombinedProfileScore(bestPlayOnBeatmap.Score, bestPlayOnBeatmap.User)));
                }

                allPlays = filteredPlays;

                if (token.IsCancellationRequested)
                    return;

                var localOrdered = allPlays.OrderByDescending(x => x.Score.SoloScore.PP).ToList();
                var liveOrdered = allPlays.OrderByDescending(x => x.Score.LivePP ?? 0).ToList();

                Schedule(() =>
                {
                    foreach (var play in allPlays)
                    {
                        var score = play.Score;

                        if (score.LivePP != null)
                        {
                            score.Position.Value = localOrdered.IndexOf(play) + 1;
                            score.PositionChange.Value = liveOrdered.IndexOf(play) - localOrdered.IndexOf(play);
                        }
                    }
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
                    updateSorting(ProfileSortCriteria.Local);
                });
            }, TaskContinuationOptions.None);
        }
    }
}
