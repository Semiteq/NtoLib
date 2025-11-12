using System;
using NtoLib.Recipes.MbeTable.ModuleCore.Attributes;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Recipes;

public sealed class ValidationSnapshotProvider : IValidationSnapshotProvider
{
    private readonly IRecipeAttributesService _attributes;

    public ValidationSnapshotProvider(IRecipeAttributesService attributes)
    {
        _attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
        _attributes.ValidationSnapshotChanged += OnSnapshotChanged;
    }

    public ValidationSnapshot GetSnapshot()
    {
        return _attributes.GetValidationSnapshot();
    }

    public event Action<ValidationSnapshot>? SnapshotChanged;

    private void OnSnapshotChanged(ValidationSnapshot snapshot)
    {
        SnapshotChanged?.Invoke(snapshot);
    }
}