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

using SAM.Core.Models;

namespace SAM.Core.Services;

/// <summary>
/// Progress information for library fetch operation.
/// </summary>
public record LibraryFetchProgress
{
    /// <summary>
    /// Gets the current game being processed.
    /// </summary>
    public string CurrentGameName { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets the current game ID.
    /// </summary>
    public uint CurrentGameId { get; init; }
    
    /// <summary>
    /// Gets the current game index (1-based).
    /// </summary>
    public int CurrentIndex { get; init; }
    
    /// <summary>
    /// Gets the total number of games to process.
    /// </summary>
    public int TotalGames { get; init; }
    
    /// <summary>
    /// Gets the percentage complete (0-100).
    /// </summary>
    public double PercentComplete => TotalGames > 0 ? (double)CurrentIndex / TotalGames * 100 : 0;
    
    /// <summary>
    /// Gets whether the current game was successful.
    /// </summary>
    public bool IsSuccess { get; init; }
    
    /// <summary>
    /// Gets an error message if the current game failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Result of the library fetch operation.
/// </summary>
public class LibraryFetchResult
{
    /// <summary>
    /// Gets the total number of games processed.
    /// </summary>
    public int TotalGames { get; init; }
    
    /// <summary>
    /// Gets the number of games successfully fetched.
    /// </summary>
    public int SuccessCount { get; init; }
    
    /// <summary>
    /// Gets the number of games that failed.
    /// </summary>
    public int FailedCount { get; init; }
    
    /// <summary>
    /// Gets the number of games skipped (already up-to-date).
    /// </summary>
    public int SkippedCount { get; init; }
    
    /// <summary>
    /// Gets the total time taken.
    /// </summary>
    public TimeSpan Duration { get; init; }
    
    /// <summary>
    /// Gets whether the operation was cancelled.
    /// </summary>
    public bool WasCancelled { get; init; }
    
    /// <summary>
    /// Gets any games that failed with their error messages.
    /// </summary>
    public Dictionary<uint, string> FailedGames { get; init; } = [];
}

/// <summary>
/// Service for fetching all library game data including achievements and DRM status.
/// </summary>
public interface ILibraryFetchService
{
    /// <summary>
    /// Gets whether a fetch operation is currently in progress.
    /// </summary>
    bool IsFetching { get; }
    
    /// <summary>
    /// Gets the last fetch result, if any.
    /// </summary>
    LibraryFetchResult? LastResult { get; }
    
    /// <summary>
    /// Gets the timestamp of the last successful fetch.
    /// </summary>
    DateTime? LastFetchTime { get; }
    
    /// <summary>
    /// Fetches all library game data asynchronously.
    /// </summary>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the fetch operation.</returns>
    Task<LibraryFetchResult> FetchAllGamesAsync(
        IProgress<LibraryFetchProgress>? progress = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Event raised when the fetch operation progresses.
    /// </summary>
    event EventHandler<LibraryFetchProgress>? FetchProgress;
    
    /// <summary>
    /// Event raised when the fetch operation completes.
    /// </summary>
    event EventHandler<LibraryFetchResult>? FetchCompleted;
}
