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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using SAM.Core;
using SAM.Core.Services;
using SAM.Core.ViewModels;
using SAM.WinUI.Services;

namespace SAM.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _mainWindow;
    private CancellationTokenSource? _appLifetimeCts;

    /// <summary>
    /// Gets the current App instance.
    /// </summary>
    public static new App Current => (App)Application.Current;

    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// Gets the main window of the application.
    /// </summary>
    public Window? MainWindow => _mainWindow;

    /// <summary>
    /// Gets a cancellation token that is cancelled when the app shuts down.
    /// </summary>
    public CancellationToken AppShutdownToken => _appLifetimeCts?.Token ?? CancellationToken.None;

    /// <summary>
    /// Initializes a new instance of the App class.
    /// </summary>
    public App()
    {
        try
        {
            _appLifetimeCts = new CancellationTokenSource();
            Log.Initialize("SAM.WinUI");
            Services = ConfigureServices();
            InitializeComponent();
        
            // Handle unhandled exceptions
            UnhandledException += App_UnhandledException;
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SAM_CRASH.txt"),
                $"App constructor crash: {ex}");
            throw;
        }
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // Log the exception (for debugging)
        System.Diagnostics.Debug.WriteLine($"Unhandled exception: {e.Exception}");
        e.Handled = true;
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            _mainWindow = new MainWindow();
            _mainWindow.Closed += OnMainWindowClosed;
            _mainWindow.Activate();

            // Set up UI dispatcher for Steam callback service
            // Steam API requires callbacks to be processed on the main thread
            var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            if (dispatcherQueue != null)
            {
                var callbackService = GetService<ISteamCallbackService>();
                callbackService.SetUiDispatcher(action => dispatcherQueue.TryEnqueue(() => action()));
                Log.Debug("UI dispatcher configured for Steam callback service");
            }
            else
            {
                Log.Warn("Could not get DispatcherQueue for Steam callbacks");
            }

            // Initialize services after window is created
            _ = InitializeServicesAsync(AppShutdownToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Launch exception: {ex}");
            throw;
        }
    }

    private void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        // Cancel all ongoing operations when the main window closes
        _appLifetimeCts?.Cancel();
        _appLifetimeCts?.Dispose();
        _appLifetimeCts = null;
    }

    private async Task InitializeServicesAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Initialize localization
            var localizationService = GetService<ILocalizationService>();
            Loc.Initialize(localizationService);
            
            var themeService = GetService<IThemeService>();
            await themeService.InitializeAsync(cancellationToken);
            
            if (!cancellationToken.IsCancellationRequested)
            {
                themeService.SetTheme(themeService.CurrentTheme);
            }

            await CheckForUpdatesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // App is shutting down, ignore
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Service initialization exception: {ex}");
        }
    }

    private async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var settingsService = GetService<ISettingsService>();
            if (!settingsService.AutoUpdateEnabled)
            {
                return;
            }

            var updateService = GetService<IUpdateService>();
            var currentVersion = GetCurrentVersion();
            var result = await updateService.CheckForUpdateAsync(currentVersion, cancellationToken);

            if (result.IsUpdateAvailable && result.LatestVersion != null)
            {
                var notificationService = GetService<INotificationService>();
                notificationService.ShowInfo(
                    "Update available",
                    $"Version {result.LatestVersion} is available on GitHub.");
            }
        }
        catch (OperationCanceledException)
        {
            // App is shutting down, ignore
        }
        catch (Exception ex)
        {
            Log.Warn($"Update check failed: {ex.Message}");
        }
    }

    private static string GetCurrentVersion()
    {
        var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
        return version?.ToString() ?? "8.0.0";
    }

    /// <summary>
    /// Configures the services for dependency injection.
    /// </summary>
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Add SAM.Core services
        services.AddSamCoreServices();

        // Add WinUI-specific services
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<INotificationService, NotificationService>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Gets a service from the DI container.
    /// </summary>
    public static T GetService<T>() where T : class
    {
        var service = Current.Services.GetService<T>();
        return service ?? throw new InvalidOperationException($"Service {typeof(T).Name} not found.");
    }

    /// <summary>
    /// Gets a service from the DI container, or null if not found.
    /// </summary>
    public static T? TryGetService<T>() where T : class
    {
        return Current.Services.GetService<T>();
    }
}
