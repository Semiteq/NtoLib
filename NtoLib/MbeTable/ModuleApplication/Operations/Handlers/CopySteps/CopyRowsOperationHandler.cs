using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentResults;

using NtoLib.MbeTable.ModuleApplication.Operations.Pipeline;
using NtoLib.MbeTable.ModuleCore.Facade;
using NtoLib.MbeTable.ServiceClipboard;
using NtoLib.MbeTable.ServiceRecipeAssembly.Clipboard.Schema;

namespace NtoLib.MbeTable.ModuleApplication.Operations.Handlers.CopySteps;

public sealed class CopyRowsOperationHandler : IRecipeOperationHandler<CopyRowsArgs>
{
	private readonly OperationPipeline _pipeline;
	private readonly CopyRowsOperationDefinition _op;
	private readonly IRecipeFacade _facade;
	private readonly IClipboardSchemaDescriptor _schema;
	private readonly IClipboardService _clipboard;

	public CopyRowsOperationHandler(
		OperationPipeline pipeline,
		CopyRowsOperationDefinition op,
		IRecipeFacade facade,
		IClipboardSchemaDescriptor schema,
		IClipboardService clipboard)
	{
		_pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
		_op = op ?? throw new ArgumentNullException(nameof(op));
		_facade = facade ?? throw new ArgumentNullException(nameof(facade));
		_schema = schema ?? throw new ArgumentNullException(nameof(schema));
		_clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
	}

	public async Task<Result> ExecuteAsync(CopyRowsArgs args)
	{
		return await _pipeline.RunAsync(
			_op,
			() => Task.FromResult(PerformCopy(args.Indices)),
			successMessage: args.Indices.Count == 0 ? null : $"Скопировано {args.Indices.Count} строк");
	}

	private Result PerformCopy(IReadOnlyList<int> indices)
	{
		var recipe = _facade.CurrentSnapshot.Recipe;
		var valid = indices.Where(i => i >= 0 && i < recipe.Steps.Count).Distinct().OrderBy(i => i).ToList();

		if (valid.Count == 0)
			return Result.Ok();

		var steps = valid.Select(i => recipe.Steps[i]).ToList();
		return _clipboard.WriteSteps(steps, _schema.TransferColumns);
	}
}

public sealed class CopyRowsArgs
{
	public IReadOnlyList<int> Indices { get; }

	public CopyRowsArgs(IReadOnlyList<int> indices)
	{
		Indices = indices ?? throw new ArgumentNullException(nameof(indices));
	}
}
