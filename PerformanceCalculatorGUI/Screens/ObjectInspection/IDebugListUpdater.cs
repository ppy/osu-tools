using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OpenTabletDriver.Plugin.DependencyInjection;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public interface IDebugListUpdater
    {
        protected void AddInitalGroups(DebugValueList valueList);

        public void UpdateDebugList(DebugValueList valueList, DifficultyHitObject curDiffHit)
        {
            valueList.AddGroup("None");
        }
    }
}
