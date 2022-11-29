// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays.Rankings.Tables;
using osu.Game.Users;

namespace PerformanceCalculatorGUI.Components
{
    public class LeaderboardUser
    {
        public APIUser User;
        public decimal LivePP;
        public decimal LocalPP;
        public decimal Difference;
    }

    public partial class LeaderboardTable : RankingsTable<LeaderboardUser>
    {
        public LeaderboardTable(int page, IReadOnlyList<LeaderboardUser> rankings)
            : base(page, rankings)
        {
        }

        protected override RankingsTableColumn[] CreateAdditionalHeaders() => new[]
        {
            new RankingsTableColumn("New PP", Anchor.Centre, new Dimension(GridSizeMode.AutoSize), true),
            new RankingsTableColumn("Live PP", Anchor.Centre, new Dimension(GridSizeMode.AutoSize)),
        };

        protected override Drawable CreateRowBackground(LeaderboardUser item)
        {
            var background = base.CreateRowBackground(item);

            if (!item.User.Active)
                background.Alpha = 0.5f;

            return background;
        }

        protected sealed override Drawable[] CreateAdditionalContent(LeaderboardUser item) => new Drawable[]
        {
            new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Margin = new MarginPadding { Horizontal = 10, Bottom = 3 },
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Font = OsuFont.GetFont(size: TEXT_SIZE),
                        Text = item.LocalPP.ToLocalisableString(@"N0"),
                        Margin = new MarginPadding { Horizontal = 5 },
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    },
                    new DifferenceText(item.Difference)
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    }
                }
            },
            new ColouredRowText { Text = item.LivePP.ToLocalisableString(@"N0") }
        };

        protected sealed override CountryCode GetCountryCode(LeaderboardUser item) => item.User.CountryCode;

        protected sealed override Drawable CreateFlagContent(LeaderboardUser item)
        {
            var username = new LinkFlowContainer(t => t.Font = OsuFont.GetFont(size: TEXT_SIZE, italics: true))
            {
                AutoSizeAxes = Axes.X,
                RelativeSizeAxes = Axes.Y,
                TextAnchor = Anchor.CentreLeft
            };
            username.AddUserLink(item.User);
            return username;
        }

        private partial class DifferenceText : OsuSpriteText
        {
            private readonly decimal difference;

            public DifferenceText(decimal difference)
            {
                this.difference = difference;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                Font = OsuFont.GetFont(size: 10);
                Colour = difference < 0 ? colours.Red1 : difference == 0 ? colours.Yellow : colours.Green1;
                Text = $"{difference:+0.0;-0.0;-}";
            }
        }
    }
}
