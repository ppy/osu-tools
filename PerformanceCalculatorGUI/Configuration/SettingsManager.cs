// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Reflection;
using osu.Framework.Configuration;
using osu.Framework.Platform;

namespace PerformanceCalculatorGUI.Configuration
{
    public enum Settings
    {
        ClientId,
        ClientSecret,
        DefaultPath,
        CachePath
    }

    public class SettingsManager : IniConfigManager<Settings>
    {
        protected override string Filename => "perfcalc.ini";

        public SettingsManager(Storage storage)
            : base(storage)
        {
        }

        protected override void InitialiseDefaults()
        {
            SetDefault(Settings.ClientId, string.Empty);
            SetDefault(Settings.ClientSecret, string.Empty);
            SetDefault(Settings.DefaultPath, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            SetDefault(Settings.CachePath, Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "cache"));
        }
    }
}
