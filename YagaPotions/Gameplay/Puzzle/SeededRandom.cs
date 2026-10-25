namespace YagaPotions.Gameplay.Puzzle
{
    public sealed class SeededRandom
    {
        private uint _state;

        public SeededRandom(int seed)
        {
            _state = (uint)seed * 2654435761u ^ 0x9E3779B9u;
            if (_state == 0)
                _state = 1;
        }

        public int Next(int maxExclusive) => (int)(NextUInt() % (uint)maxExclusive);

        private uint NextUInt()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }
    }
}
