// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osuTK;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Game.Overlays.Toolbar;
using osu.Framework.Bindables;
using osu.Game.Scoring;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Osu.Mods;

namespace PerformanceCalculatorGUI.Components
{
    public partial class LazerCalculationSettings : ToolbarButton, IHasPopover
    {
        public readonly Bindable<bool> CalculateRankedMaps = new Bindable<bool>(true);
        public readonly Bindable<bool> CalculateUnrankedMaps = new Bindable<bool>(false);

        public readonly Bindable<bool> CalculateUnsubmittedScores = new Bindable<bool>(true);
        public readonly Bindable<bool> CalculateUnrankedMods = new Bindable<bool>(true);

        public readonly Bindable<bool> EnableScorev1Overwrite = new Bindable<bool>(false);

        public bool IsScorev1OverwritingEnabled => EnableScorev1Overwrite.Value;

        protected override Anchor TooltipAnchor => Anchor.TopRight;

        public LazerCalculationSettings()
        {
            TooltipMain = "Calculation Settings";

            SetIcon(new ScreenSelectionButtonIcon(FontAwesome.Solid.Cog) { IconSize = new Vector2(70) });
        }

        public bool ShouldBeFiltered(ScoreInfo score)
        {
            if (score.BeatmapInfo == null)
                return true;

            if (!CalculateRankedMaps.Value && score.BeatmapInfo.Status.GrantsPerformancePoints())
                return true;

            if (!CalculateUnrankedMaps.Value && !score.BeatmapInfo.Status.GrantsPerformancePoints())
                return true;

            if (!CalculateUnrankedMods.Value)
            {
                // Check for legacy score because CL is unranked
                if (!score.Mods.All(m => m.Ranked || (score.IsLegacyScore && m is OsuModClassic)))
                    return true;
            }

            if (!CalculateUnsubmittedScores.Value)
            {
                if (score.OnlineID <= 0 && score.LegacyOnlineID <= 0)
                    return true;
            }

            return false;
        }

        public Popover GetPopover() => new LazerCalculationSettingsPopover(this);

        protected override bool OnClick(ClickEvent e)
        {
            this.ShowPopover();
            return base.OnClick(e);
        }
    }

    public partial class LazerCalculationSettingsPopover : OsuPopover
    {
        private readonly LazerCalculationSettings parent;

        public LazerCalculationSettingsPopover(LazerCalculationSettings parent)
        {
            this.parent = parent;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new Container
            {
                AutoSizeAxes = Axes.Y,
                Width = 500,
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
                            new OsuCheckbox
                            {
                                LabelText = "Calculate Ranked Maps",
                                Current = { BindTarget = parent.CalculateRankedMaps }
                            },
                            new OsuCheckbox
                            {
                                LabelText = "Calculate Unranked Maps",
                                Current = { BindTarget = parent.CalculateUnrankedMaps }
                            },
                            new OsuCheckbox
                            {
                                LabelText = "Calculate Unsubmitted Scores, such as scores set on local difficulties",
                                Current = { BindTarget = parent.CalculateUnsubmittedScores }
                            },
                            new OsuCheckbox
                            {
                                LabelText = "Calculate Unranked Mods, Autopilot is excluded regardless",
                                Current = { BindTarget = parent.CalculateUnsubmittedScores }
                            },
                            new OsuCheckbox
                            {
                                LabelText = "Enable Scorev1 score overwrite for legacy scores",
                                Current = { BindTarget = parent.EnableScorev1Overwrite }
                            },
                        }
                    }
                }
            });
        }
    }
}
