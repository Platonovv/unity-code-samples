using System;
using System.Collections.Generic;
using UnityEngine;
using UnderworldSurvivors.Services.Map;

namespace UnderworldSurvivors.Gameplay.Enemies
{
    sealed class EnemyFlowField
    {
        private const ushort Unreached = ushort.MaxValue;
        private const int StraightCost = 2;
        private const int DiagonalCost = 3;
        private const int DetourSlack = 4;
        private const int LookAheadSteps = 2;
        private const int DirectionCount = 8;
        private const int FirstDiagonal = 4;
        private const float SightRange = 4f;
        private const int RebuildShift = 2;
        private const int NoTarget = -100000;
        private const float SampleShare = 0.5f;
        private const float MinLineLength = 0.01f;

        private readonly int[] _stepColumns = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private readonly int[] _stepRows = { 0, 0, 1, -1, 1, -1, 1, -1 };
        private readonly MapWalkAreaView _walkArea;
        private readonly ushort[] _costs;
        private readonly List<int>[] _buckets = new List<int>[DiagonalCost + 1];
        private readonly List<int> _touched = new(16384);
        private readonly int _columns;
        private readonly int _maxCost;
        private int _targetColumn = NoTarget;
        private int _targetRow = NoTarget;

        public EnemyFlowField(MapWalkAreaView walkArea, float radius)
        {
            _walkArea = walkArea;
            _columns = walkArea.Columns;
            _costs = new ushort[walkArea.Columns * walkArea.Rows];
            Array.Fill(_costs, Unreached);
            for (int i = 0; i < _buckets.Length; i++)
                _buckets[i] = new List<int>(1024);

            _maxCost = Mathf.CeilToInt(radius / walkArea.CellSize) * StraightCost;
        }

        public void Follow(Vector3 target)
        {
            int column = _walkArea.ColumnAt(target.x);
            int row = _walkArea.RowAt(target.z);
            if (!HasShifted(column, row) || !_walkArea.IsWalkableCell(column, row))
                return;

            _targetColumn = column;
            _targetRow = row;
            Rebuild();
        }

        public Vector3 WaypointTowards(Vector3 position, Vector3 target)
        {
            int column = _walkArea.ColumnAt(position.x);
            int row = _walkArea.RowAt(position.z);
            int cost = CostAt(column, row);
            if (cost == Unreached)
                return _walkArea.RouteTowards(position, target);

            if (cost <= OctileCost(column, row) + DetourSlack && IsClearLine(position, target))
                return target;

            int steps = 0;
            while (steps < LookAheadSteps && TryStepDown(ref column, ref row))
                steps++;

            return _walkArea.CellCenter(column, row);
        }

        private bool HasShifted(int column, int row) =>
            Mathf.Abs(column - _targetColumn) >= RebuildShift || Mathf.Abs(row - _targetRow) >= RebuildShift;

        private void Rebuild()
        {
            ResetCosts();
            int start = _targetRow * _columns + _targetColumn;
            Reach(start, 0);
            int pending = 1;
            for (int current = 0; pending > 0 && current <= _maxCost; current++)
            {
                List<int> bucket = _buckets[current % _buckets.Length];
                for (int i = 0; i < bucket.Count; i++)
                {
                    pending--;
                    if (_costs[bucket[i]] == current)
                        pending += Expand(bucket[i], current);
                }

                bucket.Clear();
            }
        }

        private void ResetCosts()
        {
            foreach (int index in _touched)
                _costs[index] = Unreached;

            _touched.Clear();
            foreach (List<int> bucket in _buckets)
                bucket.Clear();
        }

        private int Expand(int index, int cost)
        {
            int column = index % _columns;
            int row = index / _columns;
            int added = 0;
            for (int direction = 0; direction < DirectionCount; direction++)
            {
                if (!CanStep(column, row, direction))
                    continue;

                int next = (row + _stepRows[direction]) * _columns + column + _stepColumns[direction];
                int nextCost = cost + (direction < FirstDiagonal ? StraightCost : DiagonalCost);
                if (nextCost >= _costs[next])
                    continue;

                Reach(next, nextCost);
                added++;
            }

            return added;
        }

        private void Reach(int index, int cost)
        {
            if (_costs[index] == Unreached)
                _touched.Add(index);

            _costs[index] = (ushort)cost;
            _buckets[cost % _buckets.Length].Add(index);
        }

        private bool TryStepDown(ref int column, ref int row)
        {
            int best = CostAt(column, row);
            int bestDirection = -1;
            for (int direction = 0; direction < DirectionCount; direction++)
            {
                if (!CanStep(column, row, direction))
                    continue;

                int cost = CostAt(column + _stepColumns[direction], row + _stepRows[direction]);
                if (cost < best)
                {
                    best = cost;
                    bestDirection = direction;
                }
            }

            if (bestDirection < 0)
                return false;

            column += _stepColumns[bestDirection];
            row += _stepRows[bestDirection];
            return true;
        }

        private bool CanStep(int column, int row, int direction)
        {
            int nextColumn = column + _stepColumns[direction];
            int nextRow = row + _stepRows[direction];
            if (!_walkArea.IsWalkableCell(nextColumn, nextRow))
                return false;

            return direction < FirstDiagonal || (_walkArea.IsWalkableCell(nextColumn, row) && _walkArea.IsWalkableCell(column, nextRow));
        }

        private bool IsClearLine(Vector3 from, Vector3 to)
        {
            Vector3 line = to - from;
            line.y = 0f;
            float length = Mathf.Min(line.magnitude, SightRange);
            if (length < MinLineLength)
                return true;

            float sample = _walkArea.CellSize * SampleShare;
            Vector3 step = line.normalized * sample;
            int samples = Mathf.CeilToInt(length / sample);
            for (int i = 1; i <= samples; i++)
                if (!_walkArea.IsWalkable(from + step * i))
                    return false;

            return true;
        }

        private int CostAt(int column, int row) =>
            _walkArea.IsWalkableCell(column, row) ? _costs[row * _columns + column] : Unreached;

        private int OctileCost(int column, int row)
        {
            int columns = Mathf.Abs(column - _targetColumn);
            int rows = Mathf.Abs(row - _targetRow);
            return StraightCost * Mathf.Max(columns, rows) + (DiagonalCost - StraightCost) * Mathf.Min(columns, rows);
        }
    }
}
