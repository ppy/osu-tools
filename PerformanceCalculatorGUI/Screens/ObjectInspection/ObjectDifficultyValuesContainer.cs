// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Utils;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osuTK;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class ObjectDifficultyValuesContainer : Container
    {
        [Resolved]
        private Bindable<IReadOnlyList<Mod>> appliedMods { get; set; }

        private SpriteText hitObjectTypeText;

        private FillFlowContainer flowContainer;

        public Bindable<DifficultyHitObject> CurrentDifficultyHitObject { get; } = new Bindable<DifficultyHitObject>();

        private const int hit_object_type_container_height = 50;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colors)
        {
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colors.Background5
                },
                new OsuScrollContainer
                {
                    Padding = new MarginPadding(10) { Top = hit_object_type_container_height + 10 },
                    RelativeSizeAxes = Axes.Both,
                    ScrollbarAnchor = Anchor.TopLeft,
                    Child = flowContainer = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(10)
                    },
                },
                new Container
                {
                    Name = "Hit object type name container",
                    RelativeSizeAxes = Axes.X,
                    Height = hit_object_type_container_height,
                    Margin = new MarginPadding { Bottom = 10 },
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Colour = colors.Background6,
                            RelativeSizeAxes = Axes.Both
                        },
                        hitObjectTypeText = new SpriteText
                        {
                            Font = new FontUsage(size: 30),
                            Padding = new MarginPadding(10)
                        }
                    }
                }
            };

            CurrentDifficultyHitObject.ValueChanged += h => updateValues(h.NewValue);
        }

        private void updateValues(DifficultyHitObject hitObject)
        {
            flowContainer.Clear();

            if (hitObject == null)
            {
                hitObjectTypeText.Text = "";
                return;
            }

            hitObjectTypeText.Text = hitObject.BaseObject.GetType().Name;

            switch (hitObject)
            {
                case OsuDifficultyHitObject osuDifficultyHitObject:
                {
                    drawOsuValues(osuDifficultyHitObject);
                    break;
                }

                case TaikoDifficultyHitObject taikoDifficultyHitObject:
                {
                    drawTaikoValues(taikoDifficultyHitObject);
                    break;
                }

                case CatchDifficultyHitObject catchDifficultyHitObject:
                {
                    drawCatchValues(catchDifficultyHitObject);
                    break;
                }
            }
        }

        private void drawOsuValues(OsuDifficultyHitObject hitObject)
        {
            var hidden = appliedMods.Value.Any(x => x is ModHidden);
            flowContainer.AddRange(new[]
            {
                new ObjectInspectorDifficultyValue("Position", (hitObject.BaseObject as OsuHitObject)!.StackedPosition),
                new ObjectInspectorDifficultyValue("Strain Time", hitObject.StrainTime),
                new ObjectInspectorDifficultyValue("Aim Difficulty", AimEvaluator.EvaluateDifficultyOf(hitObject, true)),
                new ObjectInspectorDifficultyValue("Speed Difficulty", SpeedEvaluator.EvaluateDifficultyOf(hitObject)),
                new ObjectInspectorDifficultyValue("Rhythm Diff", RhythmEvaluator.EvaluateDifficultyOf(hitObject)),
                new ObjectInspectorDifficultyValue(hidden ? "FLHD Difficulty" : "Flashlight Diff", FlashlightEvaluator.EvaluateDifficultyOf(hitObject, hidden)),
            });

            if (hitObject.Angle is not null)
                flowContainer.Add(new ObjectInspectorDifficultyValue("Angle", MathUtils.RadiansToDegrees(hitObject.Angle.Value)));

            if (hitObject.BaseObject is Slider)
            {
                flowContainer.AddRange(new Drawable[]
                {
                    new Box
                    {
                        Name = "Separator",
                        Height = 1,
                        RelativeSizeAxes = Axes.X,
                        Alpha = 0.5f
                    },
                    new ObjectInspectorDifficultyValue("Travel Time", hitObject.TravelTime),
                    new ObjectInspectorDifficultyValue("Travel Distance", hitObject.TravelDistance),
                    new ObjectInspectorDifficultyValue("Min Jump Dist", hitObject.MinimumJumpDistance),
                    new ObjectInspectorDifficultyValue("Min Jump Time", hitObject.MinimumJumpTime)
                });
            }
        }

        private void drawTaikoValues(TaikoDifficultyHitObject hitObject)
        {
            flowContainer.AddRange(new[]
            {
                new ObjectInspectorDifficultyValue("Delta Time", hitObject.DeltaTime),
                new ObjectInspectorDifficultyValue("Rhythm Difficulty", hitObject.Rhythm.Difficulty),
                new ObjectInspectorDifficultyValue("Rhythm Ratio", hitObject.Rhythm.Ratio),
            });
        }

        private void drawCatchValues(CatchDifficultyHitObject hitObject)
        {
            flowContainer.AddRange(new[]
            {
                new ObjectInspectorDifficultyValue("Strain Time", hitObject.StrainTime),
                new ObjectInspectorDifficultyValue("Normalized Position", hitObject.NormalizedPosition)
            });
        }
    }
}
