using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ServiceClipboard.Sanitizer;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Warnings;

namespace NtoLib.Recipes.MbeTable.ServiceClipboard.Serialization;

public sealed class ClipboardSerializationService
{
	public string SerializeSteps(IReadOnlyList<Step> steps, IReadOnlyList<ColumnIdentifier> columns)
	{
		if (steps == null)
		{
			throw new ArgumentNullException(nameof(steps));
		}

		if (columns == null)
		{
			throw new ArgumentNullException(nameof(columns));
		}

		var sb = new StringBuilder();

		for (var i = 0; i < steps.Count; i++)
		{
			var step = steps[i];
			var cellValues = new List<string>(columns.Count);

			foreach (var col in columns)
			{
				string value;
				if (step.Properties.TryGetValue(col, out var prop) && prop != null)
				{
					// For action we store raw numeric value; for others display value sanitized.
					value = col == MandatoryColumns.Action
						? prop.GetValueAsObject?.ToString() ?? string.Empty
						: prop.GetDisplayValue;
				}
				else
				{
					value = string.Empty;
				}

				value = ClipboardSanitizer.SanitizeForCell(value);
				cellValues.Add(value);
			}

			sb.Append(string.Join("\t", cellValues));
			if (i < steps.Count - 1)
			{
				sb.Append('\n');
			}
		}

		return sb.ToString();
	}

	public Result<IReadOnlyList<string[]>> SplitRows(string? tsv)
	{
		if (string.IsNullOrWhiteSpace(tsv))
		{
			return Result.Ok<IReadOnlyList<string[]>>(Array.Empty<string[]>())
				.WithReason(new ClipboardEmptyWarning());
		}

		var lines = tsv!.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
		var result = lines
			.Select((line, idx) =>
			{
				var cells = line.Split('\t');
				for (var c = 0; c < cells.Length; c++)
				{
					cells[c] = ClipboardSanitizer.SanitizeForCell(cells[c]);
				}

				return cells;
			})
			.ToList()
			.AsReadOnly();

		return Result.Ok<IReadOnlyList<string[]>>(result);
	}
}
