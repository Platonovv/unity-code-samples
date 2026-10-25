using System.Collections.Generic;
using UnderworldSurvivors.Core;
using UnderworldSurvivors.Core.Save;
using UnderworldSurvivors.Services.Assets;
using UnderworldSurvivors.Services.Save;
using UnderworldSurvivors.Services.Settings;
using UnityEngine;

namespace UnderworldSurvivors.Services.Gear
{
    public sealed class GearGenerator
    {
        private readonly List<GearStat> _extraPool = new();
        private readonly ISaveService _saveService;
        private readonly GearSettings _settings;
        private readonly IRandom _random;

        public GearGenerator(ISaveService saveService, AssetProvider assetProvider, IRandom random)
        {
            _saveService = saveService;
            _random = random;
            _settings = assetProvider.GetGearSettings();
        }

        public GearItemSave Create(GearSource source, int level, bool isLegendary)
        {
            Rarity rarity = isLegendary ? Rarity.Legendary : RollRarity(source);
            UpdatePity(rarity);
            return Create(level, rarity);
        }

        public GearItemSave Create(int level, Rarity rarity)
        {
            GearItemSettings kind = _settings.Items[_random.Range(0, _settings.Items.Count)];
            var item = new GearItemSave { Item = kind.name, Slot = (int)kind.Slot, Rarity = (int)rarity, Level = level, IsNew = true };
            float power = _settings.Get(rarity).Power * _settings.Growth(level);
            AddMainStats(item, kind.Slot, power);
            CollectExtraPool(kind);
            AddExtraStat(item, kind.SignatureStat, power);
            AddExtraStats(item, _settings.Get(rarity).ExtraStats, power);
            return item;
        }

        private Rarity RollRarity(GearSource source)
        {
            Rarity rolled = RollWeights(_settings.Get(source).RarityWeights);
            if (rolled < Rarity.Rare && _random.Value < _saveService.Data.GearPity * _settings.PityStep)
                return Rarity.Rare;

            return rolled;
        }

        private Rarity RollWeights(List<float> weights)
        {
            float total = 0f;
            foreach (float weight in weights)
                total += weight;

            float roll = _random.Value * total;
            for (int i = 0; i < weights.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0f && weights[i] > 0f)
                    return (Rarity)i;
            }

            return Rarity.Common;
        }

        private void UpdatePity(Rarity rarity) =>
            _saveService.Data.GearPity = rarity >= Rarity.Rare ? 0 : _saveService.Data.GearPity + 1;

        private void AddMainStats(GearItemSave item, GearSlot slot, float power)
        {
            foreach (GearStat stat in _settings.Get(slot).MainStats)
                AddStat(item, stat, _settings.Get(stat).MainValue * power);
        }

        private void CollectExtraPool(GearItemSettings kind)
        {
            _extraPool.Clear();
            List<GearStat> mainStats = _settings.Get(kind.Slot).MainStats;
            foreach (GearSettings.StatEntry entry in _settings.Stats)
                if (!mainStats.Contains(entry.Stat) && entry.Stat != kind.SignatureStat)
                    _extraPool.Add(entry.Stat);
        }

        private void AddExtraStats(GearItemSave item, int count, float power)
        {
            for (int i = 0; i < count && _extraPool.Count > 0; i++)
            {
                int index = _random.Range(0, _extraPool.Count);
                AddExtraStat(item, _extraPool[index], power);
                _extraPool.RemoveAt(index);
            }
        }

        private void AddExtraStat(GearItemSave item, GearStat stat, float power)
        {
            GearSettings.StatEntry entry = _settings.Get(stat);
            AddStat(item, stat, _random.Range(entry.ExtraMin, entry.ExtraMax) * power);
        }

        private void AddStat(GearItemSave item, GearStat stat, float value) =>
            item.Stats.Add(new GearStatSave { Stat = (int)stat, Value = Mathf.Max(1, Mathf.RoundToInt(value)) });
    }
}
