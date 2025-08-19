using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Contracts;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Csv.Fingerprints;

public class SchemaFingerprintUtil : ISchemaFingerprintUtil
{
    public string BuildNormalized(TableSchema schema)
    {
        var cols = schema.GetColumns()
            .Where(c => c.ReadOnly == false)
            .OrderBy(c => c.Index)
            .Select(c => $"{c.Index}|{c.Code}");
        return string.Join("\n", cols);
    }
    
    public string ComputeSha256Base64(string text)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(text);
        return System.Convert.ToBase64String(sha.ComputeHash(bytes));
    }
}