using Leopotam.EcsProto;
using Leopotam.EcsProto.QoL;
using UnderworldSurvivors.Gameplay.Common;
using UnderworldSurvivors.Services.Assets;
using UnderworldSurvivors.Services.Input;
using UnityEngine;
using UnderworldSurvivors.Services.Map;

namespace UnderworldSurvivors.Gameplay.Players
{
    sealed class PlayerMoveSystem : IProtoInitSystem, IProtoRunSystem
    {
        private const float FacingTurnThreshold = 0.2f;

        [DI] private PlayerAspect _player;
        [DI] private CommonAspect _common;
        [DI] private InputService _input;
        [DI] private AssetProvider _assetProvider;
        [DI] private MapWalkAreaView _walkArea;

        private float _knockbackDamping;
        private float _mapHalfSize;
        private float _hasteMultiplier;

        public void Init(IProtoSystems systems)
        {
            _knockbackDamping = _assetProvider.GetPlayerCombatSettings().KnockbackDamping;
            _mapHalfSize = _assetProvider.GetRunSettings().MapHalfSize;
            _hasteMultiplier = _assetProvider.GetLootSettings().HasteMultiplier;
        }

        public void Run()
        {
            Vector2 input = _input.Move;
            var direction = new Vector3(input.x, 0f, input.y);
            float deltaTime = Time.deltaTime;

            foreach (ProtoEntity entity in _player.PlayersIt)
            {
                if (_player.Deaths.Has(entity))
                {
                    _player.Players.Get(entity).Motion = 0f;
                    continue;
                }

                ref Placement pose = ref _common.Placements.Get(entity);
                ref Player player = ref _player.Players.Get(entity);
                if (_player.IsDashing(entity))
                {
                    MoveDash(entity, ref pose, ref player, deltaTime);
                    continue;
                }

                player.HasteLeft = Mathf.Max(0f, player.HasteLeft - deltaTime);
                float speed = (player.HasteLeft > 0f ? player.Speed * _hasteMultiplier : player.Speed) * (1f - player.Slow) * (1f - player.Chill);
                pose.Position = _walkArea.Slide(pose.Position, ClampToMap(pose.Position + (direction * speed + player.Knockback) * deltaTime));
                player.Motion = input.magnitude;
                UpdateFacing(ref player, direction.x);
                player.Knockback *= Mathf.Exp(-_knockbackDamping * deltaTime);
            }
        }

        private void MoveDash(ProtoEntity entity, ref Placement pose, ref Player player, float deltaTime)
        {
            ref Dash dash = ref _player.Dashes.Get(entity);
            float step = Mathf.Min(deltaTime, dash.TimeLeft);
            dash.TimeLeft -= deltaTime;
            pose.Position = _walkArea.Slide(pose.Position, ClampToMap(pose.Position + dash.Velocity * step));
            player.Motion = 1f;
            player.Knockback = Vector3.zero;
            UpdateFacing(ref player, dash.Velocity.x);
        }

        private Vector3 ClampToMap(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, -_mapHalfSize, _mapHalfSize);
            position.z = Mathf.Clamp(position.z, -_mapHalfSize, _mapHalfSize);
            return position;
        }

        private void UpdateFacing(ref Player player, float horizontal)
        {
            if (horizontal > FacingTurnThreshold)
                player.FacingLeft = false;
            else if (horizontal < -FacingTurnThreshold)
                player.FacingLeft = true;
        }
    }
}
