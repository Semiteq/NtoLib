using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Pipeline;
using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Facade;
using NtoLib.Recipes.MbeTable.ModuleCore.Runtime;
using NtoLib.Recipes.MbeTable.ModuleCore.Snapshot;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Assembly;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.PasteSteps;

public sealed class PasteRowsOperationHandler : IRecipeOperationHandler<PasteRowsArgs>
{
    private readonly OperationPipeline _pipeline;
    private readonly PasteRowsOperationDefinition _op;
    private readonly IClipboardAssemblyService _assembly;
    private readonly IRecipeFacade _facade;
    private readonly ITimerService _timer;
    private readonly RecipeViewModel _viewModel;

    public PasteRowsOperationHandler(
        OperationPipeline pipeline,
        PasteRowsOperationDefinition op,
        IClipboardAssemblyService assembly,
        IRecipeFacade facade,
        ITimerService timer,
        RecipeViewModel viewModel)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _op = op ?? throw new ArgumentNullException(nameof(op));
        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        _facade = facade ?? throw new ArgumentNullException(nameof(facade));
        _timer = timer ?? throw new ArgumentNullException(nameof(timer));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }

    public async Task<Result> ExecuteAsync(PasteRowsArgs args)
    {
        var assembleResult = _assembly.AssembleFromClipboard();
        if (assembleResult.IsFailed)
            return assembleResult.ToResult();

        var steps = assembleResult.Value;
        if (steps.Count == 0)
        {
            var emptyResult = await _pipeline.RunAsync(
                _op,
                () => Task.FromResult(assembleResult.ToResult()),
                successMessage: null);

            return emptyResult;
        }

        var result = await _pipeline.RunAsync(
            _op,
            () => Task.FromResult(PerformPasteCore(args.TargetIndex, steps, assembleResult.Reasons)),
            successMessage: null);

        if (result.IsSuccess)
        {
            _viewModel.OnRecipeStructureChanged();
            _timer.Reset();
        }

        return result.ToResult();
    }

    private Result<RecipeAnalysisSnapshot> PerformPasteCore(
        int targetIndex,
        IReadOnlyList<Step> steps,
        IReadOnlyList<IReason> assemblyReasons)
    {
        var insertResult = _facade.InsertSteps(targetIndex, steps);

        if (insertResult.IsFailed)
            return insertResult;

        return insertResult.WithReasons(assemblyReasons);
    }
}

public sealed class PasteRowsArgs
{
    public int TargetIndex { get; }

    public PasteRowsArgs(int targetIndex)
    {
        TargetIndex = targetIndex;
    }
}