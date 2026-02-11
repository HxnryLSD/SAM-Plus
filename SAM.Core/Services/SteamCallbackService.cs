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

using SAM.API;
using SAM.API.Types;

namespace SAM.Core.Services;

/// <summary>
/// Implementation of Steam callback service with async support, retry logic, and timeout handling.
/// </summary>
public class SteamCallbackService : ISteamCallbackService
{
    private readonly ISteamService _steamService;
    private Client? _client;
    private API.Callbacks.UserStatsReceived? _userStatsReceivedCallback;
    private TaskCompletionSource<UserStatsReceived>? _pendingUserStatsRequest;
    private ulong _pendingUserStatsSteamId;
    
    private CancellationTokenSource? _callbackLoopCts;
    private Task? _callbackLoopTask;
    private bool _disposed;
    private SynchronizationContext? _syncContext;
    private Action<Action>? _uiDispatcher;

    public SteamCallbackOptions Options { get; set; } = new();

    public bool IsRunning => _callbackLoopTask is { IsCompleted: false };

    public event EventHandler<UserStatsReceivedEventArgs>? UserStatsReceived;

    public SteamCallbackService(ISteamService steamService)
    {
        _steamService = steamService;
    }

    /// <summary>
    /// Sets the Steam client to use for callbacks.
    /// Must be called after SteamService.InitializeForGame() to get the correct client.
    /// </summary>
    public void SetClient(Client client)
    {
        // Unregister from old client
        if (_userStatsReceivedCallback != null)
        {
            _userStatsReceivedCallback.OnRun -= OnUserStatsReceivedCallback;
            _userStatsReceivedCallback = null;
        }

        _client = client;

        // Register callback on new client
        if (_client != null)
        {
            _userStatsReceivedCallback = _client.CreateAndRegisterCallback<API.Callbacks.UserStatsReceived>();
            _userStatsReceivedCallback.OnRun += OnUserStatsReceivedCallback;
            Log.Debug("Registered UserStatsReceived callback");
        }
    }

    public void SetUiDispatcher(Action<Action> dispatcher)
    {
        _uiDispatcher = dispatcher;
        Log.Debug("UI dispatcher set");
    }

    public void StartCallbackLoop()
    {
        if (_callbackLoopTask is { IsCompleted: false })
        {
            return; // Already running
        }

        // Capture the current synchronization context (should be UI thread)
        // This is a fallback if SetUiDispatcher wasn't called
        if (_uiDispatcher == null)
        {
            _syncContext = SynchronizationContext.Current;
            if (_syncContext == null)
            {
                Log.Warn("No UI dispatcher or synchronization context available - callbacks will run on background thread (may cause issues with Steam API)");
            }
            else
            {
                Log.Debug($"Using synchronization context: {_syncContext.GetType().Name}");
            }
        }

        _callbackLoopCts = new CancellationTokenSource();
        _callbackLoopTask = RunCallbackLoopAsync(_callbackLoopCts.Token);
        Log.Debug("Started callback loop");
    }

    public void StopCallbackLoop()
    {
        if (_callbackLoopCts == null)
        {
            return;
        }

        _callbackLoopCts.Cancel();
        try
        {
            _callbackLoopTask?.Wait(1000);
        }
        catch (AggregateException)
        {
            // Ignore cancellation exceptions
        }
        
        _callbackLoopCts.Dispose();
        _callbackLoopCts = null;
        _callbackLoopTask = null;
        Log.Debug("Stopped callback loop");
    }

