using System;

namespace NtoLib.Recipes.MbeTable.Utilities;

/// <summary>
/// Centralizes safe cleanup operations (event unsubscription, disposal)
/// that must not throw during teardown.
/// </summary>
internal static class SafeDisposal
{
	/// <summary>
	/// Executes each action in isolation, swallowing any exceptions.
	/// Useful for event unsubscription and cleanup sequences where
	/// failure of one action must not prevent subsequent actions.
	/// </summary>
	public static void RunAll(params Action[] actions)
	{
		foreach (var action in actions)
		{
			try
			{
				action();
			}
			catch
			{
				// Swallowed: cleanup actions must not propagate exceptions.
			}
		}
	}

	/// <summary>
	/// Safely disposes an <see cref="IDisposable"/> instance, swallowing any exceptions.
	/// </summary>
	public static void TryDispose(IDisposable? disposable)
	{
		try
		{
			disposable?.Dispose();
		}
		catch
		{
			// Swallowed: disposal must not propagate exceptions during teardown.
		}
	}
}
