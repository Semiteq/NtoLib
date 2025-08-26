#nullable enable

using System;
using System.ComponentModel;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;

namespace NtoLib.Recipes.MbeTable.Presentation.Table
{
    /// <summary>
    /// Provides a dynamic property description for a single column in the StepViewModel.
    /// This allows the DataGridView's data binding engine to interact with the
    /// underlying Step record's properties as if they were real C# properties.
    /// </summary>
    internal sealed class StepPropertyDescriptor : PropertyDescriptor
    {
        private readonly ColumnDefinition _columnDefinition;

        public StepPropertyDescriptor(ColumnDefinition columnDefinition)
            : base(columnDefinition.Key.Value, null)
        {
            _columnDefinition = columnDefinition;
        }

        public override Type ComponentType => typeof(StepViewModel);
        public override bool IsReadOnly => _columnDefinition.ReadOnly;
        public override Type PropertyType => _columnDefinition.SystemType;

        public override bool CanResetValue(object component) => false;
        public override void ResetValue(object component) { }
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
}