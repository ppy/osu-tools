// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Screens.Edit.Components.Timelines.Summary.Parts;
using osu.Game.Screens.Edit.Compose.Components;
using osu.Game.Skinning;
using osuTK;
using osuTK.Graphics;
using static osu.Game.Screens.Edit.Compose.Components.Timeline.TimelineHitObjectBlueprint;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    internal partial class TimelineBlueprintContainer : EditorBlueprintContainer
    {
        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => false;

        public TimelineBlueprintContainer()
            : base(null)
        {
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            Height = 1f;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            foreach (var obj in Beatmap.HitObjects)
                AddBlueprintFor(obj);
        }

        protected override SelectionBlueprintContainer CreateSelectionBlueprintContainer() => new TimelineSelectionBlueprintContainer { RelativeSizeAxes = Axes.Both };

        protected override bool OnDragStart(DragStartEvent e) => false;

        protected override SelectionHandler<HitObject> CreateSelectionHandler() => new EmptySelectionHandler();

        protected override SelectionBlueprint<HitObject> CreateBlueprintFor(HitObject item) => new TimelineHitObjectBlueprint(item);

        protected sealed override DragBox CreateDragBox() => new EmptyDragBox();

        protected override void SelectAll()
        {
        }

        protected override void OnBlueprintSelected(SelectionBlueprint<HitObject> blueprint)
        {
        }

        protected override void OnBlueprintDeselected(SelectionBlueprint<HitObject> blueprint)
        {
        }

        protected partial class TimelineSelectionBlueprintContainer : BlueprintContainer<HitObject>.SelectionBlueprintContainer
        {
            protected override Container<SelectionBlueprint<HitObject>> Content { get; }

            public TimelineSelectionBlueprintContainer()
            {
                AddInternal(new TimelinePart<SelectionBlueprint<HitObject>>(Content = new HitObjectOrderedSelectionContainer { RelativeSizeAxes = Axes.Both }) { RelativeSizeAxes = Axes.Both });
            }
        }

        private partial class EmptySelectionHandler : SelectionHandler<HitObject>
        {
            protected override void DeleteItems(IEnumerable<HitObject> items)
            {
            }
        }

        protected partial class EmptyDragBox : DragBox
        {
            public EmptyDragBox()
            {
                RelativeSizeAxes = Axes.Both;
                Alpha = 0;
            }

            protected override Drawable CreateBox() => Empty();

            public override void HandleDrag(MouseButtonEvent e)
            {
            }
        }

        public partial class TimelineHitObjectBlueprint : SelectionBlueprint<HitObject>
        {
            private const float circle_size = 32;

            private readonly ExtendableCircle circle;

            private readonly Container colouredComponents;
            private readonly OsuSpriteText comboIndexText;

            [Resolved]
            private ISkinSource skin { get; set; } = null!;

            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            public TimelineHitObjectBlueprint(HitObject item)
                : base(item)
            {
                Anchor = Anchor.CentreLeft;
                Origin = Anchor.CentreLeft;

                X = (float)item.StartTimeBindable.Value;

                RelativePositionAxes = Axes.X;

                RelativeSizeAxes = Axes.X;
                Height = circle_size;
                Width = 1;

                AddRangeInternal(new Drawable[]
                {
                    circle = new ExtendableCircle
                    {
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Alpha = 0.75f
                    },
                    colouredComponents = new Container
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        RelativeSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            comboIndexText = new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.Centre,
                                Y = -1,
                                Font = OsuFont.Default.With(size: circle_size * 0.5f, weight: FontWeight.Regular),
                            },
                        }
                    },
                });
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                switch (Item)
                {
                    case IHasComboInformation comboInfo:
                        comboIndexText.Text = (comboInfo.IndexInCurrentComboBindable.Value + 1).ToString();
                        break;
                }

                updateColour();
            }

            protected override void OnSelected()
            {
            }

            protected override void OnDeselected()
            {
            }

            private void updateColour()
            {
                Color4 colour;

                switch (Item)
                {
                    case IHasDisplayColour displayColour:
                        colour = displayColour.DisplayColour.Value;
                        break;

                    case IHasComboInformation combo:
                        colour = combo.GetComboColour(skin);
                        break;

                    default:
                        colour = colourProvider.Highlight1;
                        break;
                }

                if (Item is IHasDuration duration && duration.Duration > 0)
                    circle.Colour = ColourInfo.GradientHorizontal(colour, colour.Lighten(0.4f));
                else
                    circle.Colour = colour;

                var averageColour = Interpolation.ValueAt(0.5, circle.Colour.TopLeft, circle.Colour.TopRight, 0, 1);
                colouredComponents.Colour = OsuColour.ForegroundTextColourFor(averageColour);
            }

            protected override bool ShouldBeConsideredForInput(Drawable child) => false;

            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => false;
        }
    }
}
