using System;
using System.Collections.Generic;
using Settings;
using UnityEngine;
using Random = System.Random;

namespace Flow
{
	public sealed class LevelGenerator
	{
		private const int AttemptSeedStep = 1000;
		private const int IceSearchFactor = 4;
		private const int CrateSearchFactor = 4;
		private const float ChargeStep = 1.25f;
		private const float MinChargeScale = 0.4f;
		private const float MaxChargeScale = 2.5f;
		private const int NoColor = -1;
		private const int Percent = 100;

		private readonly LevelSettings _settings;
		private readonly FlowBot _bot = new();
		private readonly List<Color32> _palette = new();
		private readonly List<int> _colorCounts = new();
		private readonly List<ShooterPlan> _plan = new();
		private readonly List<int> _bigAnchors = new();
		private ExposureMap _exposure;
		private int[] _exposedCounts;
		private int[] _left;
		private float _baseMinCharges;
		private float _baseMaxCharges;
		private int _minCharges;
		private int _maxCharges;
		private int _attempts;

		public LevelGenerator(LevelSettings settings) => _settings = settings;

		public GenerationReport LastReport { get; private set; }

		public LevelLayout Generate(int level) => Generate(level, _settings.GetPicture(level));

		public LevelLayout Generate(int level, Texture2D picture)
		{
			var preset = _settings.GetPreset(level);
			var cells = ReadCells(picture);
			var palette = _palette.ToArray();
			PrepareBuffers(picture);
			ScaleCharges(preset);
			_attempts = _settings.GetAttempts(level);
			LevelLayout best = null;
			var bestMiss = int.MaxValue;
			var chargeScale = 1f;
			for (var attempt = 0; attempt < _attempts && bestMiss > 0; attempt++)
			{
				var seed = level * AttemptSeedStep + attempt;
				SetCharges(chargeScale);
				var layout = Compose(picture, cells, palette, preset, GetIcePercent(preset, attempt), new Random(seed));
				var result = _bot.Play(layout, _settings);
				best ??= layout;
				if (!result.IsSolved)
				{
					chargeScale = Math.Max(MinChargeScale, chargeScale / ChargeStep);
					continue;
				}
				var carelessWin = MeasureCarelessWin(layout, seed);
				chargeScale = RetuneCharges(chargeScale, preset, carelessWin);
				var miss = MissDifficulty(preset, carelessWin);
				if (miss >= bestMiss)
					continue;
				best = layout;
				bestMiss = miss;
				LastReport = new GenerationReport(attempt + 1, carelessWin, result);
			}
			if (bestMiss == int.MaxValue)
				ReportUnsolved(level, picture);
			return best;
		}

		private void ReportUnsolved(int level, Texture2D picture)
		{
			LastReport = new GenerationReport(_attempts, 0, default);
			Debug.LogError($"Level {level} ({picture.name}) is not solved by the bot in {_attempts} attempts");
		}

		private int[] ReadCells(Texture2D picture)
		{
			_palette.Clear();
			_colorCounts.Clear();
			var pixels = picture.GetPixels32();
			var cells = new int[pixels.Length];
			for (var i = 0; i < pixels.Length; i++)
				cells[i] = pixels[i].a < _settings.AlphaThreshold ? FlowBoard.Empty : CountColor(pixels[i]);
			return cells;
		}

		private int CountColor(Color32 color)
		{
			color.a = byte.MaxValue;
			var index = _palette.IndexOf(color);
			if (index < 0)
			{
				index = _palette.Count;
				_palette.Add(color);
				_colorCounts.Add(0);
			}
			_colorCounts[index]++;
			return index;
		}

		private void PrepareBuffers(Texture2D picture)
		{
			_exposure = new ExposureMap(picture.width, picture.height);
			_exposedCounts = new int[_palette.Count];
			_left = new int[_palette.Count];
		}

		private void ScaleCharges(LevelPreset preset)
		{
			var filled = 0;
			foreach (var count in _colorCounts)
				filled += count;
			var scale = Math.Max(1f, Mathf.Sqrt((float)filled / _settings.ChargeReferenceCells));
			_baseMinCharges = preset.MinCharges * scale;
			_baseMaxCharges = preset.MaxCharges * scale;
		}

		private void SetCharges(float chargeScale)
		{
			_minCharges = Math.Max(1, Mathf.RoundToInt(_baseMinCharges * chargeScale));
			_maxCharges = Math.Max(_minCharges, Mathf.RoundToInt(_baseMaxCharges * chargeScale));
		}

