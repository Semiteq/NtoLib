namespace NtoLib.TrendPensManager.Entities;

public sealed class ServiceSelectionOptions
{
	public bool AddHeaters { get; init; } = true;
	public bool AddChamberHeaters { get; init; } = true;
	public bool AddShutters { get; init; } = true;
	public bool AddGases { get; init; } = true;

	public bool AddVacuumMeters { get; init; } = true;
	public bool AddPyrometer { get; init; } = true;
	public bool AddTurbines { get; init; } = true;
	public bool AddCryo { get; init; } = true;
	public bool AddIon { get; init; } = true;
	public bool AddInterferometer { get; init; } = true;

	public bool IsAnyServiceEnabled()
	{
		return AddHeaters
			   || AddChamberHeaters
			   || AddShutters
			   || AddGases
			   || AddVacuumMeters
			   || AddPyrometer
			   || AddTurbines
			   || AddCryo
			   || AddIon
			   || AddInterferometer;
	}
}
