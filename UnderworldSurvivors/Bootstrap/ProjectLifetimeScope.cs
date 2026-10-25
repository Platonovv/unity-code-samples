using UnderworldSurvivors.Bootstrap.States;
using UnderworldSurvivors.Services.Ads;
using UnderworldSurvivors.Services.Analytics;
using UnderworldSurvivors.Services.Assets;
using UnderworldSurvivors.Services.Audio;
using UnderworldSurvivors.Services.Community;
using UnderworldSurvivors.Services.Gear;
using UnderworldSurvivors.Services.Gift;
using UnderworldSurvivors.Services.Heroes;
using UnderworldSurvivors.Services.Input;
using UnderworldSurvivors.Services.Graphics;
using UnderworldSurvivors.Services.Localization;
using UnderworldSurvivors.Services.Locations;
using UnderworldSurvivors.Services.Platform;
using UnderworldSurvivors.Services.Preferences;
using UnderworldSurvivors.Services.Purchases;
using UnderworldSurvivors.Services.Run;
using UnderworldSurvivors.Services.Runes;
using UnderworldSurvivors.Services.Smith;
using UnderworldSurvivors.Services.Save;
using UnderworldSurvivors.Services.Settings;
using UnderworldSurvivors.Services.Shop;
using UnderworldSurvivors.Services.Tutorial;
using UnderworldSurvivors.UI.Ads;
using UnderworldSurvivors.UI.Cheats;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UnderworldSurvivors.Bootstrap
{
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        [Header("Settings")]
        [SerializeField] private GameSettings _settings;

        [Header("UI")]
        [SerializeField] private CheatPanelView _cheatPanel;
        [SerializeField] private AdNoticeView _adNotice;

        protected override void Configure(IContainerBuilder builder)
        {
            RegisterServices(builder);
            RegisterStates(builder);
            builder.RegisterEntryPoint<GameFlow>();
            builder.RegisterComponent(_cheatPanel);
            builder.RegisterBuildCallback(InitUi);
        }

        private void InitUi(IObjectResolver resolver)
        {
            _cheatPanel.Init(resolver.Resolve<LocationProgressService>(), resolver.Resolve<IRunLauncher>(), resolver.Resolve<LocalizationService>(), resolver.Resolve<HeroService>(), resolver.Resolve<ISaveService>(), resolver.Resolve<DeviceGraphicsService>());
            _adNotice.Init(resolver.Resolve<AdsService>(), resolver.Resolve<LocalizationService>());
        }

        private void RegisterServices(IContainerBuilder builder)
        {
            builder.RegisterInstance(_settings);
            builder.Register<AssetProvider>(Lifetime.Singleton);
            RegisterPlatform(builder);
            builder.Register<SaveService>(Lifetime.Singleton).As<ISaveService>();
            builder.Register<InputService>(Lifetime.Singleton);
            builder.Register<LocalizationService>(Lifetime.Singleton);
            builder.Register<PreferencesService>(Lifetime.Singleton);
            builder.Register<LocationProgressService>(Lifetime.Singleton);
            builder.Register<HeroService>(Lifetime.Singleton);
            builder.Register<GearService>(Lifetime.Singleton);
            builder.Register<GearGenerator>(Lifetime.Singleton);
            builder.Register<RuneService>(Lifetime.Singleton);
            builder.Register<SmithService>(Lifetime.Singleton);
            builder.Register<HeroStatsService>(Lifetime.Singleton);
            builder.Register<VibrationService>(Lifetime.Singleton);
            builder.Register<TutorialService>(Lifetime.Singleton);
            builder.Register<DailyGiftService>(Lifetime.Singleton);
            builder.Register<CommunityService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<AudioService>().AsSelf();
        }

        private void RegisterPlatform(IContainerBuilder builder)
        {
            builder.Register<PauseService>(Lifetime.Singleton);
            builder.Register<PrefsSaveStorage>(Lifetime.Singleton);
            builder.Register<AdsService>(Lifetime.Singleton);
            builder.Register<PurchaseService>(Lifetime.Singleton);
            builder.Register<ShopRewardService>(Lifetime.Singleton);
            builder.Register<ShopService>(Lifetime.Singleton);
            builder.Register<DeviceGraphicsService>(Lifetime.Singleton);
            builder.Register<AnalyticsService>(Lifetime.Singleton);
#if UNITY_WEBGL && !UNITY_EDITOR
            var platform = new WebPlatform();
            if (platform.IsVk)
                RegisterVk(builder);
            else if (platform.IsGamePush)
                RegisterGamePush(builder);
            else
                RegisterYandex(builder);
            builder.Register<YandexAnalyticsProvider>(Lifetime.Singleton).As<IAnalyticsProvider>();
#else
            builder.Register<LocalPlatformService>(Lifetime.Singleton).As<IPlatformService>();
            builder.Register<LocalAdsProvider>(Lifetime.Singleton).As<IAdsProvider>().AsSelf();
            builder.Register<LocalPurchaseProvider>(Lifetime.Singleton).As<IPurchaseProvider>().AsSelf();
            builder.Register<LocalAnalyticsProvider>(Lifetime.Singleton).As<IAnalyticsProvider>();
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private void RegisterYandex(IContainerBuilder builder)
        {
            builder.Register<YandexSdk>(Lifetime.Singleton);
            builder.Register<YandexPlatformService>(Lifetime.Singleton).As<IPlatformService>();
            builder.Register<YandexAdsProvider>(Lifetime.Singleton).As<IAdsProvider>();
            builder.Register<YandexPurchaseProvider>(Lifetime.Singleton).As<IPurchaseProvider>();
        }

        private void RegisterVk(IContainerBuilder builder)
        {
            builder.Register<VkSdk>(Lifetime.Singleton);
            builder.Register<VkPlatformService>(Lifetime.Singleton).As<IPlatformService>();
            builder.Register<VkAdsProvider>(Lifetime.Singleton).As<IAdsProvider>();
            builder.Register<VkPurchaseProvider>(Lifetime.Singleton).As<IPurchaseProvider>();
        }

        private void RegisterGamePush(IContainerBuilder builder)
        {
            builder.Register<GamePushSdk>(Lifetime.Singleton);
            builder.Register<GamePushPlatformService>(Lifetime.Singleton).As<IPlatformService>();
            builder.Register<GamePushAdsProvider>(Lifetime.Singleton).As<IAdsProvider>();
            builder.Register<GamePushPurchaseProvider>(Lifetime.Singleton).As<IPurchaseProvider>();
            builder.RegisterEntryPoint<GamePushSoundSync>();
        }
#endif

        private void RegisterStates(IContainerBuilder builder)
        {
            builder.Register<GameStateMachine>(Lifetime.Singleton);
            builder.Register<RunLauncher>(Lifetime.Singleton).As<IRunLauncher>();
            builder.Register<BootState>(Lifetime.Singleton).As<IGameState>();
            builder.Register<HubState>(Lifetime.Singleton).As<IGameState>();
            builder.Register<RunState>(Lifetime.Singleton).As<IGameState>();
        }
    }
}
