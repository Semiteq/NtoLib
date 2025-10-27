using System;
using System.IO;
using System.Net.Sockets;

using EasyModbus.Exceptions;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Transport;

internal static class PollyPolicyFactory
{
    public static AsyncRetryPolicy CreateConnectionPolicy(int maxAttempts, int backoffDelayMs, ILogger logger)
    {
        if (maxAttempts < 1) throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        if (backoffDelayMs < 0) throw new ArgumentOutOfRangeException(nameof(backoffDelayMs));

        return Policy
            .Handle<IOException>()
            .Or<SocketException>()
            .Or<ConnectionException>()
            .Or<ModbusException>()
            .WaitAndRetryAsync(
                retryCount: Math.Max(0, maxAttempts - 1),
                sleepDurationProvider: _ => TimeSpan.FromMilliseconds(backoffDelayMs),
                onRetry: (ex, _, retryNo, _) =>
                    logger.LogDebug("Connection retry {RetryNo}/{MaxAttempts} [{ExceptionType}]: {Message}",
                        retryNo, maxAttempts, ex.GetType().Name, ex.Message));
    }

    public static AsyncRetryPolicy CreateOperationPolicy(int maxAttempts, int backoffDelayMs, ILogger logger)
    {
        if (maxAttempts < 1) throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        if (backoffDelayMs < 0) throw new ArgumentOutOfRangeException(nameof(backoffDelayMs));

        return Policy
            .Handle<IOException>()
            .Or<SocketException>()
            .Or<ConnectionException>()
            .WaitAndRetryAsync(
                retryCount: Math.Max(0, maxAttempts - 1),
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(backoffDelayMs * attempt),
                onRetry: (ex, delay, retryNo, _) =>
                    logger.LogDebug("Operation retry {RetryNo}/{MaxAttempts} after {Delay}ms [{ExceptionType}]: {Message}",
                        retryNo, maxAttempts, delay.TotalMilliseconds, ex.GetType().Name, ex.Message));
    }
}