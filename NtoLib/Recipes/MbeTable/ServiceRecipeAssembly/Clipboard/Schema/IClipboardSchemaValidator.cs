using FluentResults;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Schema;

public interface IClipboardSchemaValidator
{
    Result ValidateRow(int rowIndex, string[] cells);
}