using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Errors;

public sealed class ApplicationSendBlockedByPlcError : BilingualError
{
    public ApplicationSendBlockedByPlcError()
        : base(
            "Send operation blocked by PLC logic",
            "Операция отправки заблокирована логикой ПЛК")
    {
    }
}