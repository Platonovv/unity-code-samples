using System;
using Settings;

namespace Flow
{
	public sealed class FlowBot
	{
		private const int StuckLaps = 2;

		private readonly StepResult _result = new();
		private readonly float _blunderChance;
		private readonly Random _random;
		private ExposureMap _exposure;
		private int[] _scores;

		public FlowBot(float blunderChance = 0f, int seed = 0)
		{
			_blunderChance = blunderChance;
			_random = new Random(seed);
		}

		public BotResult Play(LevelLayout layout, LevelSettings settings)
		{
			var board = layout.CreateBoard(settings);
			_exposure = new ExposureMap(board.Width, board.Height);
			_scores = new int[board.Palette.Length];
			var peakSlots = 0;
			var idleSteps = 0;
			var stuckSteps = board.TrackLength * StuckLaps;
			while (true)
			{
				LaunchUseful(board);
				board.Step(_result);
				if (board.IsCleared || _result.IsOverflow || idleSteps > stuckSteps)
					return new BotResult(board.IsCleared, peakSlots);
				idleSteps = _result.Hits.Count == 0 ? idleSteps + 1 : 0;
				peakSlots = Math.Max(peakSlots, CountParked(board));
			}
		}

		private void LaunchUseful(FlowBoard board)
		{
			while (!board.IsTrackFull)
			{
				board.CountExposed(_exposure, _scores);
				SubtractTrackDemand(board);
				if (!TryLaunchBest(board, board.Track.Count == 0))
					return;
			}
		}

		private void SubtractTrackDemand(FlowBoard board)
		{
			foreach (var shooter in board.Track)
				_scores[shooter.Color] -= shooter.Charges;
		}

		private bool TryLaunchBest(FlowBoard board, bool mustLaunch)
		{
			var bestSource = ShooterSource.Slot;
			var bestIndex = -1;
			var bestScore = int.MinValue;
			for (var i = 0; i < board.SlotCount; i++)
				Consider(board.GetSlot(i), ShooterSource.Slot, i, ref bestSource, ref bestIndex, ref bestScore);
			for (var i = 0; i < board.ColumnCount; i++)
				Consider(board.GetQueued(i, 0), ShooterSource.Column, i, ref bestSource, ref bestIndex, ref bestScore);
			if (_random.NextDouble() < _blunderChance)
				return LaunchAny(board);
			if (bestIndex < 0 || (bestScore <= 0 && !mustLaunch))
				return false;
			board.Launch(bestSource, bestIndex);
			return true;
		}

		private bool LaunchAny(FlowBoard board)
		{
			var candidates = board.SlotCount + board.ColumnCount;
			var start = _random.Next(candidates);
			for (var i = 0; i < candidates; i++)
			{
				var candidate = (start + i) % candidates;
				var source = candidate < board.SlotCount ? ShooterSource.Slot : ShooterSource.Column;
				var index = source == ShooterSource.Slot ? candidate : candidate - board.SlotCount;
				if (!board.CanLaunch(source, index))
					continue;
				board.Launch(source, index);
				return true;
			}
			return false;
		}

		private void Consider(Shooter shooter, ShooterSource source, int index, ref ShooterSource bestSource, ref int bestIndex, ref int bestScore)
		{
			if (shooter == null || shooter.IsLocked || _scores[shooter.Color] <= bestScore)
				return;
			bestSource = source;
			bestIndex = index;
			bestScore = _scores[shooter.Color];
		}

		private int CountParked(FlowBoard board)
		{
			var count = 0;
			for (var i = 0; i < board.SlotCount; i++)
				if (board.GetSlot(i) != null)
					count++;
			return count;
		}
	}
}
