using System;
using System.Collections.Generic;
using GameData;
using Services;
using Settings;

namespace Shelves
{
	public sealed class LevelGenerator
	{
		private const int SeedStep = 7919;
		private const int MaxTripleDrop = 3;

		private readonly LevelSettings _settings;
		private readonly List<ShelfMove> _reversedSolution = new();
		private readonly List<ShelfMove> _solution = new();
		private readonly List<int> _candidates = new();
		private Random _random;
		private int _kinds;

		public LevelGenerator(AssetProvider assetProvider) => _settings = assetProvider.GetLevelSettings();

		public IReadOnlyList<ShelfMove> Solution => _solution;

		public Board Generate(LevelPreset preset, int kinds, int seed)
		{
			var triples = FitTriples(preset);
			var minTriples = Math.Max(1, triples - MaxTripleDrop);
			_kinds = Math.Clamp(kinds, 1, triples);
			for (; triples >= minTriples; triples--)
			{
				if (!TryGenerate(preset, triples, seed, out var board))
					continue;
				StoreSolution();
				return board;
			}
			throw new InvalidOperationException($"No playable board for seed {seed} with {preset.ShelfCount} shelves");
		}

		private bool TryGenerate(LevelPreset preset, int triples, int seed, out Board board)
		{
			board = null;
			for (var attempt = 1; attempt <= _settings.MaxAttempts; attempt++)
			{
				_random = new Random(seed * SeedStep + attempt);
				_reversedSolution.Clear();
				board = new Board(preset.ShelfCount, preset.Depth);
				if (TryUnplay(board, triples, preset.Shuffles) && IsPlayable(board))
					return true;
			}
			return false;
		}

		private bool TryUnplay(Board board, int triples, int shuffles)
		{
			var triple = 0;
			while (triple < triples || shuffles > 0)
			{
				if (IsMatchTurn(triples - triple, shuffles))
				{
					if (!TryUnmatch(board, PickKind(triple)))
						return false;
					triple++;
				}
				else
				{
					TryUnmove(board);
					shuffles--;
				}
			}
			return true;
		}

		private int PickKind(int triple) => triple < _kinds ? triple : _random.Next(_kinds);

		private bool IsMatchTurn(int matchesLeft, int shufflesLeft)
		{
			if (matchesLeft == 0)
				return false;
			return shufflesLeft == 0 || _random.Next(matchesLeft + shufflesLeft) < matchesLeft;
		}

		private bool TryUnmatch(Board board, int item)
		{
			var target = PickTripleHost(board);
			if (target == GameConst.NoIndex)
				return false;
			var source = PickShelf(board, target, item, CanGiveBack);
			if (source == GameConst.NoIndex)
				return false;
			if (!board.IsFrontEmpty(target))
				board.PushLayersBack(target);
			var freeSlot = _random.Next(GameConst.ShelfSlots);
			for (var slot = 0; slot < GameConst.ShelfSlots; slot++)
				if (slot != freeSlot)
					board.Set(target, 0, slot, item);
			GiveBack(board, source, item, target, freeSlot);
			return true;
		}

		private void TryUnmove(Board board)
		{
			var target = PickShelf(board, GameConst.NoIndex, GameConst.EmptyItem, CanTakeFrom);
			if (target == GameConst.NoIndex)
				return;
			var targetSlot = PickItemSlot(board, target);
			var item = board.Front(target, targetSlot);
			var source = PickShelf(board, target, item, CanGiveBack);
			if (source == GameConst.NoIndex)
				return;
			board.Set(target, 0, targetSlot, GameConst.EmptyItem);
			GiveBack(board, source, item, target, targetSlot);
		}

		private void GiveBack(Board board, int source, int item, int target, int targetSlot)
		{
			if (!CanPlace(board, source, item))
				board.PushLayersBack(source);
			var sourceSlot = PickFreeSlot(board, source);
			board.Set(source, 0, sourceSlot, item);
			_reversedSolution.Add(new ShelfMove { FromShelf = source, FromSlot = sourceSlot, ToShelf = target, ToSlot = targetSlot });
		}

		private int PickTripleHost(Board board)
		{
			_candidates.Clear();
			var bestRank = int.MaxValue;
			for (var shelf = 0; shelf < board.ShelfCount; shelf++)
			{
				if (!board.IsFrontEmpty(shelf) && !board.CanPushLayersBack(shelf))
					continue;
				var rank = HostRank(board, shelf);
				if (rank > bestRank)
					continue;
				if (rank < bestRank)
					_candidates.Clear();
				bestRank = rank;
				_candidates.Add(shelf);
			}
			return _candidates.Count == 0 ? GameConst.NoIndex : _candidates[_random.Next(_candidates.Count)];
		}

		private int HostRank(Board board, int shelf)
		{
			var free = board.FreeFrontSlots(shelf);
			if (free == GameConst.ShelfSlots || free == 0)
				return board.UsedLayers(shelf);
			return board.Depth + free;
		}

		private bool CanTakeFrom(Board board, int shelf, int item)
		{
			var count = GameConst.ShelfSlots - board.FreeFrontSlots(shelf);
			return count >= 2 || count == 1 && !board.HasBackLayers(shelf);
		}

		private bool CanGiveBack(Board board, int shelf, int item) => CanPush(board, shelf) || CanPlace(board, shelf, item);

		private bool CanPush(Board board, int shelf) => !board.IsFrontEmpty(shelf) && board.CanPushLayersBack(shelf);

		private bool CanPlace(Board board, int shelf, int item) =>
			board.FreeFrontSlots(shelf) > 0 && board.CountInFront(shelf, item) < GameConst.ShelfSlots - 1;

		private int PickShelf(Board board, int except, int item, Func<Board, int, int, bool> fits)
		{
			_candidates.Clear();
			for (var shelf = 0; shelf < board.ShelfCount; shelf++)
				if (shelf != except && fits(board, shelf, item))
					_candidates.Add(shelf);
			return _candidates.Count == 0 ? GameConst.NoIndex : _candidates[_random.Next(_candidates.Count)];
		}

		private int PickItemSlot(Board board, int shelf) => PickSlot(board, shelf, false);

		private int PickFreeSlot(Board board, int shelf) => PickSlot(board, shelf, true);

		private int PickSlot(Board board, int shelf, bool free)
		{
			_candidates.Clear();
			for (var slot = 0; slot < GameConst.ShelfSlots; slot++)
				if ((board.Front(shelf, slot) == GameConst.EmptyItem) == free)
					_candidates.Add(slot);
			return _candidates[_random.Next(_candidates.Count)];
		}

		private bool IsPlayable(Board board)
		{
			for (var shelf = 0; shelf < board.ShelfCount; shelf++)
			{
				var item = board.Front(shelf, 0);
				if (item != GameConst.EmptyItem && board.CountInFront(shelf, item) == GameConst.ShelfSlots)
					return false;
			}
			return board.HasMoves();
		}

		private void StoreSolution()
		{
			_solution.Clear();
			for (var i = _reversedSolution.Count - 1; i >= 0; i--)
				_solution.Add(_reversedSolution[i]);
		}

		private int FitTriples(LevelPreset preset)
		{
			var capacity = preset.ShelfCount * preset.Depth * GameConst.ShelfSlots;
			var maxTriples = (capacity - _settings.MinFreeSlots) / GameConst.ShelfSlots;
			return Math.Clamp(preset.Triples, 1, Math.Max(1, maxTriples));
		}
	}
}
