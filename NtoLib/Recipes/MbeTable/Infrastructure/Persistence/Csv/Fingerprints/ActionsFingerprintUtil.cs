using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Contracts;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Csv.Fingerprints;

public class ActionsFingerprintUtil : IActionsFingerprintUtil
{
    public string Compute(ActionManager actionManager)
    {
        var normalized = actionManager.GetAllActions()
            .OrderBy(a => a.Key)
            .Select(a => $"{a.Key}:{a.Value}")
            .Aggregate(new StringBuilder(), (sb, s) => sb.AppendLine(s))
            .ToString();

        using var sha = SHA256.Create();
        return System.Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(normalized)));
    }
}