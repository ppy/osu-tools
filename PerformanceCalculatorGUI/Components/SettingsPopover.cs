// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Online.Chat;
using osuTK;
using PerformanceCalculatorGUI.Configuration;

namespace PerformanceCalculatorGUI.Components
{
    internal class SettingsPopover : OsuPopover
    {
        private SettingsManager configManager;

        private LinkFlowContainer linkContainer;

        private Bindable<string> clientIdBindable;
        private Bindable<string> clientSecretBindable;
        private Bindable<string> pathBindable;

        private const string api_key_link = "https://osu.ppy.sh/home/account/edit#new-oauth-application";

        [BackgroundDependencyLoader]
        private void load(SettingsManager configManager)
        {
            this.configManager = configManager;
            clientIdBindable = configManager.GetBindable<string>(Settings.ClientId);
            clientSecretBindable = configManager.GetBindable<string>(Settings.ClientSecret);
            pathBindable = configManager.GetBindable<string>(Settings.DefaultPath);

            Add(new Container
            {
                AutoSizeAxes = Axes.Y,
                Width = 600,
                Children = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        Direction = FillDirection.Vertical,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Spacing = new Vector2(18),
                        Children = new Drawable[]
                        {
                            linkContainer = new LinkFlowContainer
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                AutoSizeAxes = Axes.Both,
                                Text = "You can get API key from "
                            },
                            new LabelledNumberBox
                            {
                                RelativeSizeAxes = Axes.X,
                                Label = "Client ID",
                                Current = { BindTarget = clientIdBindable }
                            },
                            new LabelledPasswordTextBox
                            {
                                RelativeSizeAxes = Axes.X,
                                Label = "Client Secret",
                                Current = { BindTarget = clientSecretBindable }
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.X,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Size = new Vector2(0.8f, 3f),
                                Colour = OsuColour.Gray(0.5f)
                            },
                            new LabelledTextBox
                            {
                                RelativeSizeAxes = Axes.X,
                                Label = "Default file path",
                                Current = { BindTarget = pathBindable }
                            },
                            new OsuButton
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Width = 150,
                                Height = 40,
                                Text = "Save",
                                Action = saveConfig
                            }
                        }
                    }
                }
            });

            linkContainer.AddLink("here", LinkAction.External, api_key_link, api_key_link);
        }

        private void saveConfig()
        {
            configManager.Save();

            this.HidePopover();
        }
    }
}
