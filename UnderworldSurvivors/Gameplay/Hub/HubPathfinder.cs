using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnderworldSurvivors.Gameplay.Hub
{
    sealed class HubPathfinder
    {
        private const int Unvisited = -2;
        private const int Root = -1;
        private const float SampleShare = 0.5f;

        private readonly HubWalkAreaView _area;
        private readonly int[] _parents;
        private readonly int[] _queue;
        private readonly Vector2Int[] _steps =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
        };

        public HubPathfinder(HubWalkAreaView area)
        {
            _area = area;
            _parents = new int[area.Columns * area.Rows];
            _queue = new int[_parents.Length];
        }

        public bool TryFind(Vector3 from, Vector3 target, float arriveRadius, List<Vector3> path)
        {
            path.Clear();
            int goal = Search(_area.CellOf(from), target, arriveRadius * arriveRadius);
            if (goal == Root)
                return false;

            Collect(goal, path);
            Straighten(from, path);
            return true;
        }

        private int Search(Vector2Int start, Vector3 target, float arriveRadiusSqr)
        {
            if (!IsInside(start))
                return Root;

            Array.Fill(_parents, Unvisited);
            int head = 0;
            int tail = 0;
            int startIndex = IndexOf(start);
            _parents[startIndex] = Root;
            _queue[tail++] = startIndex;
            while (head < tail)
            {
                int index = _queue[head++];
                Vector2Int cell = CellAt(index);
                if (IsArrived(cell, target, arriveRadiusSqr))
                    return index;

                foreach (Vector2Int step in _steps)
                    tail = Visit(cell, step, index, tail);
            }

            return Root;
        }

        private int Visit(Vector2Int cell, Vector2Int step, int parent, int tail)
        {
            Vector2Int next = cell + step;
            if (!CanStep(cell, step))
                return tail;

            int nextIndex = IndexOf(next);
            if (_parents[nextIndex] != Unvisited)
                return tail;

            _parents[nextIndex] = parent;
            _queue[tail] = nextIndex;
            return tail + 1;
        }

        private bool CanStep(Vector2Int cell, Vector2Int step)
        {
            if (!_area.IsWalkableCell(cell + step))
                return false;

            bool isDiagonal = step.x != 0 && step.y != 0;
            return !isDiagonal || (_area.IsWalkableCell(new Vector2Int(cell.x + step.x, cell.y)) && _area.IsWalkableCell(new Vector2Int(cell.x, cell.y + step.y)));
        }

        private bool IsArrived(Vector2Int cell, Vector3 target, float arriveRadiusSqr)
        {
            Vector3 offset = _area.CellCenter(cell) - target;
            return offset.x * offset.x + offset.z * offset.z <= arriveRadiusSqr;
        }

        private void Collect(int goal, List<Vector3> path)
        {
            for (int index = goal; _parents[index] != Root; index = _parents[index])
                path.Add(_area.CellCenter(CellAt(index)));

            path.Reverse();
        }

        private void Straighten(Vector3 from, List<Vector3> path)
        {
            int write = 0;
            Vector3 anchor = from;
            int next = 0;
            while (next < path.Count)
            {
                int far = next;
                while (far + 1 < path.Count && IsClear(anchor, path[far + 1]))
                    far++;

                anchor = path[far];
                path[write++] = anchor;
                next = far + 1;
            }

            path.RemoveRange(write, path.Count - write);
        }

        private bool IsClear(Vector3 from, Vector3 to)
        {
            Vector3 offset = to - from;
            offset.y = 0f;
            float sample = _area.CellSize * SampleShare;
            int samples = Mathf.CeilToInt(offset.magnitude / sample);
            for (int i = 1; i <= samples; i++)
            {
                if (!_area.IsWalkable(from + offset * ((float)i / samples)))
                    return false;
            }

            return true;
        }

        private bool IsInside(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < _area.Columns && cell.y < _area.Rows;

        private int IndexOf(Vector2Int cell) => cell.y * _area.Columns + cell.x;

        private Vector2Int CellAt(int index) => new(index % _area.Columns, index / _area.Columns);
    }
}
