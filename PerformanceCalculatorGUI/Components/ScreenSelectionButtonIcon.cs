// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Beatmaps.Drawables.Cards;

namespace PerformanceCalculatorGUI.Components;

public partial class ScreenSelectionButtonIcon : IconPill
{
    public ScreenSelectionButtonIcon(IconUsage? icon = null)
        : base(icon ?? FontAwesome.Solid.List)
    {
    }

    public override LocalisableString TooltipText => string.Empty;
}
