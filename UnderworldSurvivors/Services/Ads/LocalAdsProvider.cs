using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UnderworldSurvivors.Services.Ads
{
    public sealed class LocalAdsProvider : IAdsProvider
    {
        private const int FakeAdMs = 300;

        public bool IsRewardGranted { get; set; } = true;
        public string LastError => string.Empty;
        public bool IsRewardedAvailable => true;

        public async UniTask<RewardedResult> ShowRewardedAsync()
        {
            await UniTask.Delay(FakeAdMs, DelayType.Realtime);
            Debug.Log($"[Ads] rewarded {(IsRewardGranted ? "granted" : "skipped")}");
            return IsRewardGranted ? RewardedResult.Granted : RewardedResult.Skipped;
        }

        public async UniTask ShowInterstitialAsync()
        {
            await UniTask.Delay(FakeAdMs, DelayType.Realtime);
            Debug.Log("[Ads] interstitial shown");
        }

        public void SetBannerVisible(bool isVisible) => Debug.Log($"[Ads] banner {(isVisible ? "on" : "off")}");
    }
}
