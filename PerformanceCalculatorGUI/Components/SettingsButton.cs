// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Beatmaps.Drawables.Cards;
using osu.Game.Input.Bindings;
using osu.Game.Overlays.Toolbar;
using osuTK;

namespace PerformanceCalculatorGUI.Components
{
    public class SettingsIconPill : IconPill
    {
        public SettingsIconPill()
            : base(FontAwesome.Solid.Cog)
        {
        }

        public override LocalisableString TooltipText => "Settings";
    }

    internal class SettingsButton : ToolbarButton, IHasPopover
    {
        public SettingsButton()
        {
            Width *= 1.4f;
            Hotkey = GlobalAction.ToggleSettings;
            TooltipText = "Settings";

            SetIcon(new SettingsIconPill { IconSize = new Vector2(80) });
        }

        public Popover GetPopover() => new SettingsPopover();

        protected override bool OnClick(ClickEvent e)
        {
            this.ShowPopover();
            return base.OnClick(e);
        }
    }
}
