using NtoLib.MbeTable.ModuleCore.Entities;

namespace NtoLib.MbeTable.ModuleCore.Analyzer;

public interface IStructureValidator
{
	StructureResult Validate(Recipe recipe);
}
