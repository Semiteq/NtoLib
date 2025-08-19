#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Analysis;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Contracts;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Csv;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Csv.Fingerprints;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Csv.Hasher;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.RecipeFile;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Validation;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Services;

/// <summary>
/// A serializer for handling CSV-based persistence of recipe data.
/// </summary>
public sealed class RecipeCsvSerializerV1 : IRecipeSerializer
{
    private readonly TableSchema _schema;
    private readonly ActionManager _actionManager;
    private readonly ICsvHelperFactory _csvHelperFactory;
    private readonly RecipeFileMetadataSerializer _recipeFileMetadataSerializer;
    private readonly SchemaFingerprintUtil _schemaFingerprintUtil;
    private readonly IActionsFingerprintUtil _actionsFingerprintUtil;
    private readonly ICsvHeaderBinder _csvHeaderBinder;
    private readonly ICsvStepMapper _csvStepMapper;
    private readonly RecipeLoopValidator _recipeLoopValidator;
    private readonly TargetAvailabilityValidator _targetAvailabilityValidator;
    private readonly IActionTargetProvider _actionTargetProvider;

    public RecipeCsvSerializerV1(
        TableSchema schema,
        ActionManager actionManager,
        ICsvHelperFactory csvFactory,
        RecipeFileMetadataSerializer metaSerializer,
        SchemaFingerprintUtil schemaFingerprintUtil,
        IActionsFingerprintUtil actionsFingerprintUtil,
        ICsvHeaderBinder headerBinder,
        ICsvStepMapper csvStepMapper,
        RecipeLoopValidator loopValidator,
        TargetAvailabilityValidator targetsValidator,
        IActionTargetProvider targetProvider)
    {
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _actionManager = actionManager ?? throw new ArgumentNullException(nameof(actionManager));
        _csvHelperFactory = csvFactory ?? throw new ArgumentNullException(nameof(csvFactory));
        _recipeFileMetadataSerializer = metaSerializer ?? throw new ArgumentNullException(nameof(metaSerializer));
        _schemaFingerprintUtil = schemaFingerprintUtil ?? throw new ArgumentNullException(nameof(schemaFingerprintUtil));
        _actionsFingerprintUtil = actionsFingerprintUtil ?? throw new ArgumentNullException(nameof(actionsFingerprintUtil));
        _csvHeaderBinder = headerBinder ?? throw new ArgumentNullException(nameof(headerBinder));
        _csvStepMapper = csvStepMapper ?? throw new ArgumentNullException(nameof(csvStepMapper));
        _recipeLoopValidator = loopValidator ?? throw new ArgumentNullException(nameof(loopValidator));
        _targetAvailabilityValidator = targetsValidator ?? throw new ArgumentNullException(nameof(targetsValidator));
        _actionTargetProvider = targetProvider ?? throw new ArgumentNullException(nameof(targetProvider));
    }

