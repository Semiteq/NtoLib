﻿using System;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using NtoLib.Recipes.MbeTable.Application.State;
using NtoLib.Recipes.MbeTable.Presentation.State;

namespace NtoLib.Recipes.MbeTable.Presentation.Commands;

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
        if (!CanExecute()) return Result.Fail("UI is busy");

        using (_busy.Enter(GetOperationKind()))
        {
            try
            {
                return await runner(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return Result.Fail("Operation canceled");
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }
        }
    }
}