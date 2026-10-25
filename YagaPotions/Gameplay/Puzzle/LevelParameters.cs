namespace YagaPotions.Gameplay.Puzzle
{
    public readonly struct LevelParameters
    {
        public int Seed { get; }
        public int ColorCount { get; }
        public int Capacity { get; }
        public int EmptyFlasks { get; }
        public int HiddenLayers { get; }

        public LevelParameters(int seed, int colorCount, int capacity, int emptyFlasks, int hiddenLayers)
        {
            Seed = seed;
            ColorCount = colorCount;
            Capacity = capacity;
            EmptyFlasks = emptyFlasks;
            HiddenLayers = hiddenLayers;
        }

        public LevelParameters WithExtraEmptyFlask() => new(Seed, ColorCount, Capacity, EmptyFlasks + 1, HiddenLayers);
    }
}
