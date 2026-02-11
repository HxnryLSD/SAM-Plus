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

using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SAM.Core.Models;
using SAM.Core.Services;

namespace SAM.Core.ViewModels;

/// <summary>
/// ViewModel for the game picker/main view.
/// </summary>
public partial class GamePickerViewModel : ViewModelBase
{
    private readonly Func<ISteamService> _steamServiceFactory;
    private readonly Func<IImageCacheService> _imageCacheServiceFactory;
    private readonly Func<IUserDataService> _userDataServiceFactory;
    private readonly Func<IGameCacheService> _gameCacheServiceFactory;
    private ISteamService? _steamService;
    private IImageCacheService? _imageCacheService;
    private IUserDataService? _userDataService;
    private IGameCacheService? _gameCacheService;

    [ObservableProperty]
    private ObservableCollection<GameModel> _games = [];

    [ObservableProperty]
    private ObservableCollection<GameModel> _filteredGames = [];

    [ObservableProperty]
    private GameModel? _selectedGame;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private GameFilterType _filterType = GameFilterType.All;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string? _currentUserInfo;

    [ObservableProperty]
    private bool _isBackgroundRefresh;

    [ObservableProperty]
    private bool _hasCachedGames;

    public GamePickerViewModel(
        Func<ISteamService> steamServiceFactory,
        Func<IImageCacheService> imageCacheServiceFactory,
        Func<IUserDataService> userDataServiceFactory,
        Func<IGameCacheService> gameCacheServiceFactory)
    {
        _steamServiceFactory = steamServiceFactory;
        _imageCacheServiceFactory = imageCacheServiceFactory;
        _userDataServiceFactory = userDataServiceFactory;
        _gameCacheServiceFactory = gameCacheServiceFactory;
    }

    private ISteamService SteamService => _steamService ??= _steamServiceFactory();
    private IImageCacheService ImageCacheService => _imageCacheService ??= _imageCacheServiceFactory();
    private IUserDataService UserDataService => _userDataService ??= _userDataServiceFactory();
    private IGameCacheService GameCacheService => _gameCacheService ??= _gameCacheServiceFactory();

    public async Task<bool> TryLoadCachedGamesAsync(CancellationToken cancellationToken = default)
    {
        var cachedGames = await GameCacheService.GetAllGamesAsync(cancellationToken);
        if (cachedGames.Count == 0)
        {
            return false;
        }

        var games = cachedGames.Select(CreateGameModelFromCache);
        Games = new ObservableCollection<GameModel>(games);
        ApplyFilter();
        HasCachedGames = true;
        return true;
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnFilterTypeChanged(GameFilterType value)
    {
        ApplyFilter();
    }

    [RelayCommand]
    private async Task LoadGamesAsync()
    {
        await ExecuteWithBusyAsync(async (ct) =>
        {
            var useBackgroundRefresh = HasCachedGames && Games.Count > 0;
            if (useBackgroundRefresh)
            {
                IsBackgroundRefresh = true;
            }

            try
            {
                await Task.Run(() =>
                {
                    if (!SteamService.IsInitialized)
                    {
                        SteamService.Initialize();
                    }
                }, ct);

                // Set the current user from Steam ID for user data persistence
                var steamId = SteamService.SteamId.ToString();
                UserDataService.SetCurrentUser(steamId);
                Log.Info($"UserData service initialized for Steam user: {steamId}");
                CurrentUserInfo = steamId;

                // Load saved user data for all games
                var savedUserData = await UserDataService.GetAllGameDataAsync(ct);
                Log.Info($"Loaded saved data for {savedUserData.Count} games");

                var games = (await SteamService.GetOwnedGamesAsync(ct)).ToList();

                // Merge saved user data with game list
                foreach (var game in games)
                {
                    if (savedUserData.TryGetValue(game.Id, out var userData))
                    {
                        // Apply saved DRM protection info
                        game.HasDrmProtection = userData.HasDrmProtection;
                        game.ProtectedAchievementCount = userData.ProtectedAchievementCount;
                        game.DrmProtectionInfo = userData.DrmProtectionInfo;

                        // Apply saved achievement counts if available
                        if (userData.IsInitialized)
                        {
                            game.AchievementCount = userData.TotalAchievementCount;
                            game.UnlockedAchievementCount = userData.UnlockedAchievementCount;
                        }

                        Log.Debug($"Applied saved data for {game.Name} (DRM: {game.HasDrmProtection})");
                    }
                }

                Games = new ObservableCollection<GameModel>(games);
                ApplyFilter();
                HasCachedGames = false;

                await SaveGamesToCacheAsync(games, savedUserData, steamId, ct);
            }
            finally
            {
                IsBackgroundRefresh = false;
            }
        });
    }

    [RelayCommand]
    private async Task RefreshGamesAsync()
    {
        if (IsRefreshing)
        {
            return;
        }

        try
        {
            IsRefreshing = true;
            HasCachedGames = false;
            IsBackgroundRefresh = false;
            CancelOperations(); // Cancel any previous load operation
            Games.Clear();
            FilteredGames.Clear();
            await LoadGamesAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private void SelectGame(GameModel? game)
    {
        SelectedGame = game;
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    private void ApplyFilter()
    {
        var filtered = Games.AsEnumerable();

        // Apply text filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(g =>
                g.Name.ToLowerInvariant().Contains(search) ||
                g.Id.ToString().Contains(search));
        }

        // Apply type filter
        filtered = FilterType switch
        {
            GameFilterType.GamesOnly => filtered.Where(g => g.Type == GameType.Game),
            GameFilterType.ModsOnly => filtered.Where(g => g.Type == GameType.Mod),
            GameFilterType.DlcOnly => filtered.Where(g => g.Type == GameType.Dlc),
            GameFilterType.DemosOnly => filtered.Where(g => g.Type == GameType.Demo),
            _ => filtered
        };

        FilteredGames = new ObservableCollection<GameModel>(filtered);
    }

    private static GameModel CreateGameModelFromCache(CachedGameInfo cached)
    {
        return new GameModel
        {
            Id = cached.AppId,
            Name = cached.Name,
            ImageUrl = cached.ImageUrl,
            AchievementCount = cached.AchievementCount,
            UnlockedAchievementCount = cached.UnlockedCount,
            HasDrmProtection = cached.HasDrm,
            ProtectedAchievementCount = 0,
            DrmProtectionInfo = null
        };
    }

    private static CachedGameInfo CreateCachedGameInfo(
        GameModel game,
        GameUserData? userData)
    {
        return new CachedGameInfo
        {
            AppId = game.Id,
            Name = game.Name,
            AchievementCount = game.AchievementCount,
            UnlockedCount = game.UnlockedAchievementCount,
            HasDrm = game.HasDrmProtection,
            ImageUrl = game.ImageUrl,
            LastUpdated = DateTime.UtcNow,
            LastPlayed = null,
            PlaytimeMinutes = userData?.PlaytimeMinutes ?? 0
        };
    }

    private async Task SaveGamesToCacheAsync(
        IReadOnlyList<GameModel> games,
        IReadOnlyDictionary<uint, GameUserData> savedUserData,
        string steamId,
        CancellationToken cancellationToken)
    {
        if (games.Count == 0)
        {
            return;
        }

        var cachedGames = games
            .Select(game => CreateCachedGameInfo(game, savedUserData.GetValueOrDefault(game.Id)))
            .ToList();

        await GameCacheService.SaveGamesAsync(cachedGames, steamId, cancellationToken);
    }
}

public enum GameFilterType
{
    All,
    GamesOnly,
    ModsOnly,
    DlcOnly,
    DemosOnly
}
