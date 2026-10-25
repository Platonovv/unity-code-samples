using Cysharp.Threading.Tasks;
using UnderworldSurvivors.Services.Localization;

namespace UnderworldSurvivors.Services.Platform
{
    public interface IPlatformService
    {
        bool IsDesktop { get; }
        bool CanJoinGroup { get; }
        bool CanOpenLeaderboard { get; }
        bool CanAddShortcut { get; }
        Language Language { get; }

        UniTask InitAsync();
        void ReportLoaded();
        void SetGameplayActive(bool isActive);
        UniTask<string> LoadDataAsync();
        void SaveData(string json);
        UniTask<bool> JoinGroupAsync(int groupId);
        void SetScore(int score);
        void OpenLeaderboard();
        UniTask<bool> RequestReviewAsync();
        UniTask<bool> AddShortcutAsync();
    }
}
