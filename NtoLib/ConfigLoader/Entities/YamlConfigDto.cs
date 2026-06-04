using System.Collections.Generic;

namespace NtoLib.ConfigLoader.Entities;

public sealed class YamlConfigDto
{
	public YamlConfigDto()
	{
		Shutters = new Dictionary<string, string>();
		Sources = new Dictionary<string, string>();
		ChamberHeaters = new Dictionary<string, string>();
		Waters = new Dictionary<string, string>();
		Gases = new Dictionary<string, string>();
	}

	public YamlConfigDto(
		Dictionary<string, string> shutters,
		Dictionary<string, string> sources,
		Dictionary<string, string> chamberHeaters,
		Dictionary<string, string> waters,
		Dictionary<string, string> gases)
	{
		Shutters = shutters;
		Sources = sources;
		ChamberHeaters = chamberHeaters;
		Waters = waters;
		Gases = gases;
	}

	public Dictionary<string, string> Shutters { get; set; }
	public Dictionary<string, string> Sources { get; set; }
	public Dictionary<string, string> ChamberHeaters { get; set; }
	public Dictionary<string, string> Waters { get; set; }
	public Dictionary<string, string> Gases { get; set; }
}
