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

namespace SAM.Core.Models;

/// <summary>
/// Represents a Steam achievement.
/// </summary>
public partial class AchievementModel : ObservableObject
{
    public required string Id { get; init; }
    
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private string _description = string.Empty;
    
    [ObservableProperty]
    private bool _isUnlocked;
    
    [ObservableProperty]
    private DateTime? _unlockTime;
    
    [ObservableProperty]
    private string? _iconUrl;
    
    [ObservableProperty]
    private string? _iconLockedUrl;
    
    [ObservableProperty]
    private bool _isHidden;
    
    [ObservableProperty]
    private bool _isProtected;
    
    [ObservableProperty]
    private int _permission;
    
    [ObservableProperty]
    private bool _isModified;

    /// <summary>
    /// Returns the appropriate icon URL based on unlock state.
    /// </summary>
    public string? CurrentIconUrl => IsUnlocked ? IconUrl : IconLockedUrl ?? IconUrl;
}
