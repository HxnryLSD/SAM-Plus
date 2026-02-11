/* Copyright (c) 2024-2026 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using SAM.API.Types;
using SAM.API;

namespace SAM.Core.Services;

/// <summary>
/// Service for handling Steam callbacks with async support, retry logic, and timeout handling.
/// </summary>
public interface ISteamCallbackService : IDisposable
{
    /// <summary>
    /// Configuration for retry and timeout behavior.
    /// </summary>
    SteamCallbackOptions Options { get; set; }

    /// <summary>
    /// Starts the callback processing loop.
    /// </summary>
    void StartCallbackLoop();

    /// <summary>
    /// Stops the callback processing loop.
    /// </summary>
    void StopCallbackLoop();

    /// <summary>
    /// Gets whether the callback loop is running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Sets the dispatcher for running callbacks on the UI thread.
    /// This should be called before StartCallbackLoop() to ensure thread safety with Steam API.
    /// </summary>
    /// <param name="dispatcher">An action that schedules work on the UI thread.</param>
    void SetUiDispatcher(Action<Action> dispatcher);

    /// <summary>
    /// Sets the Steam client to use for callbacks.
    /// Must be called after SteamService.InitializeForGame() to get the correct client.
    /// </summary>
    void SetClient(Client client);

    /// <summary>
    /// Requests user stats from Steam and waits for the callback.
    /// </summary>
    /// <param name="steamId">The Steam user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the stats request.</returns>
    Task<UserStatsReceivedResult> RequestUserStatsAsync(ulong steamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when user stats are received.
    /// </summary>
    event EventHandler<UserStatsReceivedEventArgs>? UserStatsReceived;
}

/// <summary>
/// Configuration options for Steam callback handling.
/// </summary>
public class SteamCallbackOptions
{
    /// <summary>
    /// Default callback polling interval in milliseconds.
    /// </summary>
    public int CallbackIntervalMs { get; set; } = 100;

    /// <summary>
    /// Timeout for waiting for callbacks in milliseconds.
    /// </summary>
    public int TimeoutMs { get; set; } = 10000;

    /// <summary>
    /// Number of retry attempts for failed API calls.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Initial delay between retries in milliseconds (used for exponential backoff).
    /// </summary>
    public int RetryDelayMs { get; set; } = 500;

    /// <summary>
    /// Maximum delay between retries in milliseconds.
    /// </summary>
    public int MaxRetryDelayMs { get; set; } = 5000;
}

/// <summary>
/// Result of a user stats request.
/// </summary>
public record UserStatsReceivedResult
{
    /// <summary>
    /// Whether the request was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The Steam result code.
    /// </summary>
    public int ResultCode { get; init; }

    /// <summary>
    /// Human-readable error message if the request failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The game ID the stats belong to.
    /// </summary>
    public ulong GameId { get; init; }

    /// <summary>
    /// The Steam user ID the stats belong to.
    /// </summary>
    public ulong SteamId { get; init; }

    /// <summary>
    /// Number of retry attempts made.
    /// </summary>
    public int RetryCount { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static UserStatsReceivedResult Successful(ulong gameId, ulong steamId) => new()
    {
        Success = true,
        ResultCode = 1, // k_EResultOK
        GameId = gameId,
        SteamId = steamId
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static UserStatsReceivedResult Failed(int resultCode, string errorMessage, int retryCount = 0) => new()
    {
        Success = false,
        ResultCode = resultCode,
        ErrorMessage = errorMessage,
        RetryCount = retryCount
    };

    /// <summary>
    /// Creates a timeout result.
    /// </summary>
    public static UserStatsReceivedResult Timeout(int timeoutMs, int retryCount = 0) => new()
    {
        Success = false,
        ResultCode = -1,
        ErrorMessage = $"Request timed out after {timeoutMs}ms",
        RetryCount = retryCount
    };

    /// <summary>
    /// Creates a cancelled result.
    /// </summary>
    public static UserStatsReceivedResult Cancelled() => new()
    {
        Success = false,
        ResultCode = -2,
        ErrorMessage = "Request was cancelled"
    };
}

/// <summary>
/// Event args for user stats received.
/// </summary>
public class UserStatsReceivedEventArgs : EventArgs
{
    public UserStatsReceived Data { get; init; }
    public bool Success => Data.Result == 1;
}
