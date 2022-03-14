// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Utils;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Pooling;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Objects;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    internal class OsuObjectInspectorDrawable : PoolableDrawableWithLifetime<OsuObjectInspectorLifetimeEntry>
    {
        protected override void OnApply(OsuObjectInspectorLifetimeEntry entry)
        {
            base.OnApply(entry);

            entry.Invalidated += onEntryInvalidated;
            refresh();
        }

        protected override void OnFree(OsuObjectInspectorLifetimeEntry entry)
        {
            base.OnFree(entry);

            entry.Invalidated -= onEntryInvalidated;
            ClearInternal(false);
        }

        private void onEntryInvalidated() => Scheduler.AddOnce(refresh);

        private void refresh()
        {
            ClearInternal(false);

            var entry = Entry;
            if (entry == null) return;

            var hitObject = entry.HitObject;
            double startTime = hitObject.StartTime - hitObject.TimeFadeIn;
            double visibleTime = hitObject.GetEndTime() - startTime;

            OsuTextFlowContainer textFlow;

            var panel = new Container
            {
                Position = hitObject.StackedPosition,
                Origin = Anchor.Centre,
                Anchor = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 5f,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0.9f,
                        Colour = OsuColour.Gray(0.1f)
                    },
                    textFlow = new OsuTextFlowContainer
                    {
                        Padding = new MarginPadding(5f),
                        AutoSizeAxes = Axes.Both,
                    }
                }
            };

            textFlow.AddParagraph($"Position: {entry.HitObject.StackedPosition}", text => text.Font = OsuFont.GetFont(size: 8));

            if (entry.DifficultyHitObject is not null)
            {
                textFlow.AddParagraph($"Strain Time: {entry.DifficultyHitObject.StrainTime:N3}", text => text.Font = OsuFont.GetFont(size: 10));

                if (entry.DifficultyHitObject.Angle is not null)
                    textFlow.AddParagraph($"Angle: {MathUtils.RadiansToDegrees(entry.DifficultyHitObject.Angle.Value):N3}", text => text.Font = OsuFont.GetFont(size: 10));

                if (entry.HitObject is Slider)
                {
                    textFlow.AddParagraph($"Travel Time: {entry.DifficultyHitObject.TravelTime:N3}", text => text.Font = OsuFont.GetFont(size: 10));
                    textFlow.AddParagraph($"Travel Distance: {entry.DifficultyHitObject.TravelDistance:N3}", text => text.Font = OsuFont.GetFont(size: 10));
                    textFlow.AddParagraph($"Minimum Jump Distance: {entry.DifficultyHitObject.MinimumJumpDistance:N3}", text => text.Font = OsuFont.GetFont(size: 10));
                    textFlow.AddParagraph($"Minimum Jump Time: {entry.DifficultyHitObject.MinimumJumpTime:N3}", text => text.Font = OsuFont.GetFont(size: 10));
                }
            }

            AddInternal(panel);

            using (panel.BeginAbsoluteSequence(startTime))
            {
                panel.FadeIn(hitObject.TimeFadeIn);

                if (entry.HitObject is Slider)
                {
                    panel.MoveTo(hitObject.StackedEndPosition, visibleTime);
                }

                panel.Delay(visibleTime).FadeOut(DrawableOsuEditorRuleset.EDITOR_HIT_OBJECT_FADE_OUT_EXTENSION).Expire();
            }

            entry.LifetimeEnd = panel.LifetimeEnd;
        }
    }
}
