using System;
using osu.Framework;
using osu.Framework.Platform;
using osu.Game.Online;

namespace PerformanceCalculatorGUI
{
    public static class Program
    {
        public static readonly EndpointConfiguration ENDPOINT_CONFIGURATION = new ProductionEndpointConfiguration();

        [STAThread]
        public static void Main(string[] args)
        {
            using DesktopGameHost host = Host.GetSuitableHost("PerformanceCalculatorGUI", false, true);
            using var game = new PerformanceCalculatorGame();

            host.Run(game);
        }
    }
}
