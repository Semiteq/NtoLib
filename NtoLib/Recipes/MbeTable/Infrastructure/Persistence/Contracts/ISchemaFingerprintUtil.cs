using NtoLib.Recipes.MbeTable.Core.Domain.Schema;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Contracts;

public interface ISchemaFingerprintUtil
{
    string BuildNormalized(TableSchema schema);
    string ComputeSha256Base64(string text);
}