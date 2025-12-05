using System;

using FluentResults;

using NtoLib.MbeTable.ModuleApplication.State;

namespace NtoLib.MbeTable.ModuleApplication.Operations.Pipeline;

internal sealed class GateFactory
{
	private readonly IStateProvider _state;

	public GateFactory(IStateProvider state)
	{
		_state = state;
	}

	public Result<IDisposable> Acquire(IOperationDefinition op)
	{
		return op.IsLongRunning
			? _state.BeginOperation(op.Kind, op.Id)
			: Result.Ok<IDisposable>(new NullDisposable());
	}
}
