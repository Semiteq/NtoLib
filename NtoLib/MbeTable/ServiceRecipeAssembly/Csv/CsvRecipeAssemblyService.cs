using System;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.MbeTable.ModuleCore.Entities;
using NtoLib.MbeTable.ServiceCsv.Data;
using NtoLib.MbeTable.ServiceRecipeAssembly.Common;
using NtoLib.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

namespace NtoLib.MbeTable.ServiceRecipeAssembly.Csv;

/// <summary>
/// CSV-specific recipe assembly service.
/// </summary>
public sealed class CsvRecipeAssemblyService : ICsvRecipeAssemblyService
{
	private readonly CsvAssemblyStrategy _csvStrategy;
	private readonly AssemblyValidator _validator;
	private readonly ILogger<CsvRecipeAssemblyService> _logger;

	public CsvRecipeAssemblyService(
		CsvAssemblyStrategy csvStrategy,
		AssemblyValidator validator,
		ILogger<CsvRecipeAssemblyService> logger)
	{
		_csvStrategy = csvStrategy ?? throw new ArgumentNullException(nameof(csvStrategy));
		_validator = validator ?? throw new ArgumentNullException(nameof(validator));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public Result<Recipe> AssembleFromCsvData(object csvData)
	{
		if (csvData is not CsvRawData rawData)
			return new AssemblyInvalidDataTypeError("CsvRawData", csvData?.GetType()?.Name ?? "null");

		return AssemblyPipeline.Assemble(
			"CSV",
			_logger,
			_validator,
			() => _csvStrategy.AssembleFromRawData(rawData));
	}
}
