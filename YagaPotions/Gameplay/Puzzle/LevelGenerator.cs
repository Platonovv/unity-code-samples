using System;
using System.Collections.Generic;

namespace YagaPotions.Gameplay.Puzzle
{
    public sealed class LevelGenerator
    {
        private const int AttemptsPerLayout = 20;
        private const int SeedStep = 7919;
        private const int MaxExtraEmptyFlasks = 3;

        private readonly PuzzleSolver _solver;
        private readonly List<PourMove> _solution = new();
        private readonly List<int> _pool = new();
        private readonly List<int> _layers = new();

        public LevelGenerator(PuzzleSolver solver)
        {
            _solver = solver;
        }

        public PuzzleBoard Generate(LevelParameters parameters)
        {
            Validate(parameters);

            for (var extra = 0; extra <= MaxExtraEmptyFlasks; extra++)
            {
                if (TryGenerate(parameters, out var board))
                    return board;

                parameters = parameters.WithExtraEmptyFlask();
            }

            throw new InvalidOperationException($"Level {parameters.Seed} is not solvable with {parameters.ColorCount} colors");
        }

        private void Validate(LevelParameters parameters)
        {
            if (parameters.ColorCount < 2 || parameters.Capacity < 2 || parameters.EmptyFlasks < 0 || parameters.HiddenLayers < 0)
                throw new ArgumentException($"Invalid level parameters for level {parameters.Seed}");
        }

        private bool TryGenerate(LevelParameters parameters, out PuzzleBoard board)
        {
            for (var attempt = 0; attempt < AttemptsPerLayout; attempt++)
            {
                board = CreateShuffledBoard(parameters, parameters.Seed * SeedStep + attempt);
                if (HasNoCompleteFlask(board) && _solver.TrySolve(board, _solution))
                    return true;
            }

            board = null;
            return false;
        }

        private PuzzleBoard CreateShuffledBoard(LevelParameters parameters, int seed)
        {
            FillPool(parameters);
            Shuffle(new SeededRandom(seed));

            var flasks = new List<Flask>(parameters.ColorCount + parameters.EmptyFlasks);
            var hidden = parameters.HiddenLayers < parameters.Capacity ? parameters.HiddenLayers : parameters.Capacity - 1;

            for (var i = 0; i < parameters.ColorCount; i++)
                flasks.Add(CreateFilledFlask(parameters.Capacity, i * parameters.Capacity, hidden));

            for (var i = 0; i < parameters.EmptyFlasks; i++)
                flasks.Add(new Flask(parameters.Capacity, Array.Empty<int>(), 0));

            return new PuzzleBoard(flasks);
        }

        private void FillPool(LevelParameters parameters)
        {
            _pool.Clear();
            for (var color = 0; color < parameters.ColorCount; color++)
                for (var i = 0; i < parameters.Capacity; i++)
                    _pool.Add(color);
        }

        private void Shuffle(SeededRandom random)
        {
            for (var i = _pool.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (_pool[i], _pool[j]) = (_pool[j], _pool[i]);
            }
        }

        private Flask CreateFilledFlask(int capacity, int start, int hidden)
        {
            _layers.Clear();
            for (var i = 0; i < capacity; i++)
                _layers.Add(_pool[start + i]);

            return new Flask(capacity, _layers, hidden);
        }

        private bool HasNoCompleteFlask(PuzzleBoard board)
        {
            foreach (var flask in board.Flasks)
                if (!flask.IsEmpty && flask.IsSingleColor)
                    return false;

            return true;
        }
    }
}
