// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Utils;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Taiko;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osuTK;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class ObjectDifficultyValuesContainer : Container
    {
        [Resolved]
        private Bindable<RulesetInfo> ruleset { get; set; }

        private readonly Dictionary<string, Dictionary<string, object>> infoDictionary;

        private TextFlowContainer flowContainer;

        public Bindable<DifficultyHitObject> CurrentDifficultyHitObject { get; } = new();

        public ObjectDifficultyValuesContainer()
        {
            CurrentDifficultyHitObject.ValueChanged += h => updateValues(h.NewValue);

            infoDictionary = new Dictionary<string, Dictionary<string, object>>();
            RelativeSizeAxes = Axes.Y;
            Width = 215;
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colors)
        {
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colors.Background5,
                    Alpha = 0.95f,
                },
                new OsuScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    ScrollbarAnchor = Anchor.TopLeft,
                    Child = flowContainer = new TextFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Masking = false,
                        Margin = new MarginPadding { Left = 15 },
                        Origin = Anchor.TopLeft,
                    },
                },
            };
        }

        private void updateValues(DifficultyHitObject hitObject)
        {
            if (hitObject == null)
            {
                flowContainer.Text = "";
                return;
            }

            switch (ruleset.Value.ShortName)
            {
                case OsuRuleset.SHORT_NAME:
                {
                    drawOsuValues(hitObject as OsuDifficultyHitObject);
                    break;
                }

                case TaikoRuleset.SHORT_NAME:
                {
                    drawTaikoValues(hitObject as TaikoDifficultyHitObject);
                    break;
                }

                case CatchRuleset.SHORT_NAME:
                {
                    drawCatchValues(hitObject as CatchDifficultyHitObject);
                    break;
                }
            }
        }

        private void drawOsuValues(OsuDifficultyHitObject hitObject)
        {
            var groupName = hitObject.BaseObject.GetType().Name;
            addGroup(groupName, new[] { "Slider", "HitCircle", "Spinner" });
            infoDictionary[groupName] = new Dictionary<string, object>
            {
                { "Position", (hitObject.BaseObject as OsuHitObject)!.StackedPosition },
                { "Strain Time", hitObject.StrainTime },
                { "Aim Difficulty", AimEvaluator.EvaluateDifficultyOf(hitObject, true) },
                { "Speed Difficulty", SpeedEvaluator.EvaluateDifficultyOf(hitObject) },
                { "Rhythm Diff", SpeedEvaluator.EvaluateDifficultyOf(hitObject) },
                { "Flashlight Diff", SpeedEvaluator.EvaluateDifficultyOf(hitObject) },
            };

            if (hitObject.Angle is not null)
                infoDictionary[groupName].Add("Angle", MathUtils.RadiansToDegrees(hitObject.Angle.Value));

            if (hitObject.BaseObject is Slider)
            {
                infoDictionary[groupName].Add("FL Travel Time", FlashlightEvaluator.EvaluateDifficultyOf(hitObject, false));
                infoDictionary[groupName].Add("Travel Time", hitObject.TravelTime);
                infoDictionary[groupName].Add("Travel Distance", hitObject.TravelDistance);
                infoDictionary[groupName].Add("Min Jump Dist", hitObject.MinimumJumpDistance);
                infoDictionary[groupName].Add("Min Jump Time", hitObject.MinimumJumpTime);
            }

            redrawValues();
        }

        private void drawTaikoValues(TaikoDifficultyHitObject hitObject)
        {
            var groupName = hitObject.BaseObject.GetType().Name;
            addGroup(groupName, new[] { "Hit", "Swell", "DrumRoll" });
            infoDictionary[groupName] = new Dictionary<string, object>
            {
                { "Delta Time", hitObject.DeltaTime },
                { "Rhythm Difficulty", hitObject.Rhythm.Difficulty },
                { "Rhythm Ratio", hitObject.Rhythm.Ratio }
            };

            redrawValues();
        }

        private void drawCatchValues(CatchDifficultyHitObject hitObject)
        {
            var groupName = hitObject.BaseObject.GetType().Name;
            addGroup(groupName, new[] { "Fruit", "Droplet" });
            infoDictionary[groupName] = new Dictionary<string, object>
            {
                { "Strain Time", hitObject.StrainTime },
                { "Normalized Position", hitObject.NormalizedPosition },
            };

            redrawValues();
        }

        private void redrawValues()
        {
            flowContainer.Text = "";

            foreach (KeyValuePair<string, Dictionary<string, object>> groupPair in infoDictionary)
            {
                // Big text
                Dictionary<string, object> groupDict = groupPair.Value;
                flowContainer.AddText($"- {groupPair.Key}\n", t =>
                {
                    t.Font = OsuFont.Torus.With(size: 28, weight: "Bold");
                    t.Colour = Colour4.Pink;
                    t.Shadow = true;
                });

                foreach (KeyValuePair<string, object> ValuePair in groupDict)
                {
                    flowContainer.AddText($"   {ValuePair.Key}:\n", t =>
                    {
                        t.Font = OsuFont.TorusAlternate.With(size: 21, weight: "SemiBold");
                        t.Colour = Colour4.White;
                        t.Shadow = true;
                        t.Truncate = true;
                    });

                    // value formatting
                    object value = ValuePair.Value;

                    if (value is double val)
                    {
                        value = Math.Truncate(val * 1000) / 1000;
                    }

                    if (value is float val2)
                    {
                        value = Math.Truncate(val2 * 1000) / 1000;
                    }

                    if (value is Vector2 val3)
                    {
                        value = new Vector2((float)(Math.Truncate(val3.X * 100) / 100), (float)Math.Truncate(val3.Y * 100) / 100);
                    }

                    flowContainer.AddText($"     -> {value}\n\n", t =>
                    {
                        t.Font = OsuFont.TorusAlternate.With(size: 21, weight: "SemiBold");
                        t.Colour = Colour4.White;
                        t.Shadow = true;
                    });
                }
            }
        }

        private void addGroup(string name, string[] overrides = null)
        {
            overrides ??= Array.Empty<string>();

            foreach (string other in overrides)
            {
                infoDictionary.Remove(other);
            }

            infoDictionary[name] = new Dictionary<string, object>();
        }
    }
}
