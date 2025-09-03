#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using MasterSCADA.Hlp;
using NtoLib.Recipes.MbeTable.Config.Loaders;

namespace NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

/// <summary>
/// Default implementation of pin map initialization from PinGroups.json.
/// </summary>
public sealed class PinMapInitializer : IPinMapInitializer
{
    private readonly IPinGroupsDataLoader _loader;
    private readonly PinGroupsValidator _validator;

    public PinMapInitializer()
        : this(new PinGroupsDataDataLoader(), new PinGroupsValidator())
    {
    }

    public PinMapInitializer(IPinGroupsDataLoader loader, PinGroupsValidator validator)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public Dictionary<string, (int FirstPinId, int PinQuantity)> InitializePinsFromConfig(
        MbeTableFB fb,
        string? baseDirectory = null,
        string fileName = "PinGroups.json")
    {
        if (fb == null) throw new ArgumentNullException(nameof(fb));
        baseDirectory ??= AppDomain.CurrentDomain.BaseDirectory;
        var filePath = Path.Combine(baseDirectory, fileName);

        var loadResult = _loader.LoadPinGroups(filePath);
        if (loadResult.IsFailed)
        {
            var reason = string.Join("; ", loadResult.Errors);
            throw new InvalidOperationException($"Failed to load '{fileName}': {reason}");
        }

        var groups = loadResult.Value;
        _validator.ValidateOrThrow(groups);

        var snapshot = new Dictionary<string, (int FirstPinId, int PinQuantity)>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var groupNode = fb.Root.AddGroup(group.PinGroupId, group.GroupName);

            for (var i = 0; i < group.PinQuantity; i++)
            {
                var pinId = group.FirstPinId + i;
                var pinName = $"{group.GroupName}{i}";
                groupNode.AddPinWithID(pinId, pinName, PinType.Pin, typeof(string), 0d);
            }

            snapshot[group.GroupName] = (group.FirstPinId, group.PinQuantity);
        }

        return snapshot;
    }
}