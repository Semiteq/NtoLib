using NtoLib.ConfigLoader.Entities;

namespace NtoLib.ConfigLoader.Io.Default;

public static class DefaultConfigurationFactory
{
	public static LoaderDto Create(ConfigLoaderGroups groups)
	{
		return new LoaderDto(
			CreateEmptyArray(groups.Shutters.Capacity),
			CreateEmptyArray(groups.Sources.Capacity),
			CreateEmptyArray(groups.ChamberHeaters.Capacity),
			CreateEmptyArray(groups.Water.Capacity),
			CreateEmptyArray(groups.Gases.Capacity));
	}

	private static string[] CreateEmptyArray(uint size)
	{
		var array = new string[size];

		for (var i = 0; i < size; i++)
		{
			array[i] = string.Empty;
		}

		return array;
	}
}
