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
using SAM.Core.Services;

namespace SAM.Core.Tests.Mocks;

/// <summary>
/// Mock implementation of IAchievementService for testing.
/// </summary>
public class MockAchievementService : IAchievementService
{
    private readonly List<AchievementModel> _achievements = [];
    private readonly List<StatModel> _stats = [];
    private readonly Dictionary<string, bool> _achievementStates = new();
    private readonly Dictionary<string, object> _statValues = new();
    private bool _isReady;
    private long _gameId;

    public bool IsReady => _isReady;
    public long GameId => _gameId;

    public event EventHandler<StatsReceivedEventArgs>? StatsReceived;

    public void SetAchievements(IEnumerable<AchievementModel> achievements)
    {
        _achievements.Clear();
        _achievements.AddRange(achievements);
        foreach (var a in achievements)
        {
            _achievementStates[a.Id] = a.IsUnlocked;
        }
    }

    public void SetStats(IEnumerable<StatModel> stats)
    {
        _stats.Clear();
        _stats.AddRange(stats);
    }

    public Task InitializeAsync(long gameId, CancellationToken cancellationToken = default)
    {
        _gameId = gameId;
        _isReady = true;
        return Task.CompletedTask;
    }

    public Task<IEnumerable<AchievementModel>> GetAchievementsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<AchievementModel>>(_achievements);
    }

    public Task<IEnumerable<StatModel>> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<StatModel>>(_stats);
    }

    public bool SetAchievement(string achievementId, bool unlock)
    {
        if (_achievementStates.ContainsKey(achievementId))
        {
            _achievementStates[achievementId] = unlock;
            var achievement = _achievements.FirstOrDefault(a => a.Id == achievementId);
            if (achievement != null)
            {
                achievement.IsUnlocked = unlock;
            }
            return true;
        }
        return false;
    }

    public bool SetStatistic(string statId, object value)
    {
        _statValues[statId] = value;
        return true;
    }

    public bool StoreStats()
    {
        StatsReceived?.Invoke(this, new StatsReceivedEventArgs { Success = true });
        return true;
    }

    public bool ResetAllStats(bool includeAchievements)
    {
        _statValues.Clear();
        if (includeAchievements)
        {
            foreach (var key in _achievementStates.Keys.ToList())
            {
                _achievementStates[key] = false;
            }
            foreach (var achievement in _achievements)
            {
                achievement.IsUnlocked = false;
            }
        }
        return true;
    }

    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void RaiseStatsReceived(bool success, int resultCode = 0, string? errorMessage = null)
    {
        StatsReceived?.Invoke(this, new StatsReceivedEventArgs
        {
            Success = success,
            ResultCode = resultCode,
            ErrorMessage = errorMessage
        });
    }
}
