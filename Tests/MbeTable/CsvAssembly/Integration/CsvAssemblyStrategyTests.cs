using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ServiceCsv.Data;
using NtoLib.Recipes.MbeTable.ServiceCsv.Parsing;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Csv;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Warnings;

using Tests.MbeTable.Core.Helpers;

using Xunit;

namespace Tests.MbeTable.CsvAssembly.Integration;

[Trait("Category", "Integration")]
[Trait("Component", "CsvAssembly")]
[Trait("Area", "ColumnMismatch")]
public sealed class CsvAssemblyStrategyTests
{
	private static readonly string[] _csvHeaders =
	{
		"action", "target", "initial_value", "task", "speed", "step_duration", "comment"
	};

	private static CsvAssemblyStrategy BuildStrategy(IServiceProvider services)
	{
		var actionRepository = services.GetRequiredService<ActionRepository>();
		var propertyRegistry = services.GetRequiredService<PropertyDefinitionRegistry>();
		var columns = services.GetRequiredService<IReadOnlyList<ColumnDefinition>>();
		var headerBinder = new CsvHeaderBinder();
		var logger = NullLoggerFactory.Instance.CreateLogger<CsvAssemblyStrategy>();

		return new CsvAssemblyStrategy(actionRepository, propertyRegistry, columns, headerBinder, logger);
	}

	private static CsvRawData BuildRawData(params string[][] records)
	{
		return new CsvRawData
		{
			Headers = _csvHeaders,
			Records = records.ToList(),
			Rows = records.Select(r => string.Join(";", r)).ToList()
		};
	}

	[Fact]
	public void EmptyCellsForApplicableColumns_FillsWithYamlDefaults()
	{
		// Action 110 ("t C плавно") uses: target, initial_value, task, speed, step_duration, comment
		// YAML defaults: initial_value=500, task=600, speed=10, step_duration=600
		// Provide only action ID and target, leave the rest empty.
		var (services, _) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var strategy = BuildStrategy(services);
		var rawData = BuildRawData(new[] { "110", "1", "", "", "", "", "" });

		var result = strategy.AssembleFromRawData(rawData);

		result.IsSuccess.Should().BeTrue("empty cells should be filled with YAML defaults");
		result.Value.Steps.Should().HaveCount(1);

		var step = result.Value.Steps[0];
		var initialValue = step.Properties[new ColumnIdentifier("initial_value")];
		var task = step.Properties[new ColumnIdentifier("task")];
		var speed = step.Properties[new ColumnIdentifier("speed")];
		var stepDuration = step.Properties[new ColumnIdentifier("step_duration")];

		initialValue.Should().NotBeNull();
		task.Should().NotBeNull();
		speed.Should().NotBeNull();
		stepDuration.Should().NotBeNull();
	}

	[Fact]
	public void NonApplicableColumn_WithNonEmptyValue_ProducesWarning_NotError()
	{
		// Action 10 ("Ждать") uses only: step_duration, comment
		// Providing a value in "target" (non-applicable) should produce a warning, not an error.
		var (services, _) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var strategy = BuildStrategy(services);
		// action=10, target=1(extra), initial_value=, task=, speed=, step_duration=10, comment=
		var rawData = BuildRawData(new[] { "10", "1", "", "", "", "10", "" });

		var result = strategy.AssembleFromRawData(rawData);

		result.IsSuccess.Should().BeTrue("extra value should produce a warning, not an error");
		result.Value.Steps.Should().HaveCount(1);

		var warning = result.Successes.OfType<AssemblyColumnNotApplicableWarning>().FirstOrDefault();
		warning.Should().NotBeNull("a warning about the extra column value should be present");
		warning!.Occurrences.Should().HaveCount(1);
		warning.Occurrences[0].ColumnCode.Should().Be("target");
		warning.Occurrences[0].ActionId.Should().Be(10);
		warning.Occurrences[0].Value.Should().Be("1");
		warning.Occurrences[0].LineNumber.Should().Be(1);
	}

