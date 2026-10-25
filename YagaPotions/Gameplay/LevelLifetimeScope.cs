using UnityEngine;
using VContainer;
using VContainer.Unity;
using YagaPotions.Services.MediatorService;
using YagaPotions.UI.Hotkeys;
using YagaPotions.UI.Mediators;

namespace YagaPotions.Gameplay
{
    public sealed class LevelLifetimeScope : LifetimeScope
    {
        [Header("UI")]
        [SerializeField] private UiOverlayRoot _uiOverlayRoot;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_uiOverlayRoot);
            builder.Register<MediatorFactory>(Lifetime.Singleton);
            builder.RegisterEntryPoint<SettingsHotkey>();
            builder.RegisterBuildCallback(ShowMediators);
        }

        private void ShowMediators(IObjectResolver resolver)
        {
            var mediators = resolver.Resolve<MediatorFactory>();
            mediators.LoadAllMediators();
            mediators.Show<HomeMediator>();
        }
    }
}
