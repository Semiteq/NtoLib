using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Parsing;

public interface IClipboardParser
{
    Result<IReadOnlyList<PortableStepDto>> Parse(IReadOnlyList<string[]> rows);
}