// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osu.Game.Overlays.Mods;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Utils;
using osuTK;
using PerformanceCalculatorGUI.Components.TextBoxes;

namespace PerformanceCalculatorGUI.Components
{
    public partial class BeatmapCard : OsuClickableContainer, IHasCustomTooltip<ProcessorWorkingBeatmap>
    {
        private readonly ProcessorWorkingBeatmap beatmap;

        [Resolved(canBeNull: true)]
        private OverlayColourProvider colourProvider { get; set; }

        [Resolved]
        private OsuColour colours { get; set; }

        [Resolved]
        private LargeTextureStore textures { get; set; }

        [Resolved]
        private Bindable<IReadOnlyList<Mod>> mods { get; set; }

        public ITooltip<ProcessorWorkingBeatmap> GetCustomTooltip() => new BeatmapCardTooltip(colourProvider);
        public ProcessorWorkingBeatmap TooltipContent { get; }

        private ModSettingChangeTracker modSettingChangeTracker;
        private OsuSpriteText bpmText = null!;

        public BeatmapCard(ProcessorWorkingBeatmap beatmap)
            : base(HoverSampleSet.Button)
        {
            this.beatmap = beatmap;
            RelativeSizeAxes = Axes.X;
            Height = 40;
            CornerRadius = ExtendedLabelledTextBox.CORNER_RADIUS;
            TooltipContent = beatmap;
        }

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            Masking = true;
            BorderColour = colourProvider?.Light1 ?? colours.GreyVioletLighter;

            AddRange(new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider?.Background5 ?? colours.Gray1
                },
                new BufferedContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0.3f,
                    Children = new Drawable[]
                    {
                        new Sprite
                        {
                            RelativeSizeAxes = Axes.Both,
                            Texture = textures.Get($"https://assets.ppy.sh/beatmaps/{beatmap.BeatmapSetInfo.OnlineID}/covers/cover.jpg"),
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            FillMode = FillMode.Fill
                        }
                    }
                },
                new OsuSpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Font = OsuFont.GetFont(size: 16, weight: FontWeight.Bold),
                    Text = $"[{beatmap.BeatmapInfo.Ruleset.Name}] {beatmap.Metadata.GetDisplayTitle()} [{beatmap.BeatmapInfo.DifficultyName}]",
                    Margin = new MarginPadding(10)
                },
                new FillFlowContainer
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Direction = FillDirection.Horizontal,
                    AutoSizeAxes = Axes.Both,
                    Margin = new MarginPadding(10),
                    Children = new Drawable[]
                    {
                        new BeatmapStatisticIcon(BeatmapStatisticsIconType.Bpm)
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(16)
                        },
                        bpmText = new OsuSpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Font = OsuFont.GetFont(size: 14)
                        }
                    }
                }
            });

            Action = () => { host.OpenUrlExternally($"https://osu.ppy.sh/beatmaps/{beatmap.BeatmapInfo.OnlineID}"); };

            mods.BindValueChanged(_ =>
            {
                modSettingChangeTracker?.Dispose();
                modSettingChangeTracker = new ModSettingChangeTracker(mods.Value);
                modSettingChangeTracker.SettingChanged += _ => updateBpm();
                updateBpm();
            }, true);

            updateBpm();
        }

        protected override bool OnHover(HoverEvent e)
        {
            BorderThickness = 2;
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            BorderThickness = 0;
            base.OnHoverLost(e);
        }

        private void updateBpm()
        {
            double rate = ModUtils.CalculateRateWithMods(mods.Value);

            int bpmMax = FormatUtils.RoundBPM(beatmap.Beatmap.ControlPointInfo.BPMMaximum, rate);
            int bpmMin = FormatUtils.RoundBPM(beatmap.Beatmap.ControlPointInfo.BPMMinimum, rate);
            int mostCommonBPM = FormatUtils.RoundBPM(60000 / beatmap.Beatmap.GetMostCommonBeatLength(), rate);

            string labelText = bpmMin == bpmMax
                ? $"{bpmMin}"
                : $"{bpmMin}-{bpmMax} (mostly {mostCommonBPM})";

            bpmText.Text = labelText;
        }

        public partial class BeatmapCardTooltip : VisibilityContainer, ITooltip<ProcessorWorkingBeatmap>
        {
            public BeatmapCardTooltip(OverlayColourProvider colourProvider)
            {
                this.colourProvider = colourProvider;
                AutoSizeAxes = Axes.Both;
                Masking = true;
                CornerRadius = 8;
            }

            protected override void PopIn() => this.FadeIn(150, Easing.OutQuint);
            protected override void PopOut() => this.Delay(150).FadeOut(500, Easing.OutQuint);

            public void Move(Vector2 pos) => Position = pos;

            private ProcessorWorkingBeatmap beatmap;

            private FillFlowContainer<VerticalAttributeDisplay> attributeContainer = null!;

            [Resolved]
            private Bindable<IReadOnlyList<Mod>> mods { get; set; }

            private readonly OverlayColourProvider colourProvider;

            private ModSettingChangeTracker modSettingChangeTracker;

            [Resolved]
            private IBindable<RulesetInfo> ruleset { get; set; }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                mods.BindValueChanged(_ =>
                {
                    modSettingChangeTracker?.Dispose();
                    modSettingChangeTracker = new ModSettingChangeTracker(mods.Value);
                    modSettingChangeTracker.SettingChanged += _ => updateValues();
                    updateValues();
                }, true);

                ruleset.BindValueChanged(_ => updateValues());
            }

            protected override bool OnMouseDown(MouseDownEvent e) => true;

            protected override bool OnClick(ClickEvent e) => true;

            private void updateValues() => Scheduler.AddOnce(() =>
            {
                if (beatmap?.BeatmapInfo == null)
                    return;

                Ruleset rulesetInstance = ruleset.Value.CreateInstance();
                var displayAttributes = rulesetInstance.GetBeatmapAttributesForDisplay(beatmap.BeatmapInfo, mods.Value).ToList();

                // make sure we have enough displays
                for (int i = attributeContainer.Count; i < displayAttributes.Count; i++)
                    attributeContainer.Add(new VerticalAttributeDisplay());

                // populate all visible attribute displays
                for (int i = 0; i < displayAttributes.Count; i++)
                    attributeContainer[i].SetAttribute(displayAttributes[i]);

                // and hide any extra ones
                for (int i = displayAttributes.Count; i < attributeContainer.Count; i++)
                    attributeContainer[i].SetAttribute(null);
            });

            public void SetContent(ProcessorWorkingBeatmap content)
            {
                if (content == beatmap && Children.Any())
                    return;

                beatmap = content;

                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background6
                    },
                    attributeContainer = new FillFlowContainer<VerticalAttributeDisplay>
                    {
                        Padding = new MarginPadding { Vertical = 24, Horizontal = 8 },
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal
                    }
                };
            }
        }
    }
}
