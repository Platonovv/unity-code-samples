using Cysharp.Threading.Tasks;

namespace UnderworldSurvivors.Services.Ads
{
    public interface IAdsProvider
    {
        string LastError { get; }
        bool IsRewardedAvailable { get; }

        UniTask<RewardedResult> ShowRewardedAsync();
        UniTask ShowInterstitialAsync();
        void SetBannerVisible(bool isVisible);
    }
}
