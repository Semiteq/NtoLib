using System;
using System.IO;

namespace NtoLib.MbeTable.ServiceCsv.Metadata;

/// <summary>
/// Handles reading and writing of recipe file metadata.
/// </summary>
public sealed class MetadataService : IMetadataService
{
	private readonly RecipeFileMetadataSerializer _serializer;

	public MetadataService(RecipeFileMetadataSerializer serializer)
	{
		_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
	}

	public (RecipeFileMetadata metadata, int linesCount) ReadMetadata(string fullText)
	{
		return _serializer.ReadAllMeta(fullText);
	}

	public void WriteMetadata(TextWriter writer, RecipeFileMetadata metadata)
	{
		_serializer.Write(writer, metadata);
	}
}
