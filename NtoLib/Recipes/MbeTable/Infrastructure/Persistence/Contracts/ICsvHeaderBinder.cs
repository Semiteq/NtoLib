using NtoLib.Recipes.MbeTable.Core.Domain.Schema;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Csv;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Contracts;

public interface ICsvHeaderBinder
{
    (CsvHeaderBinder.Binding Result, string Error) Bind(string[] headerTokens, TableSchema schema);
}