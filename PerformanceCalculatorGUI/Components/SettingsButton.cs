// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Game.Input.Bindings;

namespace PerformanceCalculatorGUI.Components
{
    public partial class SettingsButton : ScreenSelectionButton, IHasPopover
    {
        public SettingsButton()
            : base("Settings", FontAwesome.Solid.Cog, GlobalAction.ToggleSettings)
        {
        }

        public Popover GetPopover() => new SettingsPopover();

        protected override bool OnClick(ClickEvent e)
        {
            this.ShowPopover();
            return base.OnClick(e);
        }
    }
}
