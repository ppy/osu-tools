// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osuTK;
using osu.Game.Users.Drawables;

namespace PerformanceCalculatorGUI.Components
{
    public partial class ExtendedCombinedProfileScore : ExtendedProfileScore
    {
        private APIUser User;

        public ExtendedCombinedProfileScore(ExtendedScore score, APIUser user)
            : base(score)
        {
            User = user;
        }

        [BackgroundDependencyLoader]
        private void load(RulesetStore rulesets)
        {
            AddInternal(new UpdateableAvatar(User, false)
            {
                Size = new Vector2(height)
            });
        }

        protected override ExtendedProfileItemContainer ProfileScoreItems(RulesetStore rulesets)
        {
            var items = new ExtendedProfileItemContainer {
                // This doesn't show the rounded corners on the right hand side but it's the best I could figure out, please feel free to improve it
                Position = new Vector2(height, 0),
                Padding = new MarginPadding { Right = height },

                OnHoverAction = () =>
                {
                    positionChangeText.Text = $"#{Score.Position.Value}";
                },
                OnUnhoverAction = () =>
                {
                    positionChangeText.Text = $"{Score.PositionChange.Value:+0;-0;-}";
                },
                Children = ProfileScoreDrawables(rulesets)
            };

            return items;
        }
    }
}
