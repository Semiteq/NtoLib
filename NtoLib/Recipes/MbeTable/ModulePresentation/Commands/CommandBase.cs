using System;
using System.Threading;
using System.Threading.Tasks;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ModulePresentation.State;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Commands;

public abstract class CommandBase
{
    private readonly IBusyStateManager _busy;

    protected CommandBase(IBusyStateManager busy)
    {
        _busy = busy ?? throw new ArgumentNullException(nameof(busy));
    }

    public bool CanExecute() => !_busy.IsBusy && CanExecuteCore();

    public Task<Result> ExecuteAsync(CancellationToken cancellationToken = default)
        => ExecuteWithBusyAsync(ExecuteInternalAsync, cancellationToken);

    public event EventHandler? CanExecuteChanged;

    protected void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    protected virtual bool CanExecuteCore() => true;
    protected abstract OperationKind GetOperationKind();
    protected abstract Task<Result> ExecuteInternalAsync(CancellationToken ct);

    protected async Task<Result> ExecuteWithBusyAsync(Func<CancellationToken, Task<Result>> runner, CancellationToken ct)
    {
        if (!CanExecute()) return Result.Fail(new Error("UI is busy").WithMetadata(nameof(Codes), Codes.CoreInvalidOperation));

        using (_busy.Enter(GetOperationKind()))
        {
            try
            {
                return await runner(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return Result.Fail(new Error("Operation canceled").WithMetadata(nameof(Codes), Codes.CoreInvalidOperation));
            }
            catch (Exception ex)
            {
                return Result.Fail(new Error(ex.Message).WithMetadata(nameof(Codes), Codes.UnknownError));
            }
        }
    }
}