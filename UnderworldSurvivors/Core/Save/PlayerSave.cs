using System;
using System.Collections.Generic;

namespace UnderworldSurvivors.Core.Save
{
    [Serializable]
    public sealed class PlayerSave
    {
        public const int CurrentVersion = 10;
        public const int GearItemsVersion = 6;
        public const int FirstRunVersion = 7;
        public const int TutorialVersion = 10;

        public int Version = CurrentVersion;
        public long SavedAt;
        public int TotalKills;
        public bool IsFirstRunDone;
        public TutorialStep Tutorials;
        public float MusicVolume = 1f;
        public float SoundVolume = 1f;
        public bool IsVibrationOn = true;
        public bool IsDamageNumbersOn = true;
        public bool IsCheatPanelOn;
        public List<LocationProgressSave> Locations = new();
        public List<string> UnlockedHeroes = new();
        public List<string> SeenHeroes = new();
        public string SelectedHero = string.Empty;
        public int Souls;
        public int NextGearId = 1;
        public int GearPity;
        public List<GearItemSave> Gear = new();
        public List<RuneSave> Runes = new();
        public string FirstDay = string.Empty;
        public string GiftDay = string.Empty;
        public int GiftsTaken;
        public bool IsAdsRemoved;
        public bool IsGroupRewardTaken;
        public List<string> UsedPromoCodes = new();
        public int RunsFinished;
        public bool IsReviewAsked;
        public bool IsShortcutRewarded;
        public List<string> BoughtOffers = new();
        public List<string> UnconsumedPurchases = new();
        public string ShopVideoDay = string.Empty;
        public int ShopVideosTaken;
    }
}
