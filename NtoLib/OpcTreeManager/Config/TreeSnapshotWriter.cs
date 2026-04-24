using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

using FluentResults;

using NtoLib.OpcTreeManager.Entities;

namespace NtoLib.OpcTreeManager.Config;

public static class TreeSnapshotWriter
{
	private static readonly JsonSerializerOptions _jsonOptions = new()
	{
		WriteIndented = true,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
	};

	public static Result Write(Dictionary<string, NodeSnapshot> snapshot, string path)
	{
		return Result.Try(() =>
		{
			var dir = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(dir))
			{
				Directory.CreateDirectory(dir);
			}

			var json = JsonSerializer.Serialize(snapshot, _jsonOptions);
			File.WriteAllText(path, json);
		});
	}
}
