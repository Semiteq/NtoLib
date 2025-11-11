using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Errors;

public sealed class CsvFilePathEmptyError : BilingualError
{
    public CsvFilePathEmptyError()
        : base(
            "File path cannot be empty",
            "Путь к файлу не может быть пустым")
    {
    }
}