using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTabletDriver.Plugin.DependencyInjection;
using osu.Game.Rulesets.Difficulty.Preprocessing;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public interface IDebugListUpdater
    {
        public void UpdateDebugList(DebugValueList valueList, DifficultyHitObject curDiffHit) {
            valueList.AddGroup("Test");
        }
    }
}
