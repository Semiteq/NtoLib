using System;

namespace NtoLib.TrendPensManager.Entities;

public sealed record ParameterInfo
{
	public ParameterInfo(string name, string fullPath)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException(@"Parameter name must not be empty.", nameof(name));
		}

		if (string.IsNullOrWhiteSpace(fullPath))
		{
			throw new ArgumentException(@"Parameter full path must not be empty.", nameof(fullPath));
		}

		Name = name;
		FullPath = fullPath;
	}

	public string Name { get; init; }
	public string FullPath { get; init; }
}
