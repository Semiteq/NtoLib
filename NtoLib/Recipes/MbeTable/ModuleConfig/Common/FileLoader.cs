using System;
using System.Collections.Generic;
using System.IO;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Validation;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Common;

/// <summary>
/// Generic YAML section loader with integrated validation.
/// </summary>
public sealed class FileLoader<TDto> : IFileLoader<TDto> where TDto : class
{
    private readonly IYamlDeserializer _deserializer;
    private readonly ISectionValidator<TDto> _validator;

    public FileLoader(IYamlDeserializer deserializer, ISectionValidator<TDto> validator)
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
        var validationResult = _validator.Validate(items);
        if (validationResult.IsFailed)
            return Result.Fail<IReadOnlyList<TDto>>(validationResult.Errors);

        return Result.Ok(items);
    }

    private static Result<string> CheckPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Result.Fail(new ConfigError(
                "File path cannot be empty.",
                section: "filesystem",
                context: "path-check"));
        }

        return Result.Ok(path);
    }

    private static Result<string> CheckFileExists(string path)
    {
        if (!File.Exists(path))
        {
            return Result.Fail(new ConfigError(
                    $"Configuration file not found at: '{path}'",
                    section: "filesystem",
                    context: "existence-check")
                .WithDetail("filePath", path));
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
            return Result.Fail(new ConfigError(
                    $"Failed to read file '{path}': {ex.Message}",
                    section: "filesystem",
                    context: "read",
                    cause: ex)
                .WithDetail("filePath", path));
        }
    }
}