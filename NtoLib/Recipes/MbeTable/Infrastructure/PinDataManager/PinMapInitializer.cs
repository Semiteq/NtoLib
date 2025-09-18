#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using MasterSCADA.Hlp;
using NtoLib.Recipes.MbeTable.Config.Yaml.Loaders;
using NtoLib.Recipes.MbeTable.Config.Yaml.Validators;

namespace NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

/// <summary>
/// Default implementation of pin map initialization from PinGroupDefs.yaml.
/// </summary>
public sealed class PinMapInitializer : IPinMapInitializer
{
    private readonly IPinGroupDefsLoader _loader;
    private readonly IPinGroupDefsValidator _validator;

    public PinMapInitializer(IPinGroupDefsLoader loader, IPinGroupDefsValidator validator)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public Dictionary<string, (int FirstPinId, int PinQuantity)> InitializePinsFromConfig(
        MbeTableFB fb,
        string? baseDirectory = null,
        string fileName = "PinGroupDefs.yaml")
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
        _validator.Validate(groups);

        var snapshot = new Dictionary<string, (int FirstPinId, int PinQuantity)>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var groupNode = fb.Root.AddGroup(group.PinGroupId, group.GroupName);

            for (var i = 0; i < group.PinQuantity; i++)
            {
                var pinId = group.FirstPinId + i;
                var pinName = $"{group.GroupName}{i + 1}";
                groupNode.AddPinWithID(pinId, pinName, PinType.Pin, typeof(string), "");
            }

            snapshot[group.GroupName] = (group.FirstPinId, group.PinQuantity);
        }

        return snapshot;
    }
}