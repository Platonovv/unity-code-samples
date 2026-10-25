using System;
using Cysharp.Threading.Tasks;
using UnderworldSurvivors.Core.Save;
using UnderworldSurvivors.Services.Platform;
using UnityEngine;

namespace UnderworldSurvivors.Services.Save
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
                Write();

            return UniTask.CompletedTask;
        }

        public void ResetForTesting()
        {
            Data = new PlayerSave();
            Write();
            _isLocked = true;
        }

        private void Write()
        {
            Data.SavedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _platform.SaveData(JsonUtility.ToJson(Data));
        }

        private void Migrate()
        {
            if (Data.Version < PlayerSave.GearItemsVersion)
                Data.Gear.Clear();

            if (Data.Version < PlayerSave.FirstRunVersion)
                Data.IsFirstRunDone = true;

            if (Data.Version < PlayerSave.TutorialVersion && Data.IsFirstRunDone)
                Data.Tutorials = TutorialStep.Run;

            Data.Version = PlayerSave.CurrentVersion;
        }
    }
}
