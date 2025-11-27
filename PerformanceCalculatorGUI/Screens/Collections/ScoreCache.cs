// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Game.Database;
using osu.Game.Online.API.Requests.Responses;

namespace PerformanceCalculatorGUI.Screens.Collections
{
    public partial class ScoreCache : MemoryCachingComponent<long, SoloScoreInfo?>
    {
        [Resolved]
        private APIManager apiManager { get; set; } = null!;

        public Task<SoloScoreInfo?> GetScore(long id, CancellationToken token = default) => GetAsync(id, token);

        protected override async Task<SoloScoreInfo?> ComputeValueAsync(long lookup, CancellationToken token = default)
        {
            var score = await apiManager.GetJsonFromApi<SoloScoreInfo>($"scores/{lookup}").ConfigureAwait(false);
            await Task.Delay(200, token).ConfigureAwait(false);
            return score;
        }
    }
}
