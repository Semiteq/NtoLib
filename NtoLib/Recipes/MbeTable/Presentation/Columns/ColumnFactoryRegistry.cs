using System;
using System.Collections.Concurrent;
using System.Windows.Forms;

using Microsoft.Extensions.DependencyInjection;
using NtoLib.Recipes.MbeTable.Config.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.Presentation.Columns;

/// <summary>
/// DI-driven registry that maps YAML <c>column_type</c> strings to concrete
/// <see cref="IColumnFactory"/> implementations.
/// </summary>
public sealed class ColumnFactoryRegistry : IColumnFactoryRegistry
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, Type> _mapping = new(StringComparer.OrdinalIgnoreCase);

    public ColumnFactoryRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void RegisterFactory(string columnType, Type factoryType)
    {
        if (!typeof(IColumnFactory).IsAssignableFrom(factoryType))
            throw new ArgumentException($"Type {factoryType.Name} must implement IColumnFactory.");

        _mapping[columnType] = factoryType;
    }

    public DataGridViewColumn CreateColumn(ColumnDefinition definition)
    {
        if (!_mapping.TryGetValue(definition.ColumnType, out var factoryType))
            throw new InvalidOperationException($"Unknown column_type '{definition.ColumnType}'.");

        var factory = (IColumnFactory)_serviceProvider.GetRequiredService(factoryType);
        var scheme  = _serviceProvider.GetRequiredService<Style.IColorSchemeProvider>().Current;
        return factory.CreateColumn(definition, scheme);
    }
}