using System.Drawing;

using NtoLib.Devices.Helpers;
using NtoLib.Devices.Render.Common;
using NtoLib.Devices.Valves;

namespace NtoLib.Devices.Render.Valves;

internal abstract class ValveBaseRenderer : BaseRenderer
{
	/// <summary>Экземпляр ValveControl, к которому привязан данный Renderer</summary>
	protected ValveControl Control { get; private set; }

	protected ValveBaseRenderer(ValveControl valveControl)
	{
		Control = valveControl;
	}

	/// <summary>
	/// Возвращает цвета первого и второго треугольника в зависимости от статуса
	/// </summary>
	protected static Color[] GetValveColors(Status status, bool isLight)
	{
		var colors = new Color[2];

		if (!status.ConnectionOk)
		{
			colors[0] = Colors.NoData;
			colors[1] = Colors.NoData;
		}
		else if (status.Opened && !status.Collision && !status.UnknownState)
		{
			colors[0] = Colors.Opened;
			colors[1] = Colors.Opened;
		}
		else if ((status.Closed && !status.Collision && !status.UnknownState) || status.OpenedSmoothly)
		{
			colors[0] = Colors.Closed;
			colors[1] = Colors.Closed;
		}
		else
		{
			if (isLight)
			{
				colors[0] = Colors.Opened;
				colors[1] = Colors.Closed;
			}
			else
			{
				colors[0] = Colors.Closed;
				colors[1] = Colors.Opened;
			}
		}

		return colors;
	}

	/// <summary>
	/// Возвращает истину, если должна быть показана блокировка
	/// </summary>
	protected bool IsBlocked(Status status)
	{
		if (status.Opened && status.BlockClosing)
			return true;
		if (status.Closed && (status.BlockOpening || status.ForceClose))
			return true;
		if (status.OpenedSmoothly && (status.BlockClosing || status.BlockOpening))
			return true;

		return false;
	}
}
