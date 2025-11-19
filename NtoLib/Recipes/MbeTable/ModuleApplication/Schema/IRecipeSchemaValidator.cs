using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Schema;

public interface IRecipeSchemaValidator
{
    Result ValidateRow(string[] cells);
}