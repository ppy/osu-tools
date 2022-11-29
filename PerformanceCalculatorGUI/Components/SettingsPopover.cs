// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Online.Chat;
using osuTK;
using PerformanceCalculatorGUI.Components.TextBoxes;
using PerformanceCalculatorGUI.Configuration;

namespace PerformanceCalculatorGUI.Components
{
    public partial class SettingsPopover : OsuPopover
    {
        private SettingsManager configManager;

        private LinkFlowContainer linkContainer;

        private Bindable<string> clientIdBindable;
        private Bindable<string> clientSecretBindable;
        private Bindable<string> pathBindable;
        private Bindable<string> cacheBindable;
        private Bindable<float> scaleBindable;

        private const string api_key_link = "https://osu.ppy.sh/home/account/edit#new-oauth-application";

        [BackgroundDependencyLoader]
        private void load(SettingsManager configManager, OsuConfigManager osuConfig)
        {
            this.configManager = configManager;
            clientIdBindable = configManager.GetBindable<string>(Settings.ClientId);
            clientSecretBindable = configManager.GetBindable<string>(Settings.ClientSecret);
            pathBindable = configManager.GetBindable<string>(Settings.DefaultPath);
            cacheBindable = configManager.GetBindable<string>(Settings.CachePath);
            scaleBindable = osuConfig.GetBindable<float>(OsuSetting.UIScale);

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
                            new LabelledTextBox
                            {
                                RelativeSizeAxes = Axes.X,
                                Label = "Beatmap cache path",
                                Current = { BindTarget = cacheBindable }
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.X,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Size = new Vector2(0.8f, 3f),
                                Colour = OsuColour.Gray(0.5f)
                            },
                            new LabelledSliderBar<float>
                            {
                                RelativeSizeAxes = Axes.X,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Label = "UI Scale",
                                Current = { BindTarget = scaleBindable }
                            },
                            new RoundedButton
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
