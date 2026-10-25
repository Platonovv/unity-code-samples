using YagaPotions.Gameplay.Puzzle;
using static YagaPotions.Puzzle.Tests.BoardFactory;

namespace YagaPotions.Puzzle.Tests;

public sealed class LevelGeneratorTests
{
    private const int NodeLimit = 30000;
    private const int Capacity = 4;
    private const int EmptyFlasks = 2;

    public static TheoryData<int, int, int> Levels()
    {
        var data = new TheoryData<int, int, int>();
        for (var seed = 1; seed <= 40; seed++)
            data.Add(seed, 3 + seed % 6, seed % 3 == 0 ? 3 : 0);

        return data;
    }

    [Theory]
    [MemberData(nameof(Levels))]
    public void Generate_CreatesSolvableLevel(int seed, int colors, int hidden)
    {
        var board = Generate(new LevelParameters(seed, colors, Capacity, EmptyFlasks, hidden));
        var solution = new List<PourMove>();

        Assert.True(new PuzzleSolver(NodeLimit).TrySolve(board, solution));
        Apply(board, solution);
        Assert.True(board.IsSolved);
    }

    [Theory]
    [MemberData(nameof(Levels))]
    public void Generate_UsesEveryColorExactlyCapacityTimes(int seed, int colors, int hidden)
    {
        var board = Generate(new LevelParameters(seed, colors, Capacity, EmptyFlasks, hidden));

        var counts = new int[colors];
        foreach (var flask in board.Flasks)
            for (var i = 0; i < flask.Count; i++)
                counts[flask.GetLayer(i)]++;

        Assert.All(counts, count => Assert.Equal(Capacity, count));
    }

    [Theory]
    [MemberData(nameof(Levels))]
    public void Generate_StartsWithoutSortedFlasks(int seed, int colors, int hidden)
    {
        var board = Generate(new LevelParameters(seed, colors, Capacity, EmptyFlasks, hidden));

        Assert.DoesNotContain(board.Flasks, flask => !flask.IsEmpty && flask.IsSingleColor);
    }

    [Fact]
    public void Generate_IsDeterministicForSeed()
    {
        var parameters = new LevelParameters(17, 7, Capacity, EmptyFlasks, 0);

        Assert.Equal(Generate(parameters).GetStateKey(), Generate(parameters).GetStateKey());
    }

    [Fact]
    public void Generate_GivesDifferentLevelsForDifferentSeeds()
    {
        var keys = new HashSet<ulong>();
        for (var seed = 1; seed <= 20; seed++)
            keys.Add(Generate(new LevelParameters(seed, 6, Capacity, EmptyFlasks, 0)).GetStateKey());

        Assert.Equal(20, keys.Count);
    }

    [Theory]
    [InlineData(1, 4, 2, 0)]
    [InlineData(5, 1, 2, 0)]
    [InlineData(5, 4, -1, 0)]
    [InlineData(5, 4, 2, -1)]
    public void Generate_RejectsInvalidParameters(int colors, int capacity, int empty, int hidden)
    {
        var parameters = new LevelParameters(1, colors, capacity, empty, hidden);

        Assert.Throws<ArgumentException>(() => Generate(parameters));
    }

    private static PuzzleBoard Generate(LevelParameters parameters) =>
        new LevelGenerator(new PuzzleSolver(NodeLimit)).Generate(parameters);
}
