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

using Microsoft.UI.Xaml.Controls;

namespace SAM.WinUI;

/// <summary>
/// Service for navigating between pages.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Gets or sets the frame used for navigation.
    /// </summary>
    Frame? Frame { get; set; }

    /// <summary>
    /// Gets whether navigation back is possible.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Gets whether navigation forward is possible.
    /// </summary>
    bool CanGoForward { get; }

    /// <summary>
    /// Navigates to the specified page type.
    /// </summary>
    bool NavigateTo(Type pageType, object? parameter = null);

    /// <summary>
    /// Navigates to the specified page type.
    /// </summary>
    bool NavigateTo<TPage>(object? parameter = null) where TPage : Page;

    /// <summary>
    /// Navigates back to the previous page.
    /// </summary>
    void GoBack();

    /// <summary>
    /// Navigates forward to the next page.
    /// </summary>
    void GoForward();
}

/// <summary>
/// Default implementation of INavigationService.
/// </summary>
public class NavigationService : INavigationService
{
    private Frame? _frame;

    public Frame? Frame
    {
        get => _frame;
        set => _frame = value;
    }

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public bool CanGoForward => _frame?.CanGoForward ?? false;

    public bool NavigateTo(Type pageType, object? parameter = null)
    {
        if (_frame is null)
        {
            return false;
        }

        return _frame.Navigate(pageType, parameter);
    }

    public bool NavigateTo<TPage>(object? parameter = null) where TPage : Page
    {
        return NavigateTo(typeof(TPage), parameter);
    }

    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
        {
            _frame.GoBack();
        }
    }

    public void GoForward()
    {
        if (_frame?.CanGoForward == true)
        {
            _frame.GoForward();
        }
    }
}
