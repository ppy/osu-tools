using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OpenTabletDriver.Plugin.DependencyInjection;
using osu.Framework.Graphics;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Edit;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public interface IDebugListUpdater
    {

        public void UpdateDebugList(DebugValueList valueList, DifficultyHitObject curDiffHit)
        {
            valueList.AddGroup("None");
        }
    }
}
