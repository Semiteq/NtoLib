namespace NtoLib.ConfigLoader.Entities;

public sealed record ConfigLoaderGroup(
	string Name,
	string YamlSection,
	uint Capacity,
	int InBaseId,
	int OutBaseId);

public sealed record ConfigLoaderGroups(
	ConfigLoaderGroup Shutters,
	ConfigLoaderGroup Sources,
	ConfigLoaderGroup ChamberHeaters,
	ConfigLoaderGroup Gases,
	ConfigLoaderGroup Water)
{
	public static ConfigLoaderGroups Default { get; } = new(
		new ConfigLoaderGroup("Shutters", "Shutters", 16, 1000, 2000),
		new ConfigLoaderGroup("Sources", "Sources", 32, 3000, 4000),
		new ConfigLoaderGroup("ChamberHeaters", "ChamberHeaters", 16, 5000, 6000),
		new ConfigLoaderGroup("Gases", "Gases", 16, 9000, 10000),
		new ConfigLoaderGroup("Waters", "Waters", 16, 7000, 8000));
}
