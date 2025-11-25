using System.IO;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Metadata;

/// <summary>
/// Handles recipe file metadata operations.
/// </summary>
public interface IMetadataService
{
	/// <summary>
	/// Reads metadata from the full text content.
	/// </summary>
	/// <param name="fullText">Complete file content.</param>
	/// <returns>Metadata and number of metadata lines.</returns>
	(RecipeFileMetadata metadata, int linesCount) ReadMetadata(string fullText);

	/// <summary>
	/// Writes metadata to the text writer.
	/// </summary>
	/// <param name="writer">Target text writer.</param>
	/// <param name="metadata">Metadata to write.</param>
	void WriteMetadata(TextWriter writer, RecipeFileMetadata metadata);
}
