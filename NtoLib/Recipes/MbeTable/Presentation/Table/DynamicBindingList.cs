#nullable enable

using System.Collections.Generic;
using System.ComponentModel;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Presentation.Table.Columns;

namespace NtoLib.Recipes.MbeTable.Presentation.Table
{
    /// <summary>
    /// A custom BindingList that implements ITypedList to provide dynamic property descriptors
    /// based on a TableSchema. This enables DataGridView to bind to a collection of objects
    /// whose properties are determined at runtime.
    /// </summary>
    public class DynamicBindingList : BindingList<StepViewModel>, ITypedList
    {
        private readonly PropertyDescriptorCollection _propertyDescriptors;

        public DynamicBindingList(TableSchema tableSchema)
        {
            var descriptors = new List<PropertyDescriptor>();
            foreach (var colDef in tableSchema.GetColumns())
            {
                descriptors.Add(new StepPropertyDescriptor(colDef));
            }
            _propertyDescriptors = new PropertyDescriptorCollection(descriptors.ToArray());
        }

        // --- ITypedList Implementation ---

        public PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[]? listAccessors)
        {
            return _propertyDescriptors;
        }

        public string GetListName(PropertyDescriptor[]? listAccessors)
        {
            return "Steps";
        }
    }
}