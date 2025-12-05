using FluentResults;

namespace NtoLib.MbeTable.ServiceRecipeAssembly.Clipboard.Schema;

public interface IClipboardSchemaValidator
{
	Result ValidateRow(int rowIndex, string[] cells);
}
