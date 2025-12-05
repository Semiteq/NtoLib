using System.Collections.Generic;

using FluentResults;

using NtoLib.MbeTable.ModuleCore.Entities;

namespace NtoLib.MbeTable.ServiceRecipeAssembly.Clipboard.Parsing;

public interface IClipboardParser
{
	Result<IReadOnlyList<PortableStepDto>> Parse(IReadOnlyList<string[]> rows);
}
