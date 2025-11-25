using System.Collections.Generic;

using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public class AssemblyMissingOrInvalidTarget : BilingualError
{
	public AssemblyMissingOrInvalidTarget(List<string> errors)
		: base(
			$"Missing or invalid targets in current environment: " + string.Join("; ", errors),
			"Отсутствующие или недействительные цели в текущей среде: " + string.Join("; ", errors))
	{
	}
}
