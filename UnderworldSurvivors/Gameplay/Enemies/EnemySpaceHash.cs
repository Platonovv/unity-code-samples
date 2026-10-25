using System.Collections.Generic;
using Leopotam.EcsProto;
using Leopotam.SpaceHash;
using UnderworldSurvivors.Services.Settings;
using UnityEngine;

namespace UnderworldSurvivors.Gameplay.Enemies
{
    sealed class EnemySpaceHash : SpaceHash2<ProtoEntity>
    {
        private const int MaxNeighbours = 32;
        private const float SelfDistanceSqr = 0.0001f;

        private readonly ProtoEntity[] _neighbours = new ProtoEntity[MaxNeighbours];
        private readonly float[] _neighbourDistancesSqr = new float[MaxNeighbours];
        private readonly Vector3[] _neighbourOffsets = new Vector3[MaxNeighbours];

        public EnemySpaceHash(RunSettings settings)
            : base(settings.HashCellSize, -settings.ArenaHalfSize, -settings.ArenaHalfSize, settings.ArenaHalfSize, settings.ArenaHalfSize)
        {
        }

        public int NeighbourCount { get; private set; }

        public void Add(ProtoEntity entity, Vector3 position) => Add(entity, position.x, position.z);

        public ProtoEntity GetNeighbour(int index) => _neighbours[index];

        public float GetNeighbourDistanceSqr(int index) => _neighbourDistancesSqr[index];

        public Vector3 GetOffsetFromNeighbour(int index) => _neighbourOffsets[index];

        public void CollectNeighbours(Vector3 position, float radius)
        {
            NeighbourCount = 0;
            float radiusSqr = radius * radius;
            GetCellRange(position, radius, out int minX, out int minY, out int maxX, out int maxY);
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                List<Item> cell = _space[Hash(x, y)];
                if (cell == null)
                    continue;

                foreach (Item item in cell)
                {
                    float distanceSqr = DistanceSqr(position, item);
                    if (distanceSqr > radiusSqr || distanceSqr < SelfDistanceSqr)
                        continue;

                    if (NeighbourCount == MaxNeighbours)
                        return;

                    _neighbours[NeighbourCount] = item.Id;
                    _neighbourDistancesSqr[NeighbourCount] = distanceSqr;
                    _neighbourOffsets[NeighbourCount] = new Vector3(position.x - item.X, 0f, position.z - item.Y);
                    NeighbourCount++;
                }
            }
        }

        public bool TryFindNearest(Vector3 position, float radius, out ProtoEntity nearest)
        {
            nearest = default;
            float bestSqr = radius * radius;
            bool found = false;
            GetCellRange(position, radius, out int minX, out int minY, out int maxX, out int maxY);
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                List<Item> cell = _space[Hash(x, y)];
                if (cell == null)
                    continue;

                foreach (Item item in cell)
                {
                    float distanceSqr = DistanceSqr(position, item);
                    if (distanceSqr >= bestSqr)
                        continue;

                    bestSqr = distanceSqr;
                    nearest = item.Id;
                    found = true;
                }
            }

            return found;
        }

        private void GetCellRange(Vector3 position, float radius, out int minX, out int minY, out int maxX, out int maxY)
        {
            minX = WorldToSpace(position.x - radius, _minX, _spaceX);
            minY = WorldToSpace(position.z - radius, _minY, _spaceY);
            maxX = WorldToSpace(position.x + radius, _minX, _spaceX);
            maxY = WorldToSpace(position.z + radius, _minY, _spaceY);
        }

        private float DistanceSqr(Vector3 position, Item item)
        {
            float dx = position.x - item.X;
            float dz = position.z - item.Y;
            return dx * dx + dz * dz;
        }
    }
}
