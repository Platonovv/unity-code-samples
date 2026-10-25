using YagaPotions.Gameplay.Puzzle;
using static YagaPotions.Puzzle.Tests.BoardFactory;

namespace YagaPotions.Puzzle.Tests;

public sealed class PuzzleSolverTests
{
    private const int NodeLimit = 30000;

    [Fact]
    public void TrySolve_FindsSolutionThatSolvesTheBoard()
    {
        var board = Create(2, [0, 1], [1, 0], []);
        var solution = new List<PourMove>();

        Assert.True(new PuzzleSolver(NodeLimit).TrySolve(board, solution));

        Apply(board, solution);
        Assert.True(board.IsSolved);
    }

    [Fact]
    public void TrySolve_DoesNotChangeTheBoard()
    {
        var board = Create(2, [0, 1], [1, 0], []);
        var before = board.GetStateKey();

        new PuzzleSolver(NodeLimit).TrySolve(board, new List<PourMove>());

        Assert.Equal(before, board.GetStateKey());
        Assert.Equal(0, board.MoveCount);
    }

    [Fact]
    public void TrySolve_ReturnsEmptySolutionForSolvedBoard()
    {
        var solution = new List<PourMove> { new(0, 1) };

        Assert.True(new PuzzleSolver(NodeLimit).TrySolve(Create(2, [0, 0], []), solution));
        Assert.Empty(solution);
    }

    [Fact]
    public void TrySolve_FailsWhenNoMoveIsPossible()
    {
        var board = Create(2, [0, 1], [1, 0]);
        var solution = new List<PourMove>();

        Assert.False(new PuzzleSolver(NodeLimit).TrySolve(board, solution));
        Assert.Empty(solution);
    }

    [Fact]
    public void TrySolve_GivesUpAfterNodeLimit()
    {
        var board = Create(4, [0, 1, 2, 0], [1, 2, 0, 1], [2, 0, 1, 2], [], []);

        Assert.False(new PuzzleSolver(1).TrySolve(board, new List<PourMove>()));
    }

    [Fact]
    public void Solver_IsReusableBetweenBoards()
    {
        var solver = new PuzzleSolver(NodeLimit);
        var solution = new List<PourMove>();

        Assert.False(solver.TrySolve(Create(2, [0, 1], [1, 0]), solution));
        Assert.True(solver.TrySolve(Create(2, [0, 1], [1, 0], []), solution));
        Assert.NotEmpty(solution);
    }
}
