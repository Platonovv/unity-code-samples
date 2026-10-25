using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnderworldSurvivors.Core.Reactive;
using UnderworldSurvivors.Services.Ads;
using UnderworldSurvivors.Services.Analytics;
using UnderworldSurvivors.Services.Assets;
using UnderworldSurvivors.Services.Platform;
using UnderworldSurvivors.Services.Save;
using UnderworldSurvivors.Services.Settings;
using UnderworldSurvivors.Services.Shop;
using UnityEngine;
using UnityEngine.Networking;

namespace UnderworldSurvivors.Services.Purchases
{
    public sealed class PurchaseService : IReactiveObject
    {
        private const float IconPivot = 0.5f;

        private readonly IPurchaseProvider _provider;
        private readonly ISaveService _saveService;
        private readonly AdsService _ads;
        private readonly PauseService _pause;
        private readonly AnalyticsService _analytics;
        private readonly ShopRewardService _rewards;
        private readonly PurchaseSettings _settings;
        private IReadOnlyList<ProductInfo> _products = Array.Empty<ProductInfo>();

        public event Action OnChanged;

        public PurchaseService(IPurchaseProvider provider, ISaveService saveService, AdsService ads, PauseService pause, AnalyticsService analytics,
            ShopRewardService rewards, AssetProvider assetProvider)
        {
            _provider = provider;
            _saveService = saveService;
            _ads = ads;
            _pause = pause;
            _analytics = analytics;
            _rewards = rewards;
            _settings = assetProvider.GetPurchaseSettings();
        }

        public ProductInfo NoAds => Find(_settings.NoAdsId);
        public Sprite CurrencyIcon { get; private set; }
        public bool IsAdsRemoved => _saveService.Data.IsAdsRemoved;
        public bool IsBuying { get; private set; }

        public ProductInfo Find(string productId)
        {
            foreach (ProductInfo product in _products)
                if (product.Id == productId)
                    return product;

            return null;
        }

        public async UniTask RestoreAsync()
        {
            try
            {
                IReadOnlyList<string> purchased = await _provider.GetPurchasedAsync();
                foreach (string productId in purchased)
                    await GrantAsync(productId, false);
            }
            finally
            {
                await LoadCatalogAsync();
            }
        }

        public UniTask<bool> BuyNoAdsAsync() => IsAdsRemoved ? UniTask.FromResult(false) : BuyAsync(_settings.NoAdsId);

        public async UniTask<bool> BuyAsync(string productId)
        {
            if (IsBuying)
                return false;

            IsBuying = true;
            try
            {
                bool isBought = await PurchaseAsync(productId);
                _analytics.Purchase(productId, isBought);
                if (isBought)
                    await GrantAsync(productId, true);
                return isBought;
            }
            finally
            {
                IsBuying = false;
            }
        }

        private async UniTask LoadCatalogAsync()
        {
            _products = await _provider.GetCatalogAsync();
            if (_products.Count > 0)
                await LoadCurrencyIconAsync(_products[0].CurrencyIconUrl);
            OnChanged?.Invoke();
        }

        private async UniTask LoadCurrencyIconAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;

            using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            try
            {
                await request.SendWebRequest();
            }
            catch (UnityWebRequestException exception)
            {
                Debug.LogWarning($"[Purchases] currency icon failed: {exception.Message}");
                return;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            CurrencyIcon = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.one * IconPivot);
        }

        private async UniTask<bool> PurchaseAsync(string productId)
        {
            _pause.Pause(PauseReason.Purchase);
            try
            {
                UniTask<bool> purchase = _provider.PurchaseAsync(productId).Preserve();
                var (isAnswered, isBought) = await UniTask.WhenAny(purchase, WaitAnswerTimeout());
                if (!isAnswered)
                    GrantLateAsync(purchase, productId).Forget();
                return isAnswered && isBought;
            }
            finally
            {
                _pause.Resume(PauseReason.Purchase);
            }
        }

        private UniTask WaitAnswerTimeout() => UniTask.Delay(TimeSpan.FromSeconds(_settings.AnswerTimeoutSeconds), DelayType.Realtime);

        private async UniTaskVoid GrantLateAsync(UniTask<bool> purchase, string productId)
        {
            if (await purchase)
                await GrantAsync(productId, true);
        }

        private async UniTask GrantAsync(string productId, bool isFresh)
        {
            if (productId == _settings.NoAdsId)
                await RemoveAdsAsync();
            else if (_rewards.IsOffer(productId))
                await GrantOfferAsync(productId, isFresh);
        }

        private async UniTask RemoveAdsAsync()
        {
            if (IsAdsRemoved)
                return;

            _saveService.Data.IsAdsRemoved = true;
            await _saveService.SaveAsync();
            _ads.RefreshBanner();
            OnChanged?.Invoke();
        }

        private async UniTask GrantOfferAsync(string productId, bool isFresh)
        {
            List<string> unconsumed = _saveService.Data.UnconsumedPurchases;
            if (isFresh || !unconsumed.Contains(productId))
            {
                _rewards.Grant(productId);
                if (!unconsumed.Contains(productId))
                    unconsumed.Add(productId);
                await _saveService.SaveAsync();
            }

            if (await _provider.ConsumeAsync(productId))
            {
                unconsumed.Remove(productId);
                await _saveService.SaveAsync();
            }

            OnChanged?.Invoke();
        }
    }
}
