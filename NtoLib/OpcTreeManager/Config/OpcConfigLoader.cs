using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using FluentResults;

using NtoLib.OpcTreeManager.Entities;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NtoLib.OpcTreeManager.Config;

public static class OpcConfigLoader
{
	public static Result<OpcConfig> Load(string path)
	{
		if (!File.Exists(path))
		{
			return Result.Fail($"Config file not found: {path}");
		}

		return Result.Try(
			() => Deserialize(path),
			ex => new Error($"Error reading config file '{path}': {ex.Message}"));
	}

	private static OpcConfig Deserialize(string path)
	{
		var yaml = File.ReadAllText(path);

		var deserializer = new DeserializerBuilder()
			.WithNamingConvention(CamelCaseNamingConvention.Instance)
			.Build();

		var raw = deserializer.Deserialize<RawConfig>(yaml)
			?? throw new InvalidOperationException($"Config file parsed as null: {path}");

		return ConvertRaw(raw);
	}

	private static OpcConfig ConvertRaw(RawConfig raw)
	{
		var config = new OpcConfig();

		if (raw.Projects == null)
		{
			return config;
		}

		foreach (var kvp in raw.Projects)
		{
			config.Projects[kvp.Key] = ConvertNodeList(kvp.Value, parentPath: kvp.Key);
		}

		return config;
	}

	private static List<NodeSpec> ConvertNodeList(IList<object>? items, string parentPath)
	{
		var result = new List<NodeSpec>();

		if (items == null)
		{
			return result;
		}

		var seenNames = new HashSet<string>(StringComparer.Ordinal);

		foreach (var item in items)
		{
			if (item == null)
			{
				continue;
			}

			var spec = ConvertNode(item, parentPath);
			if (!seenNames.Add(spec.Name))
			{
				throw new InvalidOperationException(
					$"Duplicate sibling name '{spec.Name}' under '{parentPath}'. "
					+ "Each node name must appear at most once under the same parent.");
			}

			result.Add(spec);
		}

		return result;
	}

	private static void ValidateNodeName(string name, string parentPath)
	{
		if (string.IsNullOrEmpty(name))
		{
			throw new InvalidOperationException(
				$"Empty node name under '{parentPath}'. Every entry must have a non-empty identifier.");
		}

		if (name.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }) >= 0)
		{
			throw new InvalidOperationException(
				$"Node name '{name}' under '{parentPath}' contains whitespace. "
				+ "Use the 'Name:' mapping form (e.g. '- Valves:\\n    - VPG1') rather than '- Valves - VPG1'.");
		}
	}

	private static NodeSpec ConvertNode(object raw, string parentPath)
	{
		if (raw is string scalar)
		{
			ValidateNodeName(scalar, parentPath);
			return new NodeSpec(scalar, Children: null);
		}

		if (raw is IDictionary<object, object> map)
		{
			if (map.Count != 1)
			{
				throw new InvalidOperationException(
					$"Node entry under '{parentPath}' must have exactly one key (got {map.Count}).");
			}

			var entry = map.First();

			if (entry.Key is not string name)
			{
				throw new InvalidOperationException(
					$"Node name under '{parentPath}' must be a string (got {entry.Key?.GetType().Name ?? "null"}).");
			}

			ValidateNodeName(name, parentPath);

			var childPath = parentPath + "/" + name;

			// `- Name:` (no value after the colon) → empty children list.
			// `- Name:\n    - X` → list of children.
			// Anything else (e.g. `- Name: foo`) → malformed.
			if (entry.Value == null)
			{
				return new NodeSpec(name, new List<NodeSpec>());
			}

			if (entry.Value is IList<object> childList)
			{
				return new NodeSpec(name, ConvertNodeList(childList, childPath));
			}

			throw new InvalidOperationException(
				$"Children of '{childPath}' must be a list (got {entry.Value.GetType().Name}).");
		}

		throw new InvalidOperationException(
			$"Node entry under '{parentPath}' must be a string or a single-key mapping (got {raw.GetType().Name}).");
	}

	private sealed class RawConfig
	{
		public Dictionary<string, List<object>>? Projects { get; set; }
	}
}
