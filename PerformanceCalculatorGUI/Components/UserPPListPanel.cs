// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Colour;
using osu.Framework.Extensions.Color4Extensions;
using osuTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osuTK;
using osu.Game.Users;

namespace PerformanceCalculatorGUI.Components
{
    public class UserPPListPanel : UserListPanel
    {
        private OsuSpriteText liveLabel;
        private OsuSpriteText localLabel;

        public Bindable<decimal> livePp = new Bindable<decimal>();
        public Bindable<decimal> localPp = new Bindable<decimal>();

        public UserPPListPanel(APIUser user)
            : base(user)
        {
            RelativeSizeAxes = Axes.X;
            Height = 40;
            CornerRadius = 6;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Background.Width = 0.5f;
            Background.Origin = Anchor.CentreRight;
            Background.Anchor = Anchor.CentreRight;
            Background.Colour = ColourInfo.GradientHorizontal(Color4.White.Opacity(1), Color4.White.Opacity(0.3f));

            livePp.ValueChanged += val => { liveLabel.Text = $"live pp: {val.NewValue:N1}"; };
            localPp.ValueChanged += val => { localLabel.Text = $"local pp: {val.NewValue:N1}"; };
        }

        protected override void LoadComplete()
        {
            Status.UnbindAll();
            Activity.UnbindAll();
        }

        protected override Drawable CreateLayout()
        {
            var layout = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(10, 0),
                        Children = new Drawable[]
                        {
                            CreateAvatar().With(avatar =>
                            {
                                avatar.Anchor = Anchor.CentreLeft;
                                avatar.Origin = Anchor.CentreLeft;
                                avatar.Size = new Vector2(40);
                            }),
                            CreateFlag().With(flag =>
                            {
                                flag.Anchor = Anchor.CentreLeft;
                                flag.Origin = Anchor.CentreLeft;
                            }),
                            CreateUsername().With(username =>
                            {
                                username.Anchor = Anchor.CentreLeft;
                                username.Origin = Anchor.CentreLeft;
                            }),
                            liveLabel = new OsuSpriteText
                            {
                                Colour = Colours.RedLighter,
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft
                            },
                            localLabel = new OsuSpriteText
                            {
                                Colour = Colours.BlueLighter,
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft
                            }
                        }
                    },
                }
            };

            return layout;
        }
    }
}
