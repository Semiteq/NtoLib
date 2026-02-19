using System;
using System.Collections.Generic;

using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.State;

public sealed class PolicyReasonsSinkAdapter
{
	private readonly StateProvider _state;

	public PolicyReasonsSinkAdapter(StateProvider state)
	{
		_state = state ?? throw new ArgumentNullException(nameof(state));
	}

	public void SetPolicyReasons(IEnumerable<IReason> reasons)
	{
		_state.SetPolicyReasons(reasons);
	}
}
