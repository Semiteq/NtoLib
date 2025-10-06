using System;
using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.Config.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.Presentation.Columns;

/// <summary>
/// DI-driven registry that resolves <see cref="IColumnFactory"/> for given ColumnType string.
/// </summary>
public interface IColumnFactoryRegistry
{
    /// <summary>
    /// Registers mapping between YAML <c>column_type</c> and factory type (called from Composition Root).
    /// </summary>
    void RegisterFactory(string columnType, Type factoryType);

    /// <summary>
    /// Creates fully configured <see cref="DataGridViewColumn"/> for provided definition.
    /// </summary>
    DataGridViewColumn CreateColumn(ColumnDefinition definition);
}