using System;
using System.Collections.Concurrent;
using System.Windows.Forms;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.MbeTable.ModulePresentation.Columns.ComboBox;
using NtoLib.MbeTable.ModulePresentation.Columns.Text;
using NtoLib.MbeTable.ModulePresentation.Style;

namespace NtoLib.MbeTable.ModulePresentation.Columns;

/// <summary>
/// DI-driven registry that maps YAML <c>column_type</c> strings to concrete
/// <see cref="IFactoryColumn"/> implementations.
/// </summary>
public sealed class FactoryColumnRegistry
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ConcurrentDictionary<string, Type> _mapping = new(StringComparer.OrdinalIgnoreCase);

	public FactoryColumnRegistry(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
		SetDefaultMapping();
	}

	private void SetDefaultMapping()
	{
		_mapping["action_combo_box"] = typeof(ActionComboBox);
		_mapping["action_target_combo_box"] = typeof(TargetComboBox);
		_mapping["property_field"] = typeof(TextBoxExtension);
		_mapping["step_start_time_field"] = typeof(StepStartTime);
		_mapping["text_field"] = typeof(TextBoxExtension);
	}


	public DataGridViewColumn CreateColumn(ColumnDefinition definition)
	{
		if (!_mapping.TryGetValue(definition.ColumnType, out var factoryType))
			throw new InvalidOperationException($"Unknown column_type '{definition.ColumnType}'.");

		var factory = (IFactoryColumn)_serviceProvider.GetRequiredService(factoryType);
		var scheme = _serviceProvider.GetRequiredService<IColorSchemeProvider>().Current;
		return factory.CreateColumn(definition, scheme);
	}
}
