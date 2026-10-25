using YagaPotions.Gameplay.Puzzle;

namespace YagaPotions.Puzzle.Tests;

internal static class BoardFactory
{
    public static PuzzleBoard Create(int capacity, params int[][] flasks) =>
        new(flasks.Select(layers => new Flask(capacity, layers, 0)).ToArray());

    public static void Apply(PuzzleBoard board, IEnumerable<PourMove> moves)
    {
        foreach (var move in moves)
            Assert.True(board.Pour(move.From, move.To) > 0, $"Move {move.From}->{move.To} is not legal");
    }
}
