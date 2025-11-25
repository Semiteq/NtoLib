using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ServiceClipboard;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Parsing;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Transform;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Common;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Assembly;

public sealed class ClipboardAssemblyService : IClipboardAssemblyService
{
	private readonly IClipboardService _clipboard;
	private readonly IClipboardParser _parser;
	private readonly IClipboardStepsTransformer _transformer;
	private readonly AssemblyValidator _validator;
	private readonly ILogger<ClipboardAssemblyService> _logger;

	public ClipboardAssemblyService(
		IClipboardService clipboard,
		IClipboardParser parser,
		IClipboardStepsTransformer transformer,
		AssemblyValidator validator,
		ILogger<ClipboardAssemblyService> logger)
	{
		_clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
		_parser = parser ?? throw new ArgumentNullException(nameof(parser));
		_transformer = transformer ?? throw new ArgumentNullException(nameof(transformer));
		_validator = validator ?? throw new ArgumentNullException(nameof(validator));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public Result<IReadOnlyList<Step>> AssembleFromClipboard()
	{
		_logger.LogDebug("Starting clipboard assembly");

		var readResult = _clipboard.ReadRows();
		if (readResult.IsFailed)
		{
			_logger.LogError("Clipboard ReadRows failed");
			return readResult.ToResult<IReadOnlyList<Step>>();
		}

		var rows = readResult.Value;
		if (rows.Count == 0)
		{
			_logger.LogDebug("Clipboard has no rows");
			var empty = Result.Ok<IReadOnlyList<Step>>(Array.Empty<Step>());
			foreach (var r in readResult.Reasons)
				empty = empty.WithReason(r);
			return empty;
		}

		var parseResult = _parser.Parse(rows);
		if (parseResult.IsFailed)
		{
			_logger.LogError("Parsing clipboard rows failed");
			return parseResult.ToResult<IReadOnlyList<Step>>();
		}

		var dtos = parseResult.Value;

		var transformResult = _transformer.Transform(dtos);
		if (transformResult.IsFailed)
		{
			_logger.LogError("Transforming DTOs to Steps failed");
			return transformResult;
		}

		var steps = transformResult.Value;
		var recipe = new Recipe(steps.ToImmutableList());

		var validationResult = _validator.ValidateRecipe(recipe);
		if (validationResult.IsFailed)
		{
			_logger.LogError("Environment validation failed");
			return validationResult.ToResult<IReadOnlyList<Step>>();
		}

		var final = Result.Ok<IReadOnlyList<Step>>(steps);
		if (readResult.Reasons.Count > 0)
			final = final.WithReasons(readResult.Reasons);
		if (validationResult.Reasons.Count > 0)
			final = final.WithReasons(validationResult.Reasons);

		_logger.LogDebug("Clipboard assembly completed successfully: {Count} steps", steps.Count);
		return final;
	}
}
