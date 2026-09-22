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

	/// <summary>
	/// Writes the snapshot to <paramref name="path"/>, refusing an empty one: a zero-node snapshot
	/// is never a legitimate reference state, and this is the only writer of tree.json.
	/// </summary>
	public static Result Write(Dictionary<string, NodeSnapshot> snapshot, string path)
	{
		if (snapshot.Count == 0)
		{
			return Result.Fail(
				$"Refusing to write an empty snapshot over '{path}': the scanned group holds no nodes.");
		}

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
