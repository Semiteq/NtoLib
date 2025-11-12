using System;
using NtoLib.Recipes.MbeTable.ModuleCore.Attributes;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Recipes;

public interface IValidationSnapshotProvider
{
    ValidationSnapshot GetSnapshot();
    event Action<ValidationSnapshot>? SnapshotChanged;
}