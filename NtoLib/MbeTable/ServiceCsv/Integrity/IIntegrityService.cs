using System.Collections.Generic;

namespace NtoLib.MbeTable.ServiceCsv.Integrity;

/// <summary>
/// Manages data integrity operations using cryptographic hashing.
/// </summary>
public interface IIntegrityService
{
	/// <summary>
	/// Calculates SHA-256 hash for the provided data rows.
	/// </summary>
	/// <param name="dataRows">Collection of data rows in canonical format.</param>
	/// <returns>Base64-encoded hash string.</returns>
	string CalculateHash(IEnumerable<string> dataRows);

	/// <summary>
	/// Verifies data integrity by comparing hashes.
	/// </summary>
	/// <param name="expectedHash">Expected hash value.</param>
	/// <param name="actualHash">Actual calculated hash value.</param>
	/// <returns>Integrity check result.</returns>
	IntegrityCheckResult VerifyIntegrity(string expectedHash, string actualHash);
}