		private float RetuneCharges(float chargeScale, LevelPreset preset, int carelessWin)
		{
			if (carelessWin > preset.CarelessWinMax)
				return Math.Min(MaxChargeScale, chargeScale * ChargeStep);
			if (carelessWin < preset.CarelessWinMin)
				return Math.Max(MinChargeScale, chargeScale / ChargeStep);
			return chargeScale;
		}

		private int MeasureCarelessWin(LevelLayout layout, int seed)
		{
			var wins = 0;
			for (var run = 0; run < _settings.CarelessRuns; run++)
				if (new FlowBot(_settings.CarelessBlunder, seed + run).Play(layout, _settings).IsSolved)
					wins++;
			return Percent * wins / _settings.CarelessRuns;
		}

		private int MissDifficulty(LevelPreset preset, int carelessWin)
		{
			if (carelessWin < preset.CarelessWinMin)
				return preset.CarelessWinMin - carelessWin;
			return Math.Max(0, carelessWin - preset.CarelessWinMax);
		}

		private int GetIcePercent(LevelPreset preset, int attempt) =>
			preset.IcePercent * (_attempts - attempt) / _attempts;

		private LevelLayout Compose(Texture2D picture, int[] cells, Color32[] palette, LevelPreset preset, int icePercent, Random random)
		{
			var bigs = PlaceBigs(cells, picture.width, picture.height, preset.Bigs, random);
			var ice = PlaceIce(cells, bigs, picture.width, icePercent, random);
			PlanShooters(cells, ice, bigs, random);
			Disorder(preset.Mess, random);
			var columns = DealColumns(preset.Columns);
			HideSurprises(columns, preset.SurprisePercent, random);
			LockCrates(columns, preset.Crates, random);
			return new LevelLayout(picture.width, picture.height, cells, ice, bigs, palette, columns);
		}

		private BigPixels PlaceBigs(int[] cells, int width, int height, int count, Random random)
		{
			var bigs = new BigPixels(width, cells.Length);
			if (count == 0)
				return bigs;
			CollectSquares(cells, width, height, bigs);
			while (bigs.Count < count && _bigAnchors.Count > 0)
			{
				var anchor = TakeRandomSquare(random);
				if (bigs.IsFree(anchor))
					bigs.Add(anchor);
			}
			return bigs;
		}

		private void CollectSquares(int[] cells, int width, int height, BigPixels bigs)
		{
			_bigAnchors.Clear();
			for (var y = 0; y < height - 1; y++)
			for (var x = 0; x < width - 1; x++)
				if (IsSquare(cells, y * width + x, bigs))
					_bigAnchors.Add(y * width + x);
		}

		private bool IsSquare(int[] cells, int anchor, BigPixels bigs)
		{
			if (cells[anchor] == FlowBoard.Empty)
				return false;
			for (var corner = 1; corner < BigPixels.CellCount; corner++)
				if (cells[bigs.GetCell(anchor, corner)] != cells[anchor])
					return false;
			return true;
		}

		private int TakeRandomSquare(Random random)
		{
			var pick = random.Next(_bigAnchors.Count);
			var anchor = _bigAnchors[pick];
			_bigAnchors[pick] = _bigAnchors[^1];
			_bigAnchors.RemoveAt(_bigAnchors.Count - 1);
			return anchor;
		}

		private int[] PlaceIce(int[] cells, BigPixels bigs, int width, int percent, Random random)
		{
			var ice = new int[cells.Length];
			if (percent == 0)
				return ice;
			var target = CountFilled(cells) * percent / Percent;
			var iced = 0;
			for (var attempt = 0; iced < target && attempt < cells.Length * IceSearchFactor; attempt++)
			{
				var index = random.Next(cells.Length);
				if (cells[index] != FlowBoard.Empty && ice[index] == 0 && !bigs.IsBig(index))
					iced += FreezeCluster(cells, ice, bigs, index, width, target - iced);
			}
			return ice;
		}

		private int FreezeCluster(int[] cells, int[] ice, BigPixels bigs, int center, int width, int limit)
		{
			var frozen = Freeze(cells, ice, bigs, center, limit);
			var x = center % width;
			if (x > 0)
				frozen += Freeze(cells, ice, bigs, center - 1, limit - frozen);
			if (x < width - 1)
				frozen += Freeze(cells, ice, bigs, center + 1, limit - frozen);
			frozen += Freeze(cells, ice, bigs, center - width, limit - frozen);
			frozen += Freeze(cells, ice, bigs, center + width, limit - frozen);
			return frozen;
		}

		private int Freeze(int[] cells, int[] ice, BigPixels bigs, int index, int limit)
		{
			if (limit <= 0 || index < 0 || index >= cells.Length || cells[index] == FlowBoard.Empty || ice[index] > 0 || bigs.IsBig(index))
				return 0;
			ice[index] = 1;
			return 1;
		}

