using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Errors;

public sealed class ApplicationSendBlockedByPlcError : BilingualError
{
	public ApplicationSendBlockedByPlcError()
		: base(
			"Send operation blocked by PLC logic",
			"Операция отправки заблокирована логикой ПЛК")
	{
	}
}
