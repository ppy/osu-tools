// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays;
using osuTK;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class DebugValueList : Container
    {


        protected Dictionary<string, Dictionary<string, object>> InternalDict;
        private Box bgBox;
        private TextFlowContainer flowContainer;
        private Container switchContainer;

        public DebugValueList()
        {
            InternalDict = new Dictionary<string, Dictionary<string, object>>();
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colors)
        {
            RelativeSizeAxes = Axes.Both;
            Width = 215;
            Children = new Drawable[]{
                bgBox = new Box
                {
                    Colour = colors.Background5,
                    Alpha = 0.95f,
                    RelativeSizeAxes = Axes.Y,
                    Width = 215
                },
                new OsuScrollContainer() {
                    Width = 215,
                    Height = 670,
                    ScrollbarAnchor = Anchor.TopLeft,
                    Child = flowContainer = new TextFlowContainer()
                    {
                        Masking = false,
                        Margin = new MarginPadding { Left = 15 },
                        Size = new Vector2(200,3500),
                        Y = 3500,
                        Origin = Anchor.BottomLeft
                    },
                },
                switchContainer = new Container {
                    Size = new Vector2(215,3500),
                    X  = -85 + -1446 * 214,
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Child = new Box
                    {
                        Colour = colors.Background5,
                        Alpha = 0.95f,
                        RelativeSizeAxes = Axes.Both
                    },
                }
            };
        }


        public void UpdateValues()
        {
            flowContainer.Text = "";
            foreach (KeyValuePair<string, Dictionary<string, object>> GroupPair in InternalDict)
            {
                // Big text
                string groupName = GroupPair.Key;
                Dictionary<string, object> groupDict = GroupPair.Value;
                flowContainer.AddText($"- {GroupPair.Key}\n", t =>
                {
                    t.Scale = new Vector2(1.8f);
                    t.Font = OsuFont.Torus.With(weight: "Bold");
                    t.Colour = Colour4.Pink;
                    t.Shadow = true;
                });

                foreach (KeyValuePair<string, object> ValuePair in groupDict)
                {
                    flowContainer.AddText($"   {ValuePair.Key}:\n", t =>
                    {
                        t.Scale = new Vector2(1.3f);
                        t.Font = OsuFont.TorusAlternate.With(weight: "SemiBold");
                        t.Colour = Colour4.White;
                        t.Shadow = true;
                        t.Truncate = true;
                    });
                    flowContainer.AddText($"     -> {ValuePair.Value}\n\n", t =>
                    {
                        t.Scale = new Vector2(1.3f);
                        t.Font = OsuFont.TorusAlternate.With(weight: "SemiBold");
                        t.Colour = Colour4.White;
                        t.Shadow = true;
                    });
                }
            }
        }

        public void UpdateToggles()
        {
            switchContainer.Clear();
            switchContainer.Add(new Box
            {
                Colour = Colour4.Pink,
                Alpha = 0.95f,
                RelativeSizeAxes = Axes.Both
            });

            for (int i = 1; i < InternalDict.Keys.Count; i++)
            {
                string group = InternalDict.Keys.ElementAt(i);
                switchContainer.Add(new SpriteText
                {
                    Name = group,
                    Colour = Colour4.Red,
                    Size = new Vector2(200, 50),
                    Scale = new Vector2(1.8f),
                    Font = OsuFont.Torus.With(weight: "Bold"),
                    Shadow = true,
                });
            }

        }
        public void AddGroup(string name, string[] overrides = null)
        {
            overrides ??= Array.Empty<string>();
            foreach (string other in overrides)
            {
                InternalDict.Remove(other);
            }
            InternalDict[name] = new Dictionary<string, object>();
            UpdateToggles();
        }

        public bool GroupExists(string name)
        {
            return InternalDict.ContainsKey(name);
        }

        public void SetValue(string group, string name, object value)
        {
            InternalDict.TryGetValue(group, out var exists);
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

            InternalDict[group][name] = value;
        }

        public object GetValue(string group, string name)
        {
            return InternalDict[group][name];
        }

    }
}
