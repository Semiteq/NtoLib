using Microsoft.Extensions.Logging.Abstractions;

using NtoLib.Recipes.MbeTable.ModuleConfig;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain;
using NtoLib.Recipes.MbeTable.ModuleConfig.Formulas;
using NtoLib.Recipes.MbeTable.ModuleCore.Formulas;

namespace NtoLib.Test.MbeTable.Core.Helpers;

public sealed class TestConfigProvider
{
	public AppConfiguration AppConfiguration { get; }
	public IReadOnlyDictionary<short, CompiledFormula> CompiledFormulas { get; }

	public TestConfigProvider(string rootFolder, string propertyDefs = "PropertyDefs.yaml",
		string columnDefs = "ColumnDefs.yaml", string pinGroupDefs = "PinGroupDefs.yaml",
		string actionsDefs = "ActionsDefs.yaml")
	{
		if (string.IsNullOrWhiteSpace(rootFolder))
			throw new ArgumentNullException(nameof(rootFolder));

		var loader = new ConfigurationLoader();
		var config = loader.LoadConfiguration(rootFolder, propertyDefs, columnDefs, pinGroupDefs, actionsDefs);

		var precompiler = new FormulaPrecompiler(NullLogger<FormulaPrecompiler>.Instance);
		var precompileResult = precompiler.Precompile(config.Actions);
		if (precompileResult.IsFailed)
			throw new InvalidOperationException("Formula precompile failed: " +
												string.Join("; ", precompileResult.Errors));

		AppConfiguration = config;
		CompiledFormulas = precompileResult.Value;
	}
}
