// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays;
using osuTK;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class DebugValueList : Container {


        protected Dictionary<string, Dictionary<string, object>> InternalDict;
        private Box bgBox;
        private TextFlowContainer flowContainer;

        public DebugValueList() {
            InternalDict = new Dictionary<string, Dictionary<string, object>>();
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colors) {
            RelativeSizeAxes = Axes.Y;
            Width = 215;
            Children = new Drawable[]{
                bgBox = new Box
                {
                    Colour = colors.Background5,
                    Alpha = 0.95f,
                    RelativeSizeAxes = Axes.Both
                },
                new OsuScrollContainer() {
                    RelativeSizeAxes = Axes.X,
                    Height = 1000,
                    ScrollbarAnchor = Anchor.TopLeft,
                    Child = flowContainer = new TextFlowContainer()
                    {
                        Masking = false,
                        Margin = new MarginPadding { Left = 15 },
                        Size = new osuTK.Vector2(200,2000),
                        Y = 2000,
                        Origin = Anchor.BottomLeft
                    },
                }
            };
        }


        public void UpdateValues()
        {
            flowContainer.Text = "";
            foreach (KeyValuePair<string,Dictionary<string,object>> GroupPair in InternalDict)
            {
                // Big text
                string groupName = GroupPair.Key;
                Dictionary<string, object> groupDict = GroupPair.Value;
                flowContainer.AddText($"- {GroupPair.Key}\n", t => {
                    t.Scale = new osuTK.Vector2(1.8f);
                    t.Font = OsuFont.Torus.With(weight: "Bold");
                    t.Colour = Colour4.Pink;
                    t.Shadow = true;
                });

                foreach (KeyValuePair<string, object> ValuePair in groupDict) {
                    flowContainer.AddText($"   {ValuePair.Key} :\n", t => {
                        t.Scale = new osuTK.Vector2(1.3f);
                        t.Font = OsuFont.TorusAlternate.With(weight: "SemiBold");
                        t.Colour = Colour4.White;
                        t.Shadow = true;
                        t.Truncate = true;
                    });
                    flowContainer.AddText($"     -> {ValuePair.Value}\n\n", t => {
                        t.Scale = new osuTK.Vector2(1.3f);
                        t.Font = OsuFont.TorusAlternate.With(weight: "SemiBold");
                        t.Colour = Colour4.White;
                        t.Shadow = true;
                    });
                }
            }
        }

        public void AddGroup(string name, string[] overrides = null) {
            overrides ??= new string[0];
            foreach (string other in overrides) {
                InternalDict.Remove(other);
            }
            InternalDict[name] =  new Dictionary<string, object>();
        }

        public void SetValue(string group, string name, object value) {
            InternalDict.TryGetValue(group, out var exists);
            if (exists == null) {
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
