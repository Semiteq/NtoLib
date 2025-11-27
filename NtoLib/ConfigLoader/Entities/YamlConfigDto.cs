using System.Collections.Generic;

namespace NtoLib.ConfigLoader.Entities;

public sealed class YamlConfigDto
{
	public Dictionary<string, string> Shutter { get; set; }
	public Dictionary<string, string> Sources { get; set; }
	public Dictionary<string, string> ChamberHeater { get; set; }
	public Dictionary<string, string> Water { get; set; }

	public YamlConfigDto()
	{
		Shutter = new Dictionary<string, string>();
		Sources = new Dictionary<string, string>();
		ChamberHeater = new Dictionary<string, string>();
		Water = new Dictionary<string, string>();
	}

	public YamlConfigDto(
		Dictionary<string, string> shutter,
		Dictionary<string, string> sources,
		Dictionary<string, string> chamberHeater,
		Dictionary<string, string> water)
	{
		Shutter = shutter ?? new Dictionary<string, string>();
		Sources = sources ?? new Dictionary<string, string>();
		ChamberHeater = chamberHeater ?? new Dictionary<string, string>();
		Water = water ?? new Dictionary<string, string>();
	}
}
