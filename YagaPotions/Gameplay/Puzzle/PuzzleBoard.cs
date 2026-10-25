using System;
using System.Collections.Generic;

namespace YagaPotions.Gameplay.Puzzle
{
    public sealed class PuzzleBoard
    {
        private readonly List<Flask> _flasks;
        private readonly Stack<PourRecord> _history = new();

        public IReadOnlyList<Flask> Flasks => _flasks;
        public int MoveCount => _history.Count;
        public bool CanUndo => _history.Count > 0;

        public bool IsSolved
        {
            get
            {
                foreach (var flask in _flasks)
                    if (!flask.IsEmpty && !flask.IsComplete)
                        return false;

                return true;
            }
        }

        public PuzzleBoard(IReadOnlyList<Flask> flasks)
        {
            _flasks = new List<Flask>(flasks);
        }

        public void AddEmptyFlask() => _flasks.Add(new Flask(_flasks[0].Capacity, Array.Empty<int>(), 0));

        public bool CanPour(int from, int to)
        {
            if (from == to)
                return false;

            var source = _flasks[from];
            var target = _flasks[to];
            if (source.IsEmpty || source.IsComplete || target.IsFull || source.TopRunLength == 0)
                return false;

            return target.IsEmpty || target.TopColor == source.TopColor;
        }

        public int Pour(int from, int to)
        {
            if (!CanPour(from, to))
                return 0;

            var source = _flasks[from];
            var target = _flasks[to];
            var color = source.TopColor;
            var amount = GetPourAmount(source, target);

            source.Pop(amount);
            target.Push(color, amount);
            var revealed = source.RevealTop();

            _history.Push(new PourRecord(from, to, amount, revealed));
            return amount;
        }

        public bool Undo()
        {
            if (_history.Count == 0)
                return false;

            var record = _history.Pop();
            var source = _flasks[record.From];
            var target = _flasks[record.To];
            var color = target.TopColor;

            if (record.Revealed)
                source.HideTop();

            target.Pop(record.Amount);
            source.Push(color, record.Amount);
            return true;
        }

        public PuzzleBoard Clone()
        {
            var flasks = new Flask[_flasks.Count];
            for (var i = 0; i < _flasks.Count; i++)
                flasks[i] = _flasks[i].Clone();

            return new PuzzleBoard(flasks);
        }

        public ulong GetStateKey()
        {
            var key = 0UL;
            foreach (var flask in _flasks)
                key += flask.GetKey();

            return key;
        }

        private int GetPourAmount(Flask source, Flask target)
        {
            var free = target.Capacity - target.Count;
            var run = source.TopRunLength;
            return run < free ? run : free;
        }

        private readonly struct PourRecord
        {
            public int From { get; }
            public int To { get; }
            public int Amount { get; }
            public bool Revealed { get; }

            public PourRecord(int from, int to, int amount, bool revealed)
            {
                From = from;
                To = to;
                Amount = amount;
                Revealed = revealed;
            }
        }
    }
}
