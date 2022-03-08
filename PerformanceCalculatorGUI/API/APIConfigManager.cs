// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Configuration;
using osu.Framework.Platform;

namespace PerformanceCalculatorGUI.API
{
    public enum APISettings
    {
        ClientId,
        ClientSecret
    }

    public class APIConfigManager : IniConfigManager<APISettings>
    {
        protected override string Filename => "apiconfig.ini";

        public APIConfigManager(Storage storage)
            : base(storage)
        {
        }

        protected override void InitialiseDefaults()
        {
            SetDefault(APISettings.ClientId, string.Empty);
            SetDefault(APISettings.ClientSecret, string.Empty);
        }
    }
}
