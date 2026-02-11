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

namespace SAM.Manager;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// SAM.Manager is a single-game achievement manager that receives the game ID as a command-line argument.
/// </summary>
public partial class App : Application
{
    private static IServiceProvider? _serviceProvider;
    private Window? _window;
    
    /// <summary>
    /// The game ID to manage, received from command-line arguments.
    /// </summary>
    public static uint GameId { get; private set; }
    
    /// <summary>
    /// The game name (optional, can be passed as second argument).
    /// </summary>
    public static string? GameName { get; private set; }

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        Log.Initialize("SAM.Manager");
        Log.Method();
        
        // Parse command-line arguments FIRST, before anything else
        ParseCommandLineArguments();
        
        // Set Steam AppID environment variable BEFORE Steam client initialization
        if (GameId > 0)
        {
            Log.Info($"Setting SteamAppId environment variable to: {GameId}");
            Environment.SetEnvironmentVariable("SteamAppId", GameId.ToString());
            Environment.SetEnvironmentVariable("SteamGameId", GameId.ToString());
        }
        else
        {
            Log.Error("No GameId provided! SAM.Manager requires a game ID as command-line argument.");
        }
        
        InitializeComponent();
        
        // Configure services
        ConfigureServices();
        
        Log.MethodExit();
    }

    private void ParseCommandLineArguments()
    {
        Log.Method();
        
        var args = Environment.GetCommandLineArgs();
        Log.Info($"Command-line arguments count: {args.Length}");
        
        for (int i = 0; i < args.Length; i++)
        {
            Log.Debug($"  args[{i}]: {args[i]}");
        }
        
        // args[0] is the executable path
        // args[1] should be the GameId
        // args[2] (optional) can be the GameName
        
        if (args.Length >= 2 && uint.TryParse(args[1], out var gameId))
        {
            GameId = gameId;
            Log.Info($"GameId parsed: {GameId}");
        }
        else
        {
            Log.Error("Failed to parse GameId from command-line arguments");
            Log.Error("Usage: SAM.Manager.exe <GameId> [GameName]");
        }
        
        if (args.Length >= 3)
        {
            GameName = args[2];
            Log.Info($"GameName provided: {GameName}");
        }
        
        Log.MethodExit();
    }

    private void ConfigureServices()
    {
        Log.Method();
        
        var services = new ServiceCollection();
        
        // Add core services (Steam client, achievement service, etc.)
        services.AddSamCoreServices();
        
        // Add ViewModels as Singleton (single game, single instance)
        services.AddSingleton<AchievementManagerViewModel>();
        
        _serviceProvider = services.BuildServiceProvider();
        
        Log.MethodExit();
    }

    /// <summary>
    /// Gets a service from the dependency injection container.
    /// </summary>
    public static T GetService<T>() where T : class
    {
        if (_serviceProvider is null)
        {
            throw new InvalidOperationException("Service provider not initialized");
        }
        
        return _serviceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Log.Method();
        
        if (GameId == 0)
        {
            Log.Error("Cannot launch SAM.Manager without a valid GameId");
            // Show error and exit
            Environment.Exit(1);
            return;
        }
        
        _window = new MainWindow();
        _window.Activate();
        
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
        
        Log.Info($"SAM.Manager launched for game ID: {GameId}");
        Log.MethodExit();
    }
}
