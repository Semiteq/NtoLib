using System;

using FluentAssertions;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Test.MbeTable.Core.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Core.Integration.Properties;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "PropertyParsingEdgeCases")]
public sealed class CorePropertyParsingEdgeCasesTests
{
	private const string TimeFormatFullWithMillis = "1:02:03.456";
	private const int ExpectedSecondsFromFullWithMillis = 3723;
	private const string TimeFormatSecondsOnly = "45";
	private const int ExpectedSecondsFromSecondsOnly = 45;

	[Fact]
	public void TimeFormat_FullWithMilliseconds_ParsesCorrectly()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0);

		var result = facade.UpdateProperty(0, MandatoryColumns.StepDuration, TimeFormatFullWithMillis);

		result.IsSuccess.Should().BeTrue();
		var totalSeconds = (int)facade.CurrentSnapshot.TotalDuration.TotalSeconds;
		totalSeconds.Should().Be(ExpectedSecondsFromFullWithMillis);
	}

	[Fact]
	public void TimeFormat_SecondsOnly_ParsesCorrectly()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0);

		var result = facade.UpdateProperty(0, MandatoryColumns.StepDuration, TimeFormatSecondsOnly);

		result.IsSuccess.Should().BeTrue();
		facade.CurrentSnapshot.TotalDuration.Should().Be(TimeSpan.FromSeconds(ExpectedSecondsFromSecondsOnly));
	}

	[Fact]
	public void NegativeValue_WithNonNegativeTrue_ConvertsToAbsolute()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		const float NegativeValue = -15f;
		const float ExpectedAbsolute = 15f;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0);

		var result = facade.UpdateProperty(0, MandatoryColumns.StepDuration, NegativeValue);

		result.IsSuccess.Should().BeTrue();
		facade.CurrentSnapshot.TotalDuration.Should().Be(TimeSpan.FromSeconds(ExpectedAbsolute));
	}

	[Fact]
	public void Float_WithFormatKindInt_TruncatesToInteger()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		const float InputValue = 5.7f;
		const int ExpectedTruncated = 5;

		var d = new RecipeTestDriver(facade);
		d.AddFor(0, 1);

		var result = facade.UpdateProperty(0, MandatoryColumns.Task, InputValue);

		result.IsSuccess.Should().BeTrue();
		var actualValue = facade.CurrentSnapshot.Recipe.Steps[0]
			.Properties[MandatoryColumns.Task]!
			.GetValue<float>().Value;
		actualValue.Should().Be(ExpectedTruncated);
	}

	[Fact]
	public void String_ExceedingMaxLength_Fails()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		const int MaxLength = 255;
		var longString = new string('a', MaxLength + 10);

		var d = new RecipeTestDriver(facade);
		d.AddWait(0);

		var result = facade.UpdateProperty(0, MandatoryColumns.Comment, longString);

		result.IsFailed.Should().BeTrue();
	}
}
