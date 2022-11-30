// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using PerformanceCalculatorGUI.Components.TextBoxes;

namespace PerformanceCalculatorGUI.Components
{
    public partial class BeatmapCard : OsuClickableContainer
    {
        private readonly ProcessorWorkingBeatmap beatmap;

        [Resolved(canBeNull: true)]
        private OverlayColourProvider colourProvider { get; set; }

        [Resolved]
        private OsuColour colours { get; set; }

        [Resolved]
        private LargeTextureStore textures { get; set; }

        public BeatmapCard(ProcessorWorkingBeatmap beatmap)
            : base(HoverSampleSet.Button)
        {
            this.beatmap = beatmap;
            RelativeSizeAxes = Axes.X;
            Height = 40;
            CornerRadius = ExtendedLabelledTextBox.CORNER_RADIUS;
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
                }
            });

            Action = () => { host.OpenUrlExternally($"https://osu.ppy.sh/beatmaps/{beatmap.BeatmapInfo.OnlineID}"); };
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
    }
}