	[Fact]
	public void NonApplicableColumn_WithEmptyValue_ProducesNoWarning()
	{
		// Action 10 ("Ждать") uses only: step_duration, comment
		// Empty cells in non-applicable columns should produce no warning.
		var (services, _) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var strategy = BuildStrategy(services);
		// action=10, target=, initial_value=, task=, speed=, step_duration=10, comment=
		var rawData = BuildRawData(new[] { "10", "", "", "", "", "10", "" });

		var result = strategy.AssembleFromRawData(rawData);

		result.IsSuccess.Should().BeTrue();
		result.Value.Steps.Should().HaveCount(1);

		var warning = result.Successes.OfType<AssemblyColumnNotApplicableWarning>().FirstOrDefault();
		warning.Should().BeNull("empty cells in non-applicable columns should not generate warnings");
	}

	[Fact]
	public void MultipleRows_WithExtraValues_ProduceSingleSummaryWarning()
	{
		// Three rows of action 10 ("Ждать"), each with an extra value in a different non-applicable column.
		var (services, _) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var strategy = BuildStrategy(services);
		var rawData = BuildRawData(
			new[] { "10", "1", "", "", "", "10", "" }, // row 2: extra "target"
			new[] { "10", "", "500", "", "", "10", "" }, // row 3: extra "initial_value"
			new[] { "10", "", "", "100", "", "10", "" } // row 4: extra "task"
		);

		var result = strategy.AssembleFromRawData(rawData);

		result.IsSuccess.Should().BeTrue("extra values should produce warnings, not errors");
		result.Value.Steps.Should().HaveCount(3);

		var warning = result.Successes.OfType<AssemblyColumnNotApplicableWarning>().FirstOrDefault();
		warning.Should().NotBeNull("a summary warning should be present");
		warning!.Occurrences.Should().HaveCount(3);

		warning.Occurrences[0].LineNumber.Should().Be(1);
		warning.Occurrences[0].ColumnCode.Should().Be("target");

		warning.Occurrences[1].LineNumber.Should().Be(2);
		warning.Occurrences[1].ColumnCode.Should().Be("initial_value");

		warning.Occurrences[2].LineNumber.Should().Be(3);
		warning.Occurrences[2].ColumnCode.Should().Be("task");
	}

	[Fact]
	public void MixedRows_OnlySomeWithExtraValues_AssemblesAllSteps()
	{
		// Mix of rows: some with extra values, some without.
		var (services, _) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var strategy = BuildStrategy(services);
		var rawData = BuildRawData(
			new[] { "10", "", "", "", "", "10", "" }, // row 2: clean (Ждать)
			new[] { "10", "1", "", "", "", "10", "" }, // row 3: extra "target"
			new[] { "50", "1", "", "", "", "", "" }, // row 4: clean (Закрыть with target)
			new[] { "10", "", "", "99", "", "10", "" } // row 5: extra "task"
		);

		var result = strategy.AssembleFromRawData(rawData);

		result.IsSuccess.Should().BeTrue();
		result.Value.Steps.Should().HaveCount(4);

		var warning = result.Successes.OfType<AssemblyColumnNotApplicableWarning>().FirstOrDefault();
		warning.Should().NotBeNull();
		warning!.Occurrences.Should().HaveCount(2);

		warning.Occurrences[0].LineNumber.Should().Be(2);
		warning.Occurrences[1].LineNumber.Should().Be(4);
	}

	[Fact]
	public void FullyApplicableRow_ProducesNoWarning()
	{
		// Action 110 ("t C плавно") uses all columns. No extra values possible.
		var (services, _) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var strategy = BuildStrategy(services);
		var rawData = BuildRawData(
			new[] { "110", "1", "500", "600", "10", "600", "test comment" }
		);

		var result = strategy.AssembleFromRawData(rawData);

		result.IsSuccess.Should().BeTrue();
		result.Value.Steps.Should().HaveCount(1);

		var warning = result.Successes.OfType<AssemblyColumnNotApplicableWarning>().FirstOrDefault();
		warning.Should().BeNull("all columns are applicable, no warnings expected");
	}
}
