using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using FluentResults;

using NtoLib.OpcTreeManager.Entities;

namespace NtoLib.OpcTreeManager.Config;

public static class TreeSnapshotLoader
{
	private static readonly JsonSerializerOptions _jsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public static Result<Dictionary<string, NodeSnapshot>> Load(string path)
	{
		if (!File.Exists(path))
		{
			return Result.Fail($"Tree snapshot file not found: {path}");
		}

		return Result.Try(
			() => Deserialize(path),
			ex => new Error($"Error reading tree snapshot file '{path}': {ex.Message}"));
	}

	private static Dictionary<string, NodeSnapshot> Deserialize(string path)
	{
		var json = File.ReadAllText(path);

		var raw = JsonSerializer.Deserialize<Dictionary<string, NodeSnapshot>>(json, _jsonOptions)
			?? throw new InvalidOperationException($"Tree snapshot file parsed as null: {path}");

		var result = new Dictionary<string, NodeSnapshot>(raw.Count, StringComparer.Ordinal);

		foreach (var entry in raw)
		{
			result[entry.Key] = FilterInvalidLinks(entry.Value);
		}

		return result;
	}

	private static NodeSnapshot FilterInvalidLinks(NodeSnapshot snapshot)
	{
		if (snapshot.Links == null || snapshot.Links.Count == 0)
		{
			return snapshot with { Links = Array.Empty<LinkEntry>() };
		}

		var filtered = new List<LinkEntry>(snapshot.Links.Count);

		foreach (var link in snapshot.Links)
		{
			if (string.IsNullOrWhiteSpace(link.LocalPinPath) || string.IsNullOrWhiteSpace(link.ExternalPinPath))
			{
				continue;
			}

			filtered.Add(link);
		}

		IReadOnlyList<LinkEntry> links = filtered.Count == 0 ? Array.Empty<LinkEntry>() : filtered;

		return snapshot with { Links = links };
	}
}
