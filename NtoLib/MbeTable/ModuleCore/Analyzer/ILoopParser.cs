using NtoLib.MbeTable.ModuleCore.Entities;

namespace NtoLib.MbeTable.ModuleCore.Analyzer;

public interface ILoopParser
{
	LoopParseResult Parse(Recipe recipe);
}
