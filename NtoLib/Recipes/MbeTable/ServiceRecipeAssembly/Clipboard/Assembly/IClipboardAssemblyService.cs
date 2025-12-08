using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Assembly;

public interface IClipboardAssemblyService
{
	Result<IReadOnlyList<Step>> AssembleFromClipboard();
}
