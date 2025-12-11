using System.Collections.Generic;

namespace NtoLib.ConfigLoader.Entities;

public sealed class YamlConfigDto
{
	public Dictionary<string, string> Shutters { get; set; }
	public Dictionary<string, string> Sources { get; set; }
	public Dictionary<string, string> ChamberHeaters { get; set; }
	public Dictionary<string, string> Waters { get; set; }
	public Dictionary<string, string> Gases { get; set; }

	public YamlConfigDto()
	{
		Shutters = new Dictionary<string, string>();
		Sources = new Dictionary<string, string>();
		ChamberHeaters = new Dictionary<string, string>();
		Waters = new Dictionary<string, string>();
		Gases = new Dictionary<string, string>();
	}

	public YamlConfigDto(
		Dictionary<string, string> shutter,
		Dictionary<string, string> sources,
		Dictionary<string, string> chamberHeater,
		Dictionary<string, string> water,
		Dictionary<string, string> gases)
	{
		Shutters = shutter;
		Sources = sources;
		ChamberHeaters = chamberHeater;
		Waters = water;
		Gases = gases;
	}
}
