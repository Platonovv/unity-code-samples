namespace YagaPotions.Gameplay.Puzzle
{
    public readonly struct PourMove
    {
        public int From { get; }
        public int To { get; }

        public PourMove(int from, int to)
        {
            From = from;
            To = to;
        }
    }
}
