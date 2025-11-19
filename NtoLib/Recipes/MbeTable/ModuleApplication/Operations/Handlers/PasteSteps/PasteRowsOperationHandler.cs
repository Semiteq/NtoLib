using System;
using System.Threading.Tasks;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Pipeline;
using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleCore.Facade;
using NtoLib.Recipes.MbeTable.ModuleCore.Runtime;
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
        var result = await _pipeline.RunAsync(
            _op,
            () => Task.FromResult(PerformPaste(args.TargetIndex)),
            successMessage: null);

        if (result.IsSuccess)
        {
            _viewModel.OnRecipeStructureChanged();
            _timer.Reset();
        }

        return result;
    }

    private Result PerformPaste(int targetIndex)
    {
        var assembleResult = _assembly.AssembleFromClipboard();
        if (assembleResult.IsFailed)
            return assembleResult.ToResult();

        var steps = assembleResult.Value;
        if (steps.Count == 0)
            return assembleResult.ToResult();

        var insertResult = _facade.InsertSteps(targetIndex, steps);
        
        var final = insertResult.ToResult();
        if (assembleResult.Reasons.Count > 0)
            final = final.WithReasons(assembleResult.Reasons);
        
        return final;
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