using FluentAssertions;
using FluentAssertions.Extensibility;

[assembly: AssertionEngineInitializer(
	typeof(AssertionEngineInitializer),
	nameof(AssertionEngineInitializer.AcknowledgeSoftWarning))]

public static class AssertionEngineInitializer
{
	public static void AcknowledgeSoftWarning()
	{
		License.Accepted = true;
	}
}