		private int CountFilled(int[] cells)
		{
			var count = 0;
			foreach (var cell in cells)
				if (cell != FlowBoard.Empty)
					count++;
			return count;
		}

		private void PlanShooters(int[] source, int[] sourceIce, BigPixels sourceBigs, Random random)
		{
			_plan.Clear();
			_colorCounts.CopyTo(_left);
			AddIceDemand(source, sourceIce);
			var cells = (int[])source.Clone();
			var ice = (int[])sourceIce.Clone();
			var bigs = sourceBigs.Clone();
			var hitsLeft = SumLeft();
			while (hitsLeft > 0)
			{
				_exposure.Update(cells);
				_exposure.CountByColor(cells, ice, bigs, _exposedCounts);
				var color = PickExposedColor(random);
				if (color == NoColor)
					return;
				var spent = Peel(cells, ice, bigs, color, PickCharges(_left[color], random));
				if (spent == 0)
					return;
				_left[color] -= spent;
				hitsLeft -= spent;
				_plan.Add(new ShooterPlan(color, spent));
			}
		}

		private void AddIceDemand(int[] cells, int[] ice)
		{
			for (var i = 0; i < cells.Length; i++)
				if (ice[i] > 0)
					_left[cells[i]] += ice[i];
		}

		private int SumLeft()
		{
			var sum = 0;
			foreach (var left in _left)
				sum += left;
			return sum;
		}

		private int PickExposedColor(Random random)
		{
			var total = 0;
			foreach (var count in _exposedCounts)
				total += count;
			if (total == 0)
				return NoColor;
			var roll = random.Next(total);
			for (var color = 0; color < _exposedCounts.Length; color++)
			{
				roll -= _exposedCounts[color];
				if (roll < 0)
					return color;
			}
			return _exposedCounts.Length - 1;
		}

		private int PickCharges(int left, Random random)
		{
			var charges = Math.Min(left, random.Next(_minCharges, _maxCharges + 1));
			return left - charges < _minCharges ? left : charges;
		}

		private int Peel(int[] cells, int[] ice, BigPixels bigs, int color, int charges)
		{
			var spent = 0;
			while (spent < charges)
			{
				var spentBefore = spent;
				for (var i = 0; i < cells.Length && spent < charges; i++)
				{
					if (cells[i] != color || !_exposure.IsExposed(i))
						continue;
					PeelCell(cells, ice, bigs, i);
					spent++;
				}
				if (spent == spentBefore)
					break;
				_exposure.Update(cells);
			}
			return spent;
		}

		private void PeelCell(int[] cells, int[] ice, BigPixels bigs, int index)
		{
			if (ice[index] > 0)
				ice[index]--;
			else if (!bigs.IsBig(index))
				cells[index] = FlowBoard.Empty;
			else if (bigs.Hit(index) == 0)
				bigs.Pop(cells, index);
		}

		private void Disorder(int mess, Random random)
		{
			if (mess == 0)
				return;
			var plans = _plan.ToArray();
			var keys = new float[plans.Length];
			for (var i = 0; i < keys.Length; i++)
				keys[i] = i + (float)random.NextDouble() * (mess + 1);
			Array.Sort(keys, plans);
			_plan.Clear();
			_plan.AddRange(plans);
		}

		private List<ShooterPlan>[] DealColumns(int count)
		{
			var columns = new List<ShooterPlan>[count];
			for (var i = 0; i < count; i++)
				columns[i] = new List<ShooterPlan>();
			for (var i = 0; i < _plan.Count; i++)
				columns[i % count].Add(_plan[i]);
			return columns;
		}

		private void HideSurprises(List<ShooterPlan>[] columns, int percent, Random random)
		{
			if (percent == 0)
				return;
			foreach (var column in columns)
				for (var depth = 1; depth < column.Count; depth++)
					if (random.Next(Percent) < percent)
						column[depth] = column[depth].Hide();
		}

		private void LockCrates(List<ShooterPlan>[] columns, int count, Random random)
		{
			var locked = 0;
			for (var attempt = 0; locked < count && attempt < count * CrateSearchFactor; attempt++)
				if (TryLockCrate(columns[random.Next(columns.Length)], random))
					locked++;
		}

		private bool TryLockCrate(List<ShooterPlan> column, Random random)
		{
			if (column.Count == 0)
				return false;
			var depth = random.Next(Math.Min(_settings.CrateMaxDepth, column.Count));
			if (column[depth].IsHidden || column[depth].Locks > 0)
				return false;
			var locks = random.Next(_settings.CrateLocks.x, _settings.CrateLocks.y + 1);
			column[depth] = column[depth].Lock(locks + depth);
			return true;
		}
	}
}
