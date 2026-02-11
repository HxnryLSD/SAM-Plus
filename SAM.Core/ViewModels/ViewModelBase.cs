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

using CommunityToolkit.Mvvm.ComponentModel;
using SAM.Core.Services;
using SAM.Core.Utilities;

namespace SAM.Core.ViewModels;

/// <summary>
/// Base class for all ViewModels providing common functionality.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    private CancellationTokenSource? _operationCts;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    partial void OnErrorMessageChanged(string? value)
    {
        HasError = !string.IsNullOrEmpty(value);
    }

    protected void SetError(string? message)
    {
        ErrorMessage = message;
    }

    protected void ClearError()
    {
        ErrorMessage = null;
    }

    /// <summary>
    /// Cancels any ongoing operations.
    /// </summary>
    public void CancelOperations()
    {
        _operationCts?.Cancel();
    }

    /// <summary>
    /// Gets a new CancellationToken for the current operation.
    /// Cancels any previous operation.
    /// </summary>
    protected CancellationToken GetOperationCancellationToken()
    {
        _operationCts?.Cancel();
        _operationCts?.Dispose();
        _operationCts = new CancellationTokenSource();
        return _operationCts.Token;
    }

    protected async Task ExecuteWithBusyAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            Log.Debug("ExecuteWithBusyAsync skipped - already busy");
            return;
        }

        // Use provided token or create a new one
        var token = cancellationToken == default ? GetOperationCancellationToken() : cancellationToken;

        try
        {
            IsBusy = true;
            ClearError();
            Log.Debug("ExecuteWithBusyAsync starting action...");
            await action(token);
            Log.Debug("ExecuteWithBusyAsync action completed successfully");
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            Log.Debug("ExecuteWithBusyAsync: Operation was cancelled");
            // Don't set error for expected cancellation
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "ExecuteWithBusyAsync");
            SetError(SteamErrorHelper.GetUserFriendlyMessage(ex));
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected async Task<T?> ExecuteWithBusyAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return default;
        }

        // Use provided token or create a new one
        var token = cancellationToken == default ? GetOperationCancellationToken() : cancellationToken;

        try
        {
            IsBusy = true;
            ClearError();
            return await action(token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            Log.Debug("ExecuteWithBusyAsync<T>: Operation was cancelled");
            return default;
        }
        catch (Exception ex)
        {
            SetError(SteamErrorHelper.GetUserFriendlyMessage(ex));
            return default;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
