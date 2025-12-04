using System;

namespace NtoLib.TrendPensManager.Entities;

public sealed record ParameterInfo(string Name, string FullPath)
{
	public string Name { get; init; } = !string.IsNullOrWhiteSpace(Name)
		? Name
		: throw new ArgumentException(@"Parameter name must not be empty.", nameof(Name));

	public string FullPath { get; init; } = !string.IsNullOrWhiteSpace(FullPath)
		? FullPath
		: throw new ArgumentException(@"Parameter full path must not be empty.", nameof(FullPath));
}
