using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Test.MbeTable.Core.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Core.Integration.Formulas;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "FormulasNegative")]
public sealed class CoreFormulaNegativeTests
{
	private const string TemperatureRampActionName = "t°C плавно";
	private const string PowerRampActionName = "P% плавно";
	private const string CloseActionName = "Закрыть";
	private const float ZeroSpeed = 0f;

	[Fact]
	public void DivisionByZero_InFormula_Fails()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var repo = services.GetRequiredService<IActionRepository>();
		var actionId = ActionNameHelper.GetActionIdOrThrow(repo, TemperatureRampActionName);

		var d = new RecipeTestDriver(facade);
		d.AddDefaultStep(0).ReplaceAction(0, actionId);

		var result = facade.UpdateProperty(0, new ColumnIdentifier("speed"), ZeroSpeed);

		result.IsFailed.Should().BeTrue();
	}

	[Fact]
	public void UpdateNonFormulaColumn_DoesNotTriggerRecalculation()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var repo = services.GetRequiredService<IActionRepository>();
		var actionId = ActionNameHelper.GetActionIdOrThrow(repo, TemperatureRampActionName);

		var d = new RecipeTestDriver(facade);
		d.AddDefaultStep(0).ReplaceAction(0, actionId);

		var durationBefore = facade.CurrentSnapshot.Recipe.Steps[0]
			.Properties[MandatoryColumns.StepDuration]!
			.GetValue<float>().Value;

		facade.UpdateProperty(0, MandatoryColumns.Comment, "test comment");

		var durationAfter = facade.CurrentSnapshot.Recipe.Steps[0]
			.Properties[MandatoryColumns.StepDuration]!
			.GetValue<float>().Value;

		durationAfter.Should().Be(durationBefore);
	}

	[Fact]
	public void ActionWithoutFormula_UpdateProperty_NoRecalculation()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var repo = services.GetRequiredService<IActionRepository>();
		var actionId = ActionNameHelper.GetActionIdOrThrow(repo, CloseActionName);

		var d = new RecipeTestDriver(facade);
		d.AddDefaultStep(0).ReplaceAction(0, actionId);

		var result = facade.UpdateProperty(0, new ColumnIdentifier("target"), (short)2);

		result.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public void FormulaRecalculation_MultipleVariables_OnlyTargetChanges()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var repo = services.GetRequiredService<IActionRepository>();
		var actionId = ActionNameHelper.GetActionIdOrThrow(repo, PowerRampActionName);

		var d = new RecipeTestDriver(facade);
		d.AddDefaultStep(0).ReplaceAction(0, actionId);

		var taskBefore = facade.CurrentSnapshot.Recipe.Steps[0]
			.Properties[MandatoryColumns.Task]!
			.GetValue<float>().Value;

		facade.UpdateProperty(0, new ColumnIdentifier("speed"), 2f);

		var taskAfter = facade.CurrentSnapshot.Recipe.Steps[0]
			.Properties[MandatoryColumns.Task]!
			.GetValue<float>().Value;

		taskAfter.Should().Be(taskBefore);
	}
}
