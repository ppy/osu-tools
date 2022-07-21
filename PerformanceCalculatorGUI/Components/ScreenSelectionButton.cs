
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Beatmaps.Drawables.Cards;
using osu.Game.Input.Bindings;
using osu.Game.Overlays.Toolbar;
using osuTK;

namespace PerformanceCalculatorGUI.Components
{
    public class ScreenSelectionButtonIcon : IconPill
    {
        public ScreenSelectionButtonIcon(IconUsage? icon = null)
            : base(icon ?? FontAwesome.Solid.List)
        {
        }

        public override LocalisableString TooltipText { get; }
    }

    internal class ScreenSelectionButton : ToolbarButton
    {
        public ScreenSelectionButton(string title, IconUsage? icon = null, GlobalAction? hotkey = null)
        {
            Width = PerformanceCalculatorSceneManager.CONTROL_AREA_HEIGHT;
            Hotkey = hotkey;
            TooltipMain = title;

            SetIcon(new ScreenSelectionButtonIcon(icon) { IconSize = new Vector2(25) });
        }
    }
}
