namespace NtoLib.ConfigLoader.Entities;

public sealed record LoaderDto(
	string[] Shutters,
	string[] Sources,
	string[] ChamberHeaters,
	string[] WaterChannels
);
