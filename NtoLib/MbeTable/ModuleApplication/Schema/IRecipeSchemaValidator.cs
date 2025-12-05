using FluentResults;

namespace NtoLib.MbeTable.ModuleApplication.Schema;

public interface IRecipeSchemaValidator
{
	Result ValidateRow(string[] cells);
}
