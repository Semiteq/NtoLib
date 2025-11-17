using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Analyzer;

public interface ILoopParser
{
    LoopParseResult Parse(Recipe recipe);
}