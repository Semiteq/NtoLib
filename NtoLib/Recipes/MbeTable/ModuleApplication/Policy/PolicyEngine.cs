using System;
using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Contracts;
using NtoLib.Recipes.MbeTable.ModuleApplication.Policy.Registry;
using NtoLib.Recipes.MbeTable.ModuleApplication.State;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Policy;

public sealed class PolicyEngine
{
	private readonly ErrorPolicyRegistry _registry;

	public PolicyEngine(ErrorPolicyRegistry registry)
	{
		_registry = registry ?? throw new ArgumentNullException(nameof(registry));
	}

	public OperationDecision Decide(OperationId operation, IEnumerable<IReason> reasons)
	{
		var scope = OperationScopesMap.Map(operation);

		IReason? blockingError = null;
		IReason? blockingWarning = null;

		foreach (var r in reasons)
		{
			if (!_registry.Blocks(r, scope))
			{
				continue;
			}

			var severity = _registry.GetSeverity(r);
			if (severity == ErrorSeverity.Error || severity == ErrorSeverity.Critical)
			{
				blockingError ??= r;
			}
			else if (severity == ErrorSeverity.Warning)
			{
				blockingWarning ??= r;
			}

			if (blockingError != null)
			{
				break;
			}
		}

		if (blockingError != null)
		{
			return OperationDecision.BlockedError(blockingError);
		}

		if (blockingWarning != null)
		{
			return OperationDecision.BlockedWarning(blockingWarning);
		}

		return OperationDecision.Allowed();
	}
}
