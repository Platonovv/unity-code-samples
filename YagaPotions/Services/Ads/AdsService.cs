using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YagaPotions.Gameplay.Settings;
using YagaPotions.Services.Analytics;
using YagaPotions.Services.Assets;
using YagaPotions.Services.Platform;
using YagaPotions.Services.Save;

namespace YagaPotions.Services.Ads
{
    public sealed class AdsService
    {
        private readonly IAdsProvider _provider;
        private readonly IPlatformService _platform;
        private readonly PauseService _pause;
        private readonly ISaveService _saveService;
        private readonly AnalyticsService _analytics;
        private readonly AdsSettings _settings;
        private float _lastAdTime;

        public AdsService(IAdsProvider provider, IPlatformService platform, PauseService pause, ISaveService saveService, AnalyticsService analytics, AssetProvider assetProvider)
        {
            _provider = provider;
            _platform = platform;
            _pause = pause;
            _saveService = saveService;
            _analytics = analytics;
            _settings = assetProvider.GetAdsSettings();
            _lastAdTime = Time.realtimeSinceStartup;
        }

        public event Action OnRewardedUnavailable;
        public event Action OnRewardedGranted;

        public bool IsShowing { get; private set; }
        public string LastError => _provider.LastError;
        public bool IsRewardedAvailable => _provider.IsRewardedAvailable;

        public async UniTask<bool> ShowRewardedAsync(RewardedPlacement placement)
        {
            RewardedResult result = await ShowAsync(_provider.ShowRewardedAsync, RewardedResult.Skipped);
            _analytics.Rewarded(placement, result);
            if (result == RewardedResult.Unavailable)
                OnRewardedUnavailable?.Invoke();
            else if (result == RewardedResult.Granted)
                OnRewardedGranted?.Invoke();

            return result == RewardedResult.Granted;
        }

        public async UniTask ShowInterstitialAsync(int completedLevel)
        {
            if (completedLevel < _settings.InterstitialFromLevel || IsInterstitialReady() == false)
                return;

            bool isShown = await ShowAsync(_provider.ShowInterstitialAsync, false);
            _analytics.Interstitial(isShown);
        }

        public void RefreshBanner() => _provider.SetBannerVisible(_platform.IsDesktop && !IsAdsRemoved);

        private bool IsAdsRemoved => _saveService.Data.IsAdsRemoved;

        private bool IsInterstitialReady() => !IsShowing && !IsAdsRemoved && Time.realtimeSinceStartup - _lastAdTime >= _settings.InterstitialCooldown;

        private async UniTask<T> ShowAsync<T>(Func<UniTask<T>> show, T busyResult)
        {
            if (IsShowing)
                return busyResult;

            Begin();
            try
            {
                return await show();
            }
            finally
            {
                End();
            }
        }

        private void Begin()
        {
            IsShowing = true;
            _platform.SetGameplayActive(false);
            _pause.Pause(PauseReason.Ad);
        }

        private void End()
        {
            IsShowing = false;
            _lastAdTime = Time.realtimeSinceStartup;
            _pause.Resume(PauseReason.Ad);
        }
    }
}
