using Cysharp.Threading.Tasks;

namespace YagaPotions.Services.Ads
{
    public interface IAdsProvider
    {
        string LastError { get; }
        bool IsRewardedAvailable { get; }

        UniTask<RewardedResult> ShowRewardedAsync();
        UniTask<bool> ShowInterstitialAsync();
        void SetBannerVisible(bool isVisible);
    }
}
