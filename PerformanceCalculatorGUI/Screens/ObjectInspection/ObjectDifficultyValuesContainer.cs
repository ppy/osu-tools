// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osuTK;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class ObjectDifficultyValuesContainer : Container
    {
        public Bindable<Dictionary<string, Dictionary<string, object>>> InfoDictionary;

        private Box bgBox;
        private TextFlowContainer flowContainer;
        private Bindable<DifficultyHitObject> focusedDiffHit = new();

        public ObjectDifficultyValuesContainer(Bindable<DifficultyHitObject> diffHitBind)
        {
            focusedDiffHit.BindTo(diffHitBind);
            focusedDiffHit.ValueChanged += (ValueChangedEvent<DifficultyHitObject> _) => UpdateValues();

            InfoDictionary = new Bindable<Dictionary<string, Dictionary<string, object>>>
            {
                Value = new Dictionary<string, Dictionary<string, object>>()
            };
            RelativeSizeAxes = Axes.Y;
            Width = 215;
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colors)
        {
            Children = new Drawable[]
            {
                bgBox = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colors.Background5,
                    Alpha = 0.95f,
                },
                new OsuScrollContainer()
                {
                    RelativeSizeAxes = Axes.Both,
                    ScrollbarAnchor = Anchor.TopLeft,
                    Child = flowContainer = new TextFlowContainer()
                    {
                        AutoSizeAxes = Axes.Both,
                        Masking = false,
                        Margin = new MarginPadding { Left = 15 },
                        Origin = Anchor.TopLeft,
                    },
                },
            };
        }

        public void UpdateValues()
        {
            flowContainer.Text = "";

            foreach (KeyValuePair<string, Dictionary<string, object>> GroupPair in InfoDictionary.Value)
            {
                // Big text
                string groupName = GroupPair.Key;
                Dictionary<string, object> groupDict = GroupPair.Value;
                flowContainer.AddText($"- {GroupPair.Key}\n", t =>
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

        public void AddGroup(string name, string[] overrides = null)
        {
            overrides ??= Array.Empty<string>();

            foreach (string other in overrides)
            {
                InfoDictionary.Value.Remove(other);
            }

            InfoDictionary.Value[name] = new Dictionary<string, object>();
        }

        public bool GroupExists(string name)
        {
            return InfoDictionary.Value.ContainsKey(name);
        }

        public void SetValue(string group, string name, object value)
        {
            InfoDictionary.Value.TryGetValue(group, out var exists);

            if (exists == null)
            {
                AddGroup(group);
            }

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

            InfoDictionary.Value[group][name] = value;
        }

        public object GetValue(string group, string name)
        {
            return InfoDictionary.Value[group][name];
        }
    }
}
