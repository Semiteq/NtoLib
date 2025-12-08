using System;
using System.IO;

namespace NtoLib.Recipes.MbeTable.ModuleConfig;

/// <summary>
/// Defines the paths to configuration files required for application initialization.
/// </summary>
public sealed record ConfigurationFiles
{
	public string PropertyDefsPath { get; }
	public string ColumnDefsPath { get; }
	public string PinGroupDefsPath { get; }
	public string ActionDefsPath { get; }

	public ConfigurationFiles(string baseDirectory, params string[] fileNames)
	{
		if (fileNames == null || fileNames.Length < 4)
			throw new ArgumentException(
				"At least 4 configuration file names are required in the order: PropertyDefs.yaml, ColumnDefs.yaml, PinGroupDefs.yaml, ActionsDefs.yaml.",
				nameof(fileNames));

		PropertyDefsPath = Path.Combine(baseDirectory, fileNames[0]);
		ColumnDefsPath = Path.Combine(baseDirectory, fileNames[1]);
		PinGroupDefsPath = Path.Combine(baseDirectory, fileNames[2]);
		ActionDefsPath = Path.Combine(baseDirectory, fileNames[3]);
	}
}
