using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using NtoLib.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.MbeTable.ModuleCore.Entities;
using NtoLib.MbeTable.ModuleCore.Properties;
using NtoLib.MbeTable.ModuleCore.Services;
using NtoLib.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

namespace NtoLib.MbeTable.ServiceRecipeAssembly.Clipboard.Transform;

public sealed class ClipboardStepsTransformer : IClipboardStepsTransformer
{
	private readonly IActionRepository _actionRepository;
	private readonly PropertyDefinitionRegistry _propertyRegistry;
	private readonly IReadOnlyList<ColumnDefinition> _columns;

	public ClipboardStepsTransformer(
		IActionRepository actionRepository,
		PropertyDefinitionRegistry propertyRegistry,
		IReadOnlyList<ColumnDefinition> columns)
	{
		_actionRepository = actionRepository ?? throw new ArgumentNullException(nameof(actionRepository));
		_propertyRegistry = propertyRegistry ?? throw new ArgumentNullException(nameof(propertyRegistry));
		_columns = columns ?? throw new ArgumentNullException(nameof(columns));
	}

	public Result<IReadOnlyList<Step>> Transform(IReadOnlyList<PortableStepDto> dtos)
	{
		var steps = new List<Step>(dtos.Count);

		for (int i = 0; i < dtos.Count; i++)
		{
			var dto = dtos[i];
			var actionResult = _actionRepository.GetActionDefinitionById(dto.ActionId);
			if (actionResult.IsFailed)
				return Result
					.Fail<IReadOnlyList<Step>>(
						new ClipboardTransformFailedError(i, $"ActionId {dto.ActionId} not found"))
					.WithErrors(actionResult.Errors);

			var actionDef = actionResult.Value;

			var builderResult = StepBuilder.Create(actionDef, _propertyRegistry, _columns);
			if (builderResult.IsFailed)
				return Result.Fail<IReadOnlyList<Step>>(new ClipboardTransformFailedError(i, "Builder creation failed"))
					.WithErrors(builderResult.Errors);

			var builder = builderResult.Value;

			foreach (var kv in dto.RawValues)
			{
				var columnDef =
					actionDef.Columns.FirstOrDefault(c => c.Key.Equals(kv.Key, StringComparison.OrdinalIgnoreCase));
				if (columnDef == null)
				{
					// Skip unknown keys silently; schema descriptor should prevent this.
					continue;
				}

				var columnId = new ColumnIdentifier(columnDef.Key);
				if (!builder.Supports(columnId))
					continue;

				var propertyDef = _propertyRegistry.GetPropertyDefinition(columnDef.PropertyTypeId);
				var parseResult = propertyDef.TryParse(kv.Value);
				if (parseResult.IsFailed)
				{
					return Result
						.Fail<IReadOnlyList<Step>>(new ClipboardTransformFailedError(i,
							$"Failed to parse value '{kv.Value}' for column '{kv.Key}'"))
						.WithErrors(parseResult.Errors);
				}

				var setResult = builder.WithOptionalDynamic(columnId, parseResult.Value);
				if (setResult.IsFailed)
				{
					return Result
						.Fail<IReadOnlyList<Step>>(
							new ClipboardTransformFailedError(i, $"Failed to set column '{kv.Key}'"))
						.WithErrors(setResult.Errors);
				}
			}

			steps.Add(builder.Build());
		}

		return steps.AsReadOnly();
	}
}
