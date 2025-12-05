using System.Collections.Generic;

using FluentResults;

using NtoLib.MbeTable.ModuleCore.Entities;

namespace NtoLib.MbeTable.ServiceRecipeAssembly.Clipboard.Transform;

public interface IClipboardStepsTransformer
{
	Result<IReadOnlyList<Step>> Transform(IReadOnlyList<PortableStepDto> dtos);
}
