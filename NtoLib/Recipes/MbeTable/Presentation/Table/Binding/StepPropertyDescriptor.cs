#nullable enable

using System;
using System.ComponentModel;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Binding;

/// <summary>
/// Provides a dynamic property description for a single column in the StepViewModel.
/// </summary>
internal sealed class StepPropertyDescriptor : PropertyDescriptor
{
    private readonly ColumnDefinition _columnDefinition;
    public override Type PropertyType { get; }

    public StepPropertyDescriptor(ColumnDefinition columnDefinition, Type propertyType)
        : base(columnDefinition.Key.Value, null)
    {
        _columnDefinition = columnDefinition;
        PropertyType = propertyType;
    }

    public override Type ComponentType => typeof(StepViewModel);

    public override bool IsReadOnly => _columnDefinition.ReadOnly;

    public override bool CanResetValue(object component) => false;

    public override void ResetValue(object component)
    {
    }

    public override bool ShouldSerializeValue(object component) => false;

    public override object? GetValue(object? component)
    {
        if (component is not StepViewModel viewModel)
            return null;

        return viewModel.GetPropertyValue(_columnDefinition.Key);
    }

    public override void SetValue(object? component, object? value)
    {
        if (component is not StepViewModel viewModel)
            return;

        viewModel.SetPropertyValue(_columnDefinition.Key, value);
    }
}