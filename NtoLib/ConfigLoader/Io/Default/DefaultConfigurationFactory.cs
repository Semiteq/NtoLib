using NtoLib.ConfigLoader.Entities;

namespace NtoLib.ConfigLoader.Io.Default;

public static class DefaultConfigurationFactory
{
	public static LoaderDto Create(
		uint shutterQuantity,
		uint sourcesQuantity,
		uint chamberHeaterQuantity,
		uint waterQuantity)
	{
		return new LoaderDto(
			CreateEmptyArray(shutterQuantity),
			CreateEmptyArray(sourcesQuantity),
			CreateEmptyArray(chamberHeaterQuantity),
			CreateEmptyArray(waterQuantity));
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
