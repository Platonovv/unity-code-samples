using System.Collections.Generic;
using UnderworldSurvivors.Core;
using UnderworldSurvivors.Services.Settings;
using UnityEngine;

namespace UnderworldSurvivors.Gameplay.Enemies
{
    sealed class WaveDirector
    {
        private readonly WaveSettings _settings;
        private readonly EnemyTypes _types;
        private readonly IRandom _random;
        private int _waveIndex = -1;

        public WaveDirector(WaveSettings settings, EnemyTypes types, IRandom random)
        {
            _settings = settings;
            _types = types;
            _random = random;
        }

        public float Elapsed { get; private set; }
        public bool IsBossDue { get; private set; }
        public bool HasBossSpawned { get; private set; }
        public bool HasBoss => _settings.Boss != null;
        public bool IsRunOver => Elapsed >= _settings.RunDuration;
        public int BossTypeIndex => _types.IndexOf(_settings.Boss);
        public WaveSettings.Wave CurrentWave => _settings.Waves[Mathf.Max(0, _waveIndex)];

        public int RampedEnemyCount()
        {
            int next = _waveIndex + 1;
            if (_waveIndex < 0 || next >= _settings.Waves.Count)
                return CurrentWave.EnemyCount;

            WaveSettings.Wave nextWave = _settings.Waves[next];
            float progress = Mathf.InverseLerp(CurrentWave.StartSecond, nextWave.StartSecond, Elapsed);
            return Mathf.RoundToInt(Mathf.Lerp(CurrentWave.EnemyCount, nextWave.EnemyCount, progress));
        }

        public void Advance(float deltaTime)
        {
            float previous = Elapsed;
            Elapsed += deltaTime;
            IsBossDue = HasBoss && previous < _settings.BossSecond && Elapsed >= _settings.BossSecond;
            HasBossSpawned |= IsBossDue;
            EnterReachedWaves();
        }

        public int PickEnemyType()
        {
            List<EnemyTypeSettings> enemies = CurrentWave.Enemies;
            return _types.IndexOf(enemies[_random.Range(0, enemies.Count)]);
        }

        public bool TryPickElite(out int typeIndex)
        {
            List<EnemyTypeSettings> elites = _settings.Elites;
            typeIndex = elites.Count > 0 ? _types.IndexOf(elites[_random.Range(0, elites.Count)]) : -1;
            return typeIndex >= 0;
        }

        private void EnterReachedWaves()
        {
            while (_waveIndex + 1 < _settings.Waves.Count && Elapsed >= _settings.Waves[_waveIndex + 1].StartSecond)
                _waveIndex++;
        }
    }
}
