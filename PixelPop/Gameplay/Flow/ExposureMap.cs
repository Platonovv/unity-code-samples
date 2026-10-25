using System;

namespace Flow
{
	public sealed class ExposureMap
	{
		private readonly int _width;
		private readonly int _height;
		private readonly bool[] _exposed;

		public ExposureMap(int width, int height)
		{
			_width = width;
			_height = height;
			_exposed = new bool[width * height];
		}

		public bool IsExposed(int index) => _exposed[index];

		public void Update(int[] cells)
		{
			Array.Clear(_exposed, 0, _exposed.Length);
			for (var x = 0; x < _width; x++)
			{
				MarkFirst(cells, x, 0, 0, 1);
				MarkFirst(cells, x, _height - 1, 0, -1);
			}
			for (var y = 0; y < _height; y++)
			{
				MarkFirst(cells, 0, y, 1, 0);
				MarkFirst(cells, _width - 1, y, -1, 0);
			}
		}

		public void CountByColor(int[] cells, int[] ice, BigPixels bigs, int[] counts)
		{
			Array.Clear(counts, 0, counts.Length);
			for (var i = 0; i < _exposed.Length; i++)
				if (_exposed[i])
					counts[cells[i]] += GetHitsLeft(i, ice, bigs);
		}

		private int GetHitsLeft(int index, int[] ice, BigPixels bigs)
		{
			if (!bigs.IsBig(index))
				return 1 + ice[index];
			return IsFirstExposed(index, bigs) ? bigs.GetHits(index) : 0;
		}

		private bool IsFirstExposed(int index, BigPixels bigs)
		{
			var anchor = bigs.GetAnchor(index);
			for (var corner = 0; corner < BigPixels.CellCount; corner++)
			{
				var cell = bigs.GetCell(anchor, corner);
				if (cell == index)
					return true;
				if (_exposed[cell])
					return false;
			}
			return true;
		}

		private void MarkFirst(int[] cells, int x, int y, int directionX, int directionY)
		{
			while (x >= 0 && x < _width && y >= 0 && y < _height)
			{
				var index = y * _width + x;
				if (cells[index] != FlowBoard.Empty)
				{
					_exposed[index] = true;
					return;
				}
				x += directionX;
				y += directionY;
			}
		}
	}
}
