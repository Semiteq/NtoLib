using System;

namespace NtoLib.TrendPensManager.Entities;

public sealed class ServiceSelectionOptions
{
	public bool AddHeaters { get; init; } = true;
	public bool AddChamberHeaters { get; init; } = true;
	public bool AddShutters { get; init; } = true;

	public bool AddVacuumMeters { get; init; } = true;
	public bool AddPyrometer { get; init; } = true;
	public bool AddTurbines { get; init; } = true;
	public bool AddCryo { get; init; } = true;
	public bool AddIon { get; init; } = true;
	public bool AddInterferometer { get; init; } = true;

	public static ServiceSelectionOptions CreateAllEnabled()
	{
		return new ServiceSelectionOptions
		{
			AddHeaters = true,
			AddChamberHeaters = true,
			AddShutters = true,
			AddVacuumMeters = true,
			AddPyrometer = true,
			AddTurbines = true,
			AddCryo = true,
			AddIon = true,
			AddInterferometer = true
		};
	}

	public bool IsAnyServiceEnabled()
	{
		return AddHeaters
			   || AddChamberHeaters
			   || AddShutters
			   || AddVacuumMeters
			   || AddPyrometer
			   || AddTurbines
			   || AddCryo
			   || AddIon
			   || AddInterferometer;
	}
}
