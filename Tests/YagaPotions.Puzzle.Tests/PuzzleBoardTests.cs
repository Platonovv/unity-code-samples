using YagaPotions.Gameplay.Puzzle;
using static YagaPotions.Puzzle.Tests.BoardFactory;

namespace YagaPotions.Puzzle.Tests;

public sealed class PuzzleBoardTests
{
    [Fact]
    public void Pour_MovesWholeTopRunThatFits()
    {
        var board = Create(4, [0, 1, 1, 1], []);

        var poured = board.Pour(0, 1);

        Assert.Equal(3, poured);
        Assert.Equal(1, board.Flasks[0].Count);
        Assert.Equal(1, board.Flasks[1].TopColor);
    }

    [Fact]
    public void Pour_IsLimitedByFreeSpace()
    {
        var board = Create(4, [0, 1, 1, 1], [2, 2, 1]);

        var poured = board.Pour(0, 1);

        Assert.Equal(1, poured);
        Assert.Equal(3, board.Flasks[0].Count);
        Assert.True(board.Flasks[1].IsFull);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(2, 0)]
    public void CanPour_RejectsSameFlaskOtherColorAndEmptySource(int from, int to)
    {
        var board = Create(4, [0, 1], [2], []);

        Assert.False(board.CanPour(from, to));
    }

    [Fact]
    public void CanPour_RejectsCompleteFlask()
    {
        var board = Create(2, [0, 0], []);

        Assert.False(board.CanPour(0, 1));
    }

    [Fact]
    public void Undo_RestoresPreviousState()
    {
        var board = Create(4, [0, 1, 1], [1], []);
        var before = board.GetStateKey();

        board.Pour(0, 1);
        board.Pour(0, 2);
        Assert.True(board.Undo());
        Assert.True(board.Undo());

        Assert.Equal(before, board.GetStateKey());
        Assert.Equal(0, board.MoveCount);
        Assert.False(board.Undo());
    }

    [Fact]
    public void Pour_RevealsHiddenLayer_AndUndoHidesItAgain()
    {
        var board = new PuzzleBoard([new Flask(3, [0, 1, 2], 2), new Flask(3, [], 0)]);

        board.Pour(0, 1);
        Assert.Equal(1, board.Flasks[0].HiddenCount);

        board.Undo();
        Assert.Equal(2, board.Flasks[0].HiddenCount);
        Assert.Equal(2, board.Flasks[0].TopColor);
    }

    [Fact]
    public void TopRunLength_IsZeroWhenTopLayerIsHidden()
    {
        var flask = new Flask(3, [0, 0], 2);

        Assert.Equal(0, flask.TopRunLength);
    }

    [Fact]
    public void IsSolved_WhenEveryFlaskIsCompleteOrEmpty()
    {
        Assert.True(Create(2, [0, 0], [1, 1], []).IsSolved);
        Assert.False(Create(2, [0, 1], [1, 0], []).IsSolved);
        Assert.False(Create(2, [0], [0]).IsSolved);
    }

    [Fact]
    public void StateKey_DoesNotDependOnFlaskOrder()
    {
        var board = Create(4, [0, 1], [1, 0], []);
        var swapped = Create(4, [], [1, 0], [0, 1]);

        Assert.Equal(board.GetStateKey(), swapped.GetStateKey());
    }

    [Fact]
    public void Clone_IsIndependentFromOriginal()
    {
        var board = Create(2, [0, 1], []);
        var clone = board.Clone();

        clone.Pour(0, 1);

        Assert.Equal(2, board.Flasks[0].Count);
        Assert.Equal(0, board.MoveCount);
    }
}
