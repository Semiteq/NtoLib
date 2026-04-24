using System.Collections.Generic;

namespace NtoLib.OpcTreeManager.Config;

public sealed class OpcConfig
{
	public Dictionary<string, List<string>> Projects { get; set; } = new();
}
