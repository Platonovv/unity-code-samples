using System.Collections.Generic;

namespace YagaPotions.Gameplay.Puzzle
{
    public sealed class Flask
    {
        private readonly List<int> _layers;

        public int Capacity { get; }
        public int HiddenCount { get; private set; }
        public int Count => _layers.Count;
        public bool IsEmpty => _layers.Count == 0;
        public bool IsFull => _layers.Count == Capacity;
        public int TopColor => _layers[_layers.Count - 1];
        public bool IsComplete => IsFull && IsSingleColor;

        public bool IsSingleColor
        {
            get
            {
                for (var i = 1; i < _layers.Count; i++)
                    if (_layers[i] != _layers[0])
                        return false;

                return true;
            }
        }

        public int TopRunLength
        {
            get
            {
                if (IsEmpty || IsLayerHidden(_layers.Count - 1))
                    return 0;

                var color = TopColor;
                var length = 0;
                for (var i = _layers.Count - 1; i >= HiddenCount && _layers[i] == color; i--)
                    length++;

                return length;
            }
        }

        public Flask(int capacity, IReadOnlyList<int> layers, int hiddenCount)
        {
            Capacity = capacity;
            _layers = new List<int>(capacity);
            for (var i = 0; i < layers.Count; i++)
                _layers.Add(layers[i]);

            HiddenCount = hiddenCount;
        }

        public int GetLayer(int index) => _layers[index];

        public bool IsLayerHidden(int index) => index < HiddenCount;

        public Flask Clone() => new(Capacity, _layers, HiddenCount);

        internal void Push(int color, int amount)
        {
            for (var i = 0; i < amount; i++)
                _layers.Add(color);
        }

        internal void Pop(int amount) => _layers.RemoveRange(_layers.Count - amount, amount);

        internal bool RevealTop()
        {
            if (IsEmpty || !IsLayerHidden(_layers.Count - 1))
                return false;

            HiddenCount = _layers.Count - 1;
            return true;
        }

        internal void HideTop() => HiddenCount++;

        internal ulong GetKey()
        {
            var key = Mix((ulong)HiddenCount + 1);
            for (var i = 0; i < _layers.Count; i++)
                key = Mix(key ^ (ulong)(_layers[i] + 1));

            return Mix(key ^ (ulong)_layers.Count);
        }

        private ulong Mix(ulong value)
        {
            value += 0x9E3779B97F4A7C15UL;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}
