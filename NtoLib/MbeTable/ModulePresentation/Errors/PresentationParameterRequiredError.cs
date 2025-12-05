using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModulePresentation.Errors;

public sealed class PresentationParameterRequiredError : BilingualError
{
	public string ParameterName { get; }

	public PresentationParameterRequiredError(string parameterName)
		: base(
			$"{parameterName} is required",
			$"{parameterName} обязателен")
	{
		ParameterName = parameterName;
		Metadata["parameterName"] = parameterName;
	}
}
