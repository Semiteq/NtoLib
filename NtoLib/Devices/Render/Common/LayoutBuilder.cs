using System;
using System.Drawing;
using System.Windows.Forms;

using FB.VisualFB;

using NtoLib.Devices.Pumps;
using NtoLib.Devices.Valves;

namespace NtoLib.Devices.Render.Common;

public static class LayoutBuilder
{
	public static DeviceLayout BuildLayout(VisualControlBase control, bool noButtons)
	{
		Size elementSize;
		Size tableSize;

		Point elementLocation;
		Point tableLocation;

		if (control is ValveControl valveControl)
		{
			(elementSize, tableSize) = CalculateValveSize(valveControl, noButtons);

			Orientation buttonOrientation;
			if (IsHorizontal(valveControl.Orientation))
			{
				if (valveControl.ButtonOrientation == ButtonOrientation.RightBottom)
				{
					buttonOrientation = Orientation.Bottom;
				}
				else
				{
					buttonOrientation = Orientation.Top;
				}
			}
			else
			{
				if (valveControl.ButtonOrientation == ButtonOrientation.RightBottom)
				{
					buttonOrientation = Orientation.Right;
				}
				else
				{
					buttonOrientation = Orientation.Left;
				}
			}

			(elementLocation, tableLocation) = CalculatePositions((elementSize, tableSize), buttonOrientation);
		}
		else if (control is PumpControl pumpControl)
		{
			(elementSize, tableSize) = CalculatePumpSize(pumpControl, noButtons);

			Orientation buttonOrientation;
			if (pumpControl.IsHorizontal())
			{
				buttonOrientation = pumpControl.ButtonOrientation == ButtonOrientation.RightBottom
					? Orientation.Right
					: Orientation.Left;
			}
			else
			{
				buttonOrientation = pumpControl.ButtonOrientation == ButtonOrientation.RightBottom
					? Orientation.Bottom
					: Orientation.Top;
			}

			(elementLocation, tableLocation) = CalculatePositions((elementSize, tableSize), buttonOrientation);
		}
		else
		{
			throw new NotImplementedException();
		}

		var layout = new DeviceLayout
		{
			DeviceRectangle = new Rectangle(elementLocation, elementSize),
			ButtonTableRectangle = new Rectangle(tableLocation, tableSize)
		};

		return layout;
	}

	public static void RebuildTable(TableLayoutPanel table, Orientation orientation, Button[] buttons)
	{
		if (orientation is Orientation.Top or Orientation.Bottom)
		{
			foreach (var t in buttons)
			{
				table.SetRow(t, 0);
			}

			table.RowCount = 1;
			table.ColumnCount = buttons.Length;

			for (var i = 0; i < buttons.Length; i++)
			{
				table.ColumnStyles[i].SizeType = SizeType.Percent;
				table.ColumnStyles[i].Width = 1f / buttons.Length;
				table.SetColumn(buttons[i], i);
			}
		}
		else
		{
			foreach (var t in buttons)
			{
				table.SetColumn(t, 0);
			}

			table.ColumnCount = 1;
			table.RowCount = buttons.Length;

			for (var i = 0; i < buttons.Length; i++)
			{
				table.RowStyles[i].SizeType = SizeType.Percent;
				table.RowStyles[i].Height = 1f / buttons.Length;
				table.SetRow(buttons[i], i);
			}
		}
	}

	private static (Size valveSize, Size tableSize) CalculateValveSize(ValveControl valve, bool noButtons)
	{
		var bounds = valve.Bounds;

		var valveSize = new Size();
		var tableSize = new Size();

		if (noButtons)
		{
			valveSize = bounds.Size;
			tableSize = new Size();

			return (valveSize, tableSize);
		}

		float hwRatio;

		if (valve.IsSlideGate)
		{
			hwRatio = 3f / 4f;
		}
		else
		{
			hwRatio = 2f / 3f;
		}

		if (IsHorizontal(valve.Orientation))
		{
			var heightFromWidth = (int)(hwRatio * bounds.Width);
			if (heightFromWidth < bounds.Height)
			{
				valveSize.Height = heightFromWidth;
				valveSize.Width = bounds.Width;
			}
			else
			{
				valveSize.Height = bounds.Height;
				valveSize.Width = (int)((1 / hwRatio) * bounds.Height);
			}

			tableSize.Height = bounds.Height - valveSize.Height - 1;
			tableSize.Width = valveSize.Width;
		}
		else
		{
			var widthFromHeight = (int)(hwRatio * bounds.Height);
			if (widthFromHeight < bounds.Width)
			{
				valveSize.Height = bounds.Height;
				valveSize.Width = widthFromHeight;
			}
			else
			{
				valveSize.Height = (int)((1 / hwRatio) * bounds.Width);
				valveSize.Width = bounds.Width;
			}

			tableSize.Height = valveSize.Height;
			tableSize.Width = bounds.Width - valveSize.Width - 1;
		}

		return (valveSize, tableSize);
	}

	private static (Size pumpSize, Size tableSize) CalculatePumpSize(PumpControl pump, bool noButtons)
	{
		var bounds = pump.Bounds;

		var tableSize = new Size();

		var pumpHeight = Math.Min(bounds.Height, bounds.Width);
		var pumpSize = new Size(pumpHeight, pumpHeight);

		if (noButtons)
		{
			tableSize = new Size();

			return (pumpSize, tableSize);
		}

		if (pumpHeight == bounds.Width)
		{
			tableSize.Height = bounds.Height - pumpSize.Height - 1;
			tableSize.Width = pumpSize.Width;
		}
		else
		{
			tableSize.Height = pumpSize.Height;
			tableSize.Width = bounds.Width - pumpSize.Width - 1;
		}

		return (pumpSize, tableSize);
	}

	private static (Point ElementLocation, Point TableLocation) CalculatePositions((Size Element, Size Table) size,
		Orientation tableOrientation)
	{
		var valveLocation = new Point();
		var tableLocation = new Point();

		switch (tableOrientation)
		{
			case Orientation.Bottom:
			{
				valveLocation.X = 0;
				tableLocation.X = 0;

				valveLocation.Y = 0;
				tableLocation.Y = size.Element.Height;

				break;
			}
			case Orientation.Right:
			{
				valveLocation.Y = 0;
				tableLocation.Y = 0;

				valveLocation.X = 0;
				tableLocation.X = size.Element.Width;

				break;
			}
			case Orientation.Top:
			{
				valveLocation.X = 0;
				tableLocation.X = 0;

				valveLocation.Y = size.Table.Height;
				tableLocation.Y = 0;

				break;
			}
			case Orientation.Left:
			{
				valveLocation.Y = 0;
				tableLocation.Y = 0;

				valveLocation.X = size.Table.Width;
				tableLocation.X = 0;

				break;
			}
		}

		return (valveLocation, tableLocation);
	}

	private static bool IsHorizontal(Orientation orientation)
	{
		return orientation is Orientation.Right or Orientation.Left;
	}
}
