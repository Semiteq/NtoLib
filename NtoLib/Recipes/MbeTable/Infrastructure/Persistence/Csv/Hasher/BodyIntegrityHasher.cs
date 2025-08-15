#nullable enable
using System.Security.Cryptography;
using System.Text;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Csv.Hasher;

public sealed class BodyIntegrityHasher
{
    private readonly SHA256 _sha = SHA256.Create();
    private readonly StringBuilder _sb = new();

    public void AppendDataRow(string csvLineCanonical)
    {
        _sb.Append(csvLineCanonical);
        _sb.Append("\r\n");
    }

    public string ComputeBase64()
    {
        var bytes = Encoding.UTF8.GetBytes(_sb.ToString());
        var hash = _sha.ComputeHash(bytes);
        return System.Convert.ToBase64String(hash);
    }
}