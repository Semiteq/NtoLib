using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Analyzer;

public interface IStructureValidator
{
	StructureResult Validate(Recipe recipe);
}
