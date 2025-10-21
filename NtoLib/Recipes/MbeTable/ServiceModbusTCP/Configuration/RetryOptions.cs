namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Configuration;

/// <summary>
/// Retry policy parameters used by Polly.
/// </summary>
/// <param name="MaxAttempts">Total number of attempts including the first try.</param>
/// <param name="BackoffDelayMs">Delay between attempts in milliseconds.</param>
public sealed record RetryOptions(int MaxAttempts, int BackoffDelayMs);