    private async Task RunCallbackLoopAsync(CancellationToken cancellationToken)
    {
        Log.Debug("Callback loop started");
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Run callbacks on the UI thread
                // Steam API expects to be called from the main thread that initialized it
                await RunCallbacksOnUiThreadAsync();
                
                await Task.Delay(Options.CallbackIntervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Error in callback loop");
            }
        }
        Log.Debug("Callback loop ended");
    }

    private async Task RunCallbacksOnUiThreadAsync()
    {
        // Priority: UI dispatcher > SynchronizationContext > direct call
        if (_uiDispatcher != null)
        {
            var tcs = new TaskCompletionSource<bool>();
            _uiDispatcher(() =>
            {
                try
                {
                    _client?.RunCallbacks(false);
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            await tcs.Task;
        }
        else if (_syncContext != null)
        {
            var tcs = new TaskCompletionSource<bool>();
            _syncContext.Post(_ =>
            {
                try
                {
                    _client?.RunCallbacks(false);
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, null);
            await tcs.Task;
        }
        else
        {
            // No dispatcher available, run directly (may cause issues)
            _client?.RunCallbacks(false);
        }
    }
    public async Task<UserStatsReceivedResult> RequestUserStatsAsync(ulong steamId, CancellationToken cancellationToken = default)
    {
        if (_steamService.SteamUserStats == null)
        {
            return UserStatsReceivedResult.Failed(-1, "Steam client not initialized");
        }

        int retryCount = 0;
        int currentDelay = Options.RetryDelayMs;

        while (retryCount <= Options.MaxRetries)
        {
            try
            {
                var result = await RequestUserStatsOnceAsync(steamId, cancellationToken);
                
                if (result.Success || cancellationToken.IsCancellationRequested)
                {
                    return result with { RetryCount = retryCount };
                }

                // Check if this is a retryable error
                if (!IsRetryableError(result.ResultCode))
                {
                    return result with { RetryCount = retryCount };
                }

                retryCount++;
                if (retryCount > Options.MaxRetries)
                {
                    Log.Warn($"Max retries ({Options.MaxRetries}) reached for RequestUserStats");
                    return result with { RetryCount = retryCount - 1, ErrorMessage = $"{result.ErrorMessage} (after {retryCount - 1} retries)" };
                }

                Log.Debug($"Retry {retryCount}/{Options.MaxRetries} after {currentDelay}ms delay");
                await Task.Delay(currentDelay, cancellationToken);
                
                // Exponential backoff
                currentDelay = Math.Min(currentDelay * 2, Options.MaxRetryDelayMs);
            }
            catch (OperationCanceledException)
            {
                return UserStatsReceivedResult.Cancelled();
            }
        }

        return UserStatsReceivedResult.Failed(-1, $"Failed after {Options.MaxRetries} retries", retryCount);
    }

    private async Task<UserStatsReceivedResult> RequestUserStatsOnceAsync(ulong steamId, CancellationToken cancellationToken)
    {
        if (_steamService.SteamUserStats == null)
        {
            return UserStatsReceivedResult.Failed(-1, "Steam client not initialized");
        }

        // Create completion source for this request
        _pendingUserStatsRequest = new TaskCompletionSource<UserStatsReceived>();
        _pendingUserStatsSteamId = steamId;

        try
        {
            // Make the request
            Log.Debug($"Requesting user stats for SteamId={steamId}");
            var callHandle = _steamService.SteamUserStats.RequestUserStats(steamId);
            
            if (callHandle == CallHandle.Invalid)
            {
                Log.Warn("RequestUserStats returned invalid call handle");
                return UserStatsReceivedResult.Failed(-1, "Failed to initiate stats request");
            }

            // Wait for callback with timeout
            using var timeoutCts = new CancellationTokenSource(Options.TimeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                var callbackTask = _pendingUserStatsRequest.Task;
                var completedTask = await Task.WhenAny(callbackTask, Task.Delay(Timeout.Infinite, linkedCts.Token));

                if (completedTask == callbackTask)
                {
                    var data = await callbackTask;
                    
                    if (data.Result == 1) // k_EResultOK
                    {
                        Log.Debug($"User stats received successfully for GameId={data.GameId}");
                        return UserStatsReceivedResult.Successful(data.GameId, data.SteamIdUser);
                    }
                    else
                    {
                        var errorMsg = TranslateResultCode(data.Result);
                        Log.Warn($"User stats request failed: {errorMsg} (code={data.Result})");
                        return UserStatsReceivedResult.Failed(data.Result, errorMsg);
                    }
                }
                else
                {
                    // This shouldn't happen since Delay(Infinite) never completes
                    return UserStatsReceivedResult.Timeout(Options.TimeoutMs);
                }
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                Log.Warn($"User stats request timed out after {Options.TimeoutMs}ms");
                return UserStatsReceivedResult.Timeout(Options.TimeoutMs);
            }
        }
        finally
        {
            _pendingUserStatsRequest = null;
        }
    }

    private void OnUserStatsReceivedCallback(UserStatsReceived data)
    {
        Log.Debug($"UserStatsReceived callback: GameId={data.GameId}, Result={data.Result}, SteamId={data.SteamIdUser}");
        
        // Raise event
        UserStatsReceived?.Invoke(this, new UserStatsReceivedEventArgs { Data = data });

        // Complete pending request if it matches
        if (_pendingUserStatsRequest != null && data.SteamIdUser == _pendingUserStatsSteamId)
        {
            _pendingUserStatsRequest.TrySetResult(data);
        }
    }

    private static bool IsRetryableError(int resultCode)
    {
        // Steam result codes that warrant a retry
        return resultCode switch
        {
            2 => true,  // k_EResultFail - generic failure
            3 => true,  // k_EResultNoConnection
            16 => true, // k_EResultTimeout
            20 => true, // k_EResultServiceUnavailable
            27 => true, // k_EResultTryAnotherCM
            52 => true, // k_EResultRateLimitExceeded
            _ => false
        };
    }

    public static string TranslateResultCode(int resultCode)
    {
        return resultCode switch
        {
            1 => "Success",
            2 => "Generic failure",
            3 => "No connection",
            5 => "Invalid password",
            6 => "Already logged in elsewhere",
            7 => "Invalid protocol version",
            8 => "Invalid parameter",
            9 => "File not found",
            10 => "Busy",
            11 => "Invalid state",
            12 => "Invalid name",
            13 => "Invalid email",
            14 => "Duplicate name",
            15 => "Access denied",
            16 => "Timeout",
            17 => "Banned",
            18 => "Account not found",
            19 => "Invalid Steam ID",
            20 => "Service unavailable",
            21 => "Not logged on",
            22 => "Pending",
            23 => "Encryption failure",
            24 => "Insufficient privilege",
            25 => "Limit exceeded",
            26 => "Revoked",
            27 => "Expired",
            28 => "Already redeemed",
            29 => "Duplicate request",
            30 => "Already owned",
            31 => "IP not found",
            32 => "Persist failed",
            33 => "Locking failed",
            34 => "Logon session replaced",
            35 => "Connect failed",
            36 => "Handshake failed",
            37 => "IO failure",
            38 => "Remote disconnect",
            39 => "Shopping cart not found",
            40 => "Blocked",
            41 => "Ignored",
            42 => "No match",
            43 => "Account disabled",
            44 => "Service read-only",
            45 => "Account not featured",
            46 => "Administrator OK",
            47 => "Content version",
            48 => "Try another CM",
            49 => "Password required to kick session",
            50 => "Already logged in elsewhere",
            51 => "Suspended",
            52 => "Cancelled",
            53 => "Data corruption",
            54 => "Disk full",
            55 => "Remote call failed",
            56 => "Password unset",
            57 => "External account unlinked",
            58 => "PSN ticket invalid",
            59 => "External account already linked",
            60 => "Remote file conflict",
            61 => "Illegal password",
            62 => "Same as previous value",
            63 => "Account logon denied",
            64 => "Cannot use old password",
            65 => "Invalid login auth code",
            66 => "Account logon denied no mail",
            67 => "Hardware not capable of IPT",
            68 => "IPT init error",
            69 => "Parental control restricted",
            70 => "Facebook query error",
            71 => "Expired login auth code",
            72 => "IP login restriction failed",
            73 => "Account locked down",
            74 => "Account logon denied verified email required",
            75 => "No matching URL",
            76 => "Bad response",
            77 => "Require password re-entry",
            78 => "Value out of range",
            79 => "Unexpected error",
            80 => "Disabled",
            81 => "Invalid CEG submission",
            82 => "Restricted device",
            83 => "Region locked",
            84 => "Rate limit exceeded",
            85 => "Account login denied need two factor",
            86 => "Item deleted",
            87 => "Account login denied throttle",
            88 => "Two factor code mismatch",
            89 => "Two factor activation code mismatch",
            90 => "Account associated to multiple partners",
            91 => "Not modified",
            92 => "No mobile device",
            93 => "Time not synced",
            94 => "SMS code failed",
            95 => "Account limit exceeded",
            96 => "Account activity limit exceeded",
            97 => "Phone activity limit exceeded",
            98 => "Refund to wallet",
            99 => "Email send failure",
            100 => "Not settled",
            101 => "Need captcha",
            102 => "GSLT denied",
            103 => "GS owner denied",
            104 => "Invalid item type",
            105 => "IP banned",
            106 => "GSLT expired",
            107 => "Insufficient funds",
            108 => "Too many pending",
            _ => $"Unknown error (code {resultCode})"
        };
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                StopCallbackLoop();
                
                if (_userStatsReceivedCallback != null)
                {
                    _userStatsReceivedCallback.OnRun -= OnUserStatsReceivedCallback;
                    _userStatsReceivedCallback = null;
                }
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
