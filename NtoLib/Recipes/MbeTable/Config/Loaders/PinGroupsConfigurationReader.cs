#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Models.ActionTargets;

namespace NtoLib.Recipes.MbeTable.Config.Loaders;

/// <summary>
/// Facade for loading pin groups configuration from disk using <see cref="IPinGroupsDataLoader"/>.
/// </summary>
public sealed class PinGroupsConfigurationReader
{
    private readonly IPinGroupsDataLoader _loader;

    public PinGroupsConfigurationReader()
        : this(new PinGroupsDataDataLoader())
    {
    }

    public PinGroupsConfigurationReader(IPinGroupsDataLoader loader)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
    }

    public Result<List<PinGroupData>> Load(string baseDirectory, string fileName = "PinGroups.json")
    {
        if (string.IsNullOrWhiteSpace(baseDirectory))
            return Result.Fail("Base directory is null or empty.");

        var path = Path.Combine(baseDirectory, fileName);
        return _loader.LoadPinGroups(path);
    }
}