using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Integrity;

/// <summary>
/// Implements data integrity operations using SHA-256 hashing.
/// </summary>
public sealed class IntegrityService : IIntegrityService
{
    public string CalculateHash(IEnumerable<string> dataRows)
    {
        using var sha256 = SHA256.Create();
        var stringBuilder = new StringBuilder();
        
        foreach (var row in dataRows)
        {
            stringBuilder.Append(row);
            stringBuilder.Append("\r\n");
        }
        
        var bytes = Encoding.UTF8.GetBytes(stringBuilder.ToString());
        var hashBytes = sha256.ComputeHash(bytes);
        
        return Convert.ToBase64String(hashBytes);
    }

    public IntegrityCheckResult VerifyIntegrity(string expectedHash, string actualHash)
    {
        var isValid = string.Equals(expectedHash, actualHash, StringComparison.Ordinal);
        
        return new IntegrityCheckResult
        {
            IsValid = isValid,
            ExpectedHash = expectedHash,
            ActualHash = actualHash
        };
    }
}