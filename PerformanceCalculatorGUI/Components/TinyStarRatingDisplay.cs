// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Utils;
using osuTK;
using osuTK.Graphics;

namespace PerformanceCalculatorGUI.Components
{
    /// <summary>
    /// A pill that displays the star rating of a beatmap.
    /// </summary>
    public partial class TinyStarRatingDisplay : CompositeDrawable, IHasCustomTooltip<string>
    {
        private readonly DifficultyAttributes difficultyAttributes;
        private readonly Box background;
        private readonly SpriteIcon starIcon;
        private readonly OsuSpriteText starsText;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private OverlayColourProvider? colourProvider { get; set; }

        /// <summary>
        /// Creates a new <see cref="TinyStarRatingDisplay"/> using an already computed <see cref="DifficultyAttributes"/>.
        /// </summary>
        public TinyStarRatingDisplay(DifficultyAttributes difficultyAttributes)
        {
            this.difficultyAttributes = difficultyAttributes;

            AutoSizeAxes = Axes.Both;

            InternalChild = new CircularContainer
            {
                Masking = true,
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    background = new Box
                    {
                        Alpha = 0.75f,
                        RelativeSizeAxes = Axes.Both,
                    },
                    new GridContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        AutoSizeAxes = Axes.Both,
                        Margin = new MarginPadding { Horizontal = 5.5f },
                        ColumnDimensions = new[]
                        {
                            new Dimension(GridSizeMode.AutoSize),
                            new Dimension(GridSizeMode.Absolute, 1f),
                            new Dimension(GridSizeMode.AutoSize, minSize: 15f),
                        },
                        RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                        Content = new[]
                        {
                            new[]
                            {
                                starIcon = new SpriteIcon
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Icon = FontAwesome.Solid.Star,
                                    Size = new Vector2(5f),
                                },
                                Empty(),
                                starsText = new OsuSpriteText
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Margin = new MarginPadding { Bottom = 1.4f },
                                    Spacing = new Vector2(-1.4f),
                                    Font = OsuFont.Torus.With(size: 10.0f, weight: FontWeight.Bold, fixedWidth: true),
                                    Shadow = false,
                                },
                            }
                        }
                    },
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            starsText.Text = difficultyAttributes.StarRating < 0 ? "-" : difficultyAttributes.StarRating.FormatStarRating();

            background.Colour = colours.ForStarDifficulty(difficultyAttributes.StarRating).Darken(0.1f);

            starIcon.Colour = difficultyAttributes.StarRating >= OsuColour.STAR_DIFFICULTY_DEFINED_COLOUR_CUTOFF ? colours.Orange1 : colourProvider?.Background5 ?? Color4Extensions.FromHex("303d47");
            starsText.Colour = difficultyAttributes.StarRating >= OsuColour.STAR_DIFFICULTY_DEFINED_COLOUR_CUTOFF ? colours.Orange1 : colourProvider?.Background5 ?? Color4.Black.Opacity(0.75f);
        }

        public string TooltipContent => AttributeConversion.ToReadableString(difficultyAttributes);

        public ITooltip<string> GetCustomTooltip() => new MultilineTooltip();
    }
}