    /// <summary>
    /// Deserializes a recipe from the provided text reader, parsing metadata, body text,
    /// and performing validation checks for the recipe structure and content integrity.
    /// </summary>
    /// <param name="reader">The text reader containing the serialized recipe data.</param>
    /// <returns>A tuple containing the deserialized recipe object if successful and a list of parsing or validation errors.</returns>
    public (Recipe? Recipe, IImmutableList<RecipeFileError> Errors) Deserialize(TextReader reader)
    {
        var fullText = reader.ReadToEnd();
        var errors = ImmutableList.CreateBuilder<RecipeFileError>();

        // Metadata
        var (meta, metaLines, signatureFound, versionFound) = _recipeFileMetadataSerializer.ReadAllMeta(fullText);

        if (!signatureFound)
            errors.Add(new RecipeFileError(0, null, "Missing or invalid signature line (expected '# MBE-RECIPE v=...')"));

        if (!versionFound)
            errors.Add(new RecipeFileError(0, null, "Missing version in signature line (expected 'v=...')"));
        else if (meta.Version != 1)
            errors.Add(new RecipeFileError(0, null, $"Unsupported version {meta.Version}"));

        if (errors.Count > 0) return (null, errors.ToImmutable());

        // Body as text
        var bodyText = ExtractBodyText(fullText, metaLines);

        // CSV parsing (header + data)
        using var csv = _csvHelperFactory.CreateReader(new StringReader(bodyText));
        if (!csv.Read())
            return (null, errors.ToImmutable().Add(new RecipeFileError(0, null, "Missing header")));

        csv.ReadHeader();
        var headerTokens = csv.HeaderRecord ?? Array.Empty<string>();
        var (binding, bindErr) = _csvHeaderBinder.Bind(headerTokens, _schema);
        if (bindErr is not null)
            return (null, errors.ToImmutable().Add(new RecipeFileError(1, null, bindErr)));

        // Check separator
        if (meta.Separator != _csvHelperFactory.Separator)
            return (null,
                errors.ToImmutable().Add(new RecipeFileError(0, null,
                    $"Separator mismatch: meta='{meta.Separator}' vs expected='{_csvHelperFactory.Separator}'")));

        // Read rows and compute hash on the fly to avoid storing all rows in memory
        var steps = ImmutableList.CreateBuilder<Step>();
        var bodyHasher = new BodyIntegrityHasher();
        var rowsCount = 0;
        var lineNo = 1; // header

        while (csv.Read())
        {
            lineNo++;
            var record = csv.Context.Reader.Parser.Record ?? Array.Empty<string>();

            // Canonicalize the row exactly as we write it (same delimiter/quoting/newline rules)
            var canonicalRow = BuildCanonicalRowLine(record);
            bodyHasher.AppendDataRow(canonicalRow);
            rowsCount++;

            var (step, rowErrors) = _csvStepMapper.FromRecord(lineNo, record, binding!);

            if (rowErrors.Count > 0)
            {
                errors.AddRange(rowErrors);
                return (null, errors.ToImmutable());
            }

            if (step is not null) steps.Add(step);
        }

        var recipe = new Recipe(steps.ToImmutable());

        // Check consistency ROWS + HASH
        if (meta.Rows != 0 && meta.Rows != rowsCount)
            return (null,
                errors.ToImmutable().Add(new RecipeFileError(0, null,
                    $"Rows mismatch: meta={meta.Rows}, actual={rowsCount}")));

        if (!string.IsNullOrWhiteSpace(meta.BodyHashBase64))
        {
            var computed = bodyHasher.ComputeBase64();
            if (!string.Equals(computed, meta.BodyHashBase64, StringComparison.Ordinal))
                return (null, errors.ToImmutable().Add(new RecipeFileError(0, null, "Body hash mismatch")));
        }

        // Chek fingerprint
        var expectedSchemaFp =
            _schemaFingerprintUtil.ComputeSha256Base64(_schemaFingerprintUtil.BuildNormalized(_schema));
        if (!string.IsNullOrEmpty(meta.SchemaFingerprint) &&
            !string.Equals(expectedSchemaFp, meta.SchemaFingerprint, StringComparison.Ordinal))
            return (null, errors.ToImmutable().Add(new RecipeFileError(0, null, "Schema fingerprint mismatch")));

        var expectedActionsFp = _actionsFingerprintUtil.Compute(_actionManager);
        if (!string.IsNullOrEmpty(meta.ActionsFingerprint) &&
            !string.Equals(expectedActionsFp, meta.ActionsFingerprint, StringComparison.Ordinal))
            return (null, errors.ToImmutable().Add(new RecipeFileError(0, null, "Actions fingerprint mismatch")));

        // Validation
        var loopRes = _recipeLoopValidator.Validate(recipe);
        if (!loopRes.IsValid)
            return (null,
                errors.ToImmutable()
                    .Add(new RecipeFileError(0, null, loopRes.ErrorMessage ?? "Loop structure invalid")));

        var (okTargets, tgtErr) = _targetAvailabilityValidator.Validate(recipe, _actionManager, _actionTargetProvider);
        if (!okTargets)
            return (null, errors.ToImmutable().Add(new RecipeFileError(0, null, tgtErr ?? "Missing targets")));

        return (recipe, errors.ToImmutable());
    }

    /// <summary>
    /// Serializes the given recipe into CSV format and writes it to the provided text writer.
    /// This includes generating metadata, computing body integrity hashes, and formatting
    /// the data into a canonical structure for export.
    /// </summary>
    /// <param name="recipe">The recipe to be serialized into CSV format.</param>
    /// <param name="writer">The text writer to which the serialized CSV data will be written.</param>
    /// <returns>A list of errors encountered during the serialization process, if any.</returns>
    public IImmutableList<RecipeFileError> Serialize(Recipe recipe, TextWriter writer)
    {
        var (bodyText, canonicalRowsCount) = BuildBodyText(recipe);

        // Compute BODY_SHA256 over the data rows (after header)
        var dataOnly = ExtractDataOnly(bodyText);
        var hasher = new BodyIntegrityHasher();
        if (!string.IsNullOrEmpty(dataOnly))
        {
            foreach (var row in SplitRows(dataOnly))
                hasher.AppendDataRow(row);
        }

        var bodyHash = hasher.ComputeBase64();

        var meta = new RecipeFileMetadata
        {
            Signature = "MBE-RECIPE",
            Version = 1,
            Separator = _csvHelperFactory.Separator,
            SchemaFingerprint =
                _schemaFingerprintUtil.ComputeSha256Base64(_schemaFingerprintUtil.BuildNormalized(_schema)),
            ActionsFingerprint = _actionsFingerprintUtil.Compute(_actionManager),
            Rows = canonicalRowsCount,
            BodyHashBase64 = bodyHash,
            Extras = new Dictionary<string, string> { ["ExportedAtUtc"] = DateTime.UtcNow.ToString("O") }
        };

        _recipeFileMetadataSerializer.Write(writer, meta);
        writer.Write(bodyText);

        return ImmutableList<RecipeFileError>.Empty;
    }

