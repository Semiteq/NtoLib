using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Transform;

public interface IClipboardStepsTransformer
{
    Result<IReadOnlyList<Step>> Transform(IReadOnlyList<PortableStepDto> dtos);
}