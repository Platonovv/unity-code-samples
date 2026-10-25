using Leopotam.EcsProto;
using Leopotam.EcsProto.QoL;
using UnderworldSurvivors.Gameplay.Common;
using UnderworldSurvivors.Gameplay.Players;
using UnderworldSurvivors.Services.Assets;
using UnderworldSurvivors.Services.Run;
using UnderworldSurvivors.Services.Settings;
using UnityEngine;

namespace UnderworldSurvivors.Gameplay.Enemies
{
    sealed class EnemySpawnSystem : IProtoInitSystem, IProtoRunSystem
    {
        private const float FirstSpecialShare = 0.4f;

        [DI] private EnemyAspect _enemy;
        [DI] private PlayerAspect _player;
        [DI] private CommonAspect _common;
        [DI] private AssetProvider _assetProvider;
        [DI] private RunStats _stats;
        [DI] private RunReactive _run;
        [DI] private EnemySpawnArea _spawnArea;
        [DI] private EnemyTypes _types;
        [DI] private WaveDirector _director;
        [DI] private CurseScale _curse;

        private EliteSettings _elite;
        private int _maxSpawnsPerFrame;
        private int _nextElite;
        private Vector3 _center;
        private float _spawnBudget;

        public void Init(IProtoSystems systems)
        {
            _maxSpawnsPerFrame = _assetProvider.GetRunSettings().MaxSpawnsPerFrame;
            _elite = _assetProvider.GetEliteSettings();
        }

        public void Run()
        {
            foreach (ProtoEntity player in _player.PlayersIt)
                _center = _common.Placements.Get(player).Position;

            if (_director.IsBossDue)
                SpawnBoss();

            SpawnSummoned();
            SpawnDueElite();
            int difference = _stats.TargetEnemyCount - (_stats.EnemyCount - _stats.SummonedCount);
            if (difference > 0 && !_run.IsLooting)
                SpawnFromWave(TakeSpawnBudget(difference));
            else if (difference < 0)
                Despawn(-difference);
        }

        private int TakeSpawnBudget(int wanted)
        {
            _spawnBudget = Mathf.Min(_spawnBudget + _director.CurrentWave.SpawnsPerSecond * Time.deltaTime, _maxSpawnsPerFrame);
            int count = Mathf.Min(wanted, Mathf.FloorToInt(_spawnBudget));
            _spawnBudget -= count;
            return count;
        }

        private void SpawnBoss()
        {
            Spawn(_director.BossTypeIndex, _spawnArea.GetPoint(_center), false, false);
            _run.BossNameKey = _types.Get(_director.BossTypeIndex).NameKey;
            _run.IsBossAlive = true;
            _run.BossHealthPercent = 1f;
        }

        private void SpawnFromWave(int count)
        {
            for (int i = 0; i < count; i++)
                Spawn(_director.PickEnemyType(), _spawnArea.GetPoint(_center), false, false);
        }

        private void SpawnSummoned()
        {
            foreach (ProtoEntity entity in _enemy.SummonedSpawnsIt)
            {
                SummonedSpawn spawn = _enemy.SummonedSpawns.Get(entity);
                ProtoEntity enemy = Spawn(spawn.TypeIndex, _spawnArea.Place(spawn.Position), true, spawn.IsElite);
                if (spawn.RushDistance > 0f)
                    StartRush(enemy, spawn);

                _enemy.World().DelEntity(entity);
            }
        }

        private void StartRush(ProtoEntity enemy, in SummonedSpawn spawn)
        {
            ref Rush rush = ref _enemy.Rushes.Add(enemy);
            rush.Direction = spawn.RushDirection;
            rush.Speed = spawn.RushSpeed;
            rush.DistanceLeft = spawn.RushDistance;
            _enemy.Enemies.Get(enemy).FacingLeft = spawn.RushDirection.x < 0f;
        }

        private void SpawnDueElite()
        {
            float[] seconds = _elite.SpawnSeconds;
            if (_nextElite >= seconds.Length || _director.Elapsed < seconds[_nextElite])
                return;

            _nextElite++;
            if (!_run.IsBossAlive && !_run.IsLooting && _director.TryPickElite(out int typeIndex))
                Spawn(typeIndex, _spawnArea.GetPoint(_center), false, true);
        }

        private ProtoEntity Spawn(int typeIndex, Vector3 position, bool isSummoned, bool isElite)
        {
            EnemyTypeSettings type = _types.Get(typeIndex);
            ref Enemy enemy = ref _enemy.Enemies.NewEntity(out ProtoEntity entity);
            enemy.TypeIndex = typeIndex;
            enemy.MaxHealth = _curse.Health(isElite ? _elite.HealthOf(type) : type.Health);
            enemy.Health = enemy.MaxHealth;
            enemy.AnimationSeed = Random.value;
            enemy.IsSummoned = isSummoned;
            enemy.IsElite = isElite;
            ref EnemyAttack attack = ref _enemy.Attacks.Add(entity);
            attack.Damage = _curse.Damage(isElite ? type.Damage * _elite.DamageMultiplier : type.Damage);
            attack.Cooldown = Random.Range(0f, type.AttackInterval);
            attack.MillstoneCooldown = type.MillstoneCooldown * FirstSpecialShare;
            attack.SummonCooldown = type.SummonCooldown * FirstSpecialShare;
            attack.HealCooldown = type.HealCooldown * FirstSpecialShare;
            attack.ChargeCooldown = type.ChargeCooldown * FirstSpecialShare;
            _common.Placements.Add(entity).Position = position;
            _stats.EnemyCount++;
            if (isSummoned)
                _stats.SummonedCount++;

            return entity;
        }

        private void Despawn(int count)
        {
            foreach (ProtoEntity entity in _enemy.AliveIt)
            {
                if (count <= 0)
                    return;

                ref Enemy enemy = ref _enemy.Enemies.Get(entity);
                if (enemy.IsSummoned || enemy.IsElite || _types.Get(enemy.TypeIndex).IsBoss)
                    continue;

                _enemy.Dead.Add(entity).Killed = false;
                count--;
            }
        }
    }
}