    /// <summary>
    /// Extracts the body text by removing the specified number of metadata lines
    /// from the beginning of the input text while preserving the remaining content.
    /// </summary>
    /// <param name="fullText">The full text containing both metadata and body content.</param>
    /// <param name="metaLines">The number of metadata lines to exclude from the beginning of the text.</param>
    /// <returns>The body text after removing the specified metadata lines.</returns>
    private string ExtractBodyText(string fullText, int metaLines)
    {
        using var sr = new StringReader(fullText);
        var sb = new StringBuilder();

        for (var i = 0; i < metaLines; i++)
            sr.ReadLine(); // skip meta

        string? line;
        var first = true;

        while ((line = sr.ReadLine()) != null)
        {
            if (!first) sb.Append('\n');
            sb.Append(line);
            first = false;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds the body text of a recipe by generating a CSV format representation
    /// of its structured data, including headers and rows, based on the recipe's steps.
    /// </summary>
    /// <param name="recipe">The recipe containing steps to be serialized into body text.</param>
    /// <returns>A tuple containing the generated CSV body text and the total number of rows written.</returns>
    private (string BodyText, int RowsCount) BuildBodyText(Recipe recipe)
    {
        var bodySb = new StringBuilder();

        using var bodySw = new StringWriter(bodySb);
        using var csv = _csvHelperFactory.CreateWriter(bodySw);

        var orderedCols = _schema.GetColumns()
            .Where(c => c.ReadOnly == false)
            .OrderBy(c => c.Index)
            .ToArray();

        // Header
        foreach (var c in orderedCols) csv.WriteField(c.Code);
        csv.NextRecord();

        // Rows
        var rows = 0;
        foreach (var step in recipe.Steps)
        {
            var cells = _csvStepMapper.ToRecord(step, orderedCols);
            foreach (var field in cells) csv.WriteField(field);
            csv.NextRecord();
            rows++;
        }

        return (bodySb.ToString(), rows);
    }

    /// <summary>
    /// Extracts only the data block (excluding the header) from the provided body text while retaining the trailing CRLF.
    /// </summary>
    /// <param name="bodyText">The body text containing both header and data from which the data block will be extracted.</param>
    /// <returns>A string representing the extracted data block with rows separated by CRLF and ending with CRLF, or an empty string if no data block is found.</returns>
    private string ExtractDataOnly(string bodyText)
    {
        var nlIdx = bodyText.IndexOf("\r\n", StringComparison.Ordinal);
        if (nlIdx < 0) return string.Empty;
        return bodyText.Substring(nlIdx + 2);
    }

    /// <summary>
    /// Splits the provided CSV data into individual rows, based on the standard end-of-line character sequence.
    /// </summary>
    /// <param name="dataOnly">The CSV-formatted string containing data rows separated by '\r\n'.</param>
    /// <returns>An enumerable collection of individual rows as strings, excluding the final empty row if present.</returns>
    private IEnumerable<string> SplitRows(string dataOnly)
    {
        var parts = dataOnly.Split(new[] { "\r\n" }, StringSplitOptions.None);
        for (var i = 0; i < parts.Length - 1; i++)
            yield return parts[i];
    }

    /// <summary>
    /// Constructs a canonical row string from the given array of record fields using CSV formatting.
    /// </summary>
    /// <param name="record">An array of strings representing the fields of a single CSV record.</param>
    /// <returns>A string containing the CSV-formatted row constructed from the provided fields, with any trailing newline or carriage return characters removed.</returns>
    private string BuildCanonicalRowLine(string[] record)
    {
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        using var w = _csvHelperFactory.CreateWriter(sw);
        foreach (var f in record) w.WriteField(f);
        w.NextRecord();
        return sb.ToString().TrimEnd('\r', '\n');
    }
}