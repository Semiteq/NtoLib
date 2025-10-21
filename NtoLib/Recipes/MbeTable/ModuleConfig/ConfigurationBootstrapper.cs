using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Common;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Actions;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Columns;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.PinGroups;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Properties;
using NtoLib.Recipes.MbeTable.ModuleConfig.Mapping;
using NtoLib.Recipes.MbeTable.ModuleConfig.Sections;
using NtoLib.Recipes.MbeTable.ModuleConfig.Validation;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties.Contracts;

namespace NtoLib.Recipes.MbeTable.ModuleConfig;

/// <summary>
/// Entry point for bootstrapping the application configuration from YAML files.
/// Orchestrates loading, validation, conversion, and assembly of configuration data.
/// </summary>
public sealed class ConfigurationBootstrapper
{
    private readonly IYamlDeserializer _yamlDeserializer;
    private readonly CrossReferenceValidator _crossReferenceValidator;

    private readonly PropertyDefinitionMapper _propertyMapper;
    private readonly ColumnDefinitionMapper _columnMapper;
    private readonly ActionDefinitionMapper _actionMapper;
    private readonly PinGroupDataMapper _pinGroupMapper;

    public ConfigurationBootstrapper()
    {
        _yamlDeserializer = new YamlDeserializer();
        _crossReferenceValidator = new CrossReferenceValidator();

        _propertyMapper = new PropertyDefinitionMapper();
        _columnMapper = new ColumnDefinitionMapper();
        _actionMapper = new ActionDefinitionMapper();
        _pinGroupMapper = new PinGroupDataMapper();
    }

    /// <summary>
    /// Loads and validates the complete application configuration from YAML files.
    /// Returns validated sections and assembled application configuration.
    /// </summary>
    /// <param name="files">The configuration file paths.</param>
    /// <returns>A result containing sections and assembled application configuration, or error details.</returns>
    public Result<(ConfigurationSections Sections, AppConfiguration AppConfiguration)> LoadConfiguration(
        ConfigurationFiles files)
    {
        try
        {
            var sectionsResult = LoadAllSections(files);
            if (sectionsResult.IsFailed)
                return sectionsResult.ToResult();

            var sections = sectionsResult.Value;

            var crossRefResult = _crossReferenceValidator.Validate(sections);
            if (crossRefResult.IsFailed)
                return crossRefResult.ToResult<(ConfigurationSections, AppConfiguration)>();

            var appConfig = ConvertAndAssemble(sections);
            return Result.Ok((sections, appConfig));
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("A critical error occurred during configuration bootstrapping.").CausedBy(ex));
        }
    }

    private Result<ConfigurationSections> LoadAllSections(ConfigurationFiles files)
    {
        var propertyDefsResult = LoadPropertyDefs(files.PropertyDefsPath);
        if (propertyDefsResult.IsFailed)
            return propertyDefsResult.ToResult();

        var columnDefsResult = LoadColumnDefs(files.ColumnDefsPath);
        if (columnDefsResult.IsFailed)
            return columnDefsResult.ToResult();

        var pinGroupDefsResult = LoadPinGroupDefs(files.PinGroupDefsPath);
        if (pinGroupDefsResult.IsFailed)
            return pinGroupDefsResult.ToResult();

        var actionDefsResult = LoadActionDefs(files.ActionDefsPath);
        if (actionDefsResult.IsFailed)
            return actionDefsResult.ToResult();

        var sections = new ConfigurationSections(
            propertyDefsResult.Value,
            columnDefsResult.Value,
            pinGroupDefsResult.Value,
            actionDefsResult.Value);

        return Result.Ok(sections);
    }

    private Result<PropertyDefsSection> LoadPropertyDefs(string path)
    {
        var validator = new PropertyDefsValidator();
        var loader = new SectionLoader<YamlPropertyDefinition>(_yamlDeserializer, validator);

        return loader.Load(path)
            .Map(items => new PropertyDefsSection(items));
    }

    private Result<ColumnDefsSection> LoadColumnDefs(string path)
    {
        var validator = new ColumnDefsValidator();
        var loader = new SectionLoader<YamlColumnDefinition>(_yamlDeserializer, validator);

        return loader.Load(path)
            .Map(items => new ColumnDefsSection(items));
    }

    private Result<PinGroupDefsSection> LoadPinGroupDefs(string path)
    {
        var validator = new PinGroupDefsValidator();
        var loader = new SectionLoader<YamlPinGroupDefinition>(_yamlDeserializer, validator);

        return loader.Load(path)
            .Map(items => new PinGroupDefsSection(items));
    }

    private Result<ActionDefsSection> LoadActionDefs(string path)
    {
        var validator = new ActionDefsValidator();
        var loader = new SectionLoader<YamlActionDefinition>(_yamlDeserializer, validator);

        return loader.Load(path)
            .Map(items => new ActionDefsSection(items));
    }

    private AppConfiguration ConvertAndAssemble(ConfigurationSections sections)
    {
        // Build dictionary while preserving PropertyTypeId from YAML
        var propertyDefinitions = new Dictionary<string, IPropertyTypeDefinition>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var yamlDef in sections.PropertyDefs.Items)
        {
            var mapped = _propertyMapper.Map(yamlDef);
            propertyDefinitions[yamlDef.PropertyTypeId] = mapped;
        }

        var columnDefinitions = _columnMapper.MapMany(sections.ColumnDefs.Items);
        
        var actionDefinitions = _actionMapper.MapMany(sections.ActionDefs.Items)
            .ToDictionary(a => a.Id);
        
        var pinGroupData = _pinGroupMapper.MapMany(sections.PinGroupDefs.Items);

        return new AppConfiguration(
            propertyDefinitions,
            columnDefinitions,
            actionDefinitions,
            pinGroupData);
    }
}