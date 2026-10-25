using Cysharp.Threading.Tasks;
using UnityEngine;
using UnderworldSurvivors.Services.Localization;

namespace UnderworldSurvivors.Services.Platform
{
    public sealed class LocalPlatformService : IPlatformService
    {
        private readonly PrefsSaveStorage _storage;

        public LocalPlatformService(PrefsSaveStorage storage)
        {
            _storage = storage;
        }

        public bool IsDesktop => !Application.isMobilePlatform;
        public bool CanJoinGroup => true;
        public bool CanOpenLeaderboard => true;
        public bool CanAddShortcut => true;
        public Language Language => Language.Russian;

        public UniTask InitAsync() => UniTask.CompletedTask;

        public void ReportLoaded()
        {
        }

        public void SetGameplayActive(bool isActive)
        {
        }

        public UniTask<string> LoadDataAsync() => UniTask.FromResult(_storage.Load());

        public void SaveData(string json) => _storage.Save(json);

        public UniTask<bool> JoinGroupAsync(int groupId) => UniTask.FromResult(true);

        public void SetScore(int score) => Debug.Log($"[Platform] score {score}");

        public void OpenLeaderboard() => Debug.Log("[Platform] leaderboard");

        public UniTask<bool> RequestReviewAsync() => UniTask.FromResult(true);

        public UniTask<bool> AddShortcutAsync() => UniTask.FromResult(true);
    }
}
