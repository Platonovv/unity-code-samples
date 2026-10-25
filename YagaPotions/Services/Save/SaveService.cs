using Cysharp.Threading.Tasks;
using UnityEngine;
using YagaPotions.GameData;
using YagaPotions.Services.Platform;

namespace YagaPotions.Services.Save
{
    public sealed class SaveService : ISaveService
    {
        private readonly IPlatformService _platform;
        private bool _isLocked;

        public SaveService(IPlatformService platform)
        {
            _platform = platform;
        }

        public PlayerSave Data { get; private set; } = new PlayerSave();

        public async UniTask LoadAsync()
        {
            string json = await _platform.LoadDataAsync();
            Data = string.IsNullOrEmpty(json) ? new PlayerSave() : JsonUtility.FromJson<PlayerSave>(json);
            Migrate();
        }

        public UniTask SaveAsync()
        {
            if (!_isLocked)
                _platform.SaveData(JsonUtility.ToJson(Data));

            return UniTask.CompletedTask;
        }

        public void ResetForTesting()
        {
            Data = new PlayerSave();
            _platform.SaveData(JsonUtility.ToJson(Data));
            _isLocked = true;
        }

        private void Migrate()
        {
            Data.Version = PlayerSave.CurrentVersion;
        }
    }
}
