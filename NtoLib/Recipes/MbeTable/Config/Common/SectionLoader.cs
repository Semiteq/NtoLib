

using System;
using System.Collections.Generic;
using System.IO;

using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Validation;
using NtoLib.Recipes.MbeTable.Journaling.Errors;

namespace NtoLib.Recipes.MbeTable.Config.Common;

/// <summary>
/// Generic YAML section loader with integrated validation.
/// </summary>
/// <typeparam name="TDto">The DTO type for the section.</typeparam>
public sealed class SectionLoader<TDto> : ISectionLoader<TDto> where TDto : class
{
    private readonly IYamlDeserializer _deserializer;
    private readonly ISectionValidator<TDto> _validator;

    public SectionLoader(IYamlDeserializer deserializer, ISectionValidator<TDto> validator)
    {
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public Result<IReadOnlyList<TDto>> Load(string filePath)
    {
        return CheckPath(filePath)
            .Bind(CheckFileExists)
            .Bind(ReadFile)
            .Bind(content => _deserializer.Deserialize<TDto>(content))
            .Bind(ValidateAndReturn);
    }

    private Result<IReadOnlyList<TDto>> ValidateAndReturn(IReadOnlyList<TDto> items)
    {
        // Pass items as IEnumerable to match the interface
        var validationResult = _validator.Validate(items);
        if (validationResult.IsFailed)
            return validationResult;

        return Result.Ok(items);
    }

    private static Result<string> CheckPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Result.Fail(new Error("File path cannot be empty.")
                .WithMetadata("code", ErrorCode.ConfigInvalidSchema));
        }

        return Result.Ok(path);
    }

    private static Result<string> CheckFileExists(string path)
    {
        if (!File.Exists(path))
        {
            return Result.Fail(new Error($"Configuration file not found at: '{path}'")
                .WithMetadata("code", ErrorCode.ConfigFileNotFound)
                .WithMetadata("filePath", path));
        }

        return Result.Ok(path);
    }

    private static Result<string> ReadFile(string path)
    {
        try
        {
            var content = File.ReadAllText(path);
            return Result.Ok(content);
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error($"Failed to read file '{path}': {ex.Message}")
                .WithMetadata("code", ErrorCode.ConfigParseError)
                .WithMetadata("filePath", path)
                .CausedBy(ex));
        }
    }
}