using System.Collections.Generic;

using NtoLib.OpcTreeManager.Entities;

namespace NtoLib.OpcTreeManager.Config;

public sealed class OpcConfig
{
	public Dictionary<string, List<NodeSpec>> Projects { get; set; } = new();
}
