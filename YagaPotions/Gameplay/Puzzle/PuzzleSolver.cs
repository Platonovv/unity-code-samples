using System.Collections.Generic;

namespace YagaPotions.Gameplay.Puzzle
{
    public sealed class PuzzleSolver
    {
        private const int MaxDepth = 200;

        private readonly int _nodeLimit;
        private readonly HashSet<ulong> _visited = new();
        private readonly List<PourMove> _path = new();
        private readonly List<List<PourMove>> _movesByDepth = new();
        private readonly List<PourMove> _toEmpty = new();
        private int _nodes;

        public PuzzleSolver(int nodeLimit)
        {
            _nodeLimit = nodeLimit;
        }

        public bool TrySolve(PuzzleBoard board, List<PourMove> solution)
        {
            _visited.Clear();
            _path.Clear();
            _nodes = 0;
            solution.Clear();

            var found = Search(board.Clone());
            if (found)
                solution.AddRange(_path);

            _visited.Clear();
            return found;
        }

        private bool Search(PuzzleBoard board)
        {
            if (board.IsSolved)
                return true;

            if (_path.Count >= MaxDepth || ++_nodes > _nodeLimit || !_visited.Add(board.GetStateKey()))
                return false;

            var moves = CollectMoves(board, _path.Count);
            foreach (var move in moves)
            {
                board.Pour(move.From, move.To);
                _path.Add(move);

                if (Search(board))
                    return true;

                _path.RemoveAt(_path.Count - 1);
                board.Undo();

                if (_nodes > _nodeLimit)
                    return false;
            }

            return false;
        }

        private List<PourMove> CollectMoves(PuzzleBoard board, int depth)
        {
            var toColor = GetMoveList(depth);
            var toEmpty = _toEmpty;
            toEmpty.Clear();
            var flasks = board.Flasks;

            for (var from = 0; from < flasks.Count; from++)
            {
                var firstEmptyTried = false;
                for (var to = 0; to < flasks.Count; to++)
                {
                    if (!board.CanPour(from, to))
                        continue;

                    if (!flasks[to].IsEmpty)
                    {
                        toColor.Add(new PourMove(from, to));
                        continue;
                    }

                    if (firstEmptyTried || IsWholeFlaskRun(flasks[from]))
                        continue;

                    firstEmptyTried = true;
                    toEmpty.Add(new PourMove(from, to));
                }
            }

            toColor.AddRange(toEmpty);
            return toColor;
        }

        private List<PourMove> GetMoveList(int depth)
        {
            while (_movesByDepth.Count <= depth)
                _movesByDepth.Add(new List<PourMove>());

            var moves = _movesByDepth[depth];
            moves.Clear();
            return moves;
        }

        private bool IsWholeFlaskRun(Flask flask) => flask.TopRunLength == flask.Count;
    }
}
