/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http; // Added
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks; // Added
using System.Windows.Forms;
using static SAM.Game.InvariantShorthand;
using APITypes = SAM.API.Types;

namespace SAM.Game
{
    internal partial class Manager : Form
    {
        // P/Invoke for scrollbar management
        [DllImport("user32.dll")]
        private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int SB_HORZ = 0;
        private const int SB_VERT = 1;
        private const int SB_BOTH = 3;
        private const int GWL_STYLE = -16;
        private const int WS_VSCROLL = 0x00200000;
        private const int WS_HSCROLL = 0x00100000;

        private readonly long _GameId;
        private readonly API.Client _SteamClient;
        private readonly API.StoreTitleBar _titleBar;
        private readonly API.StoreScrollBar _achievementScrollBar;
        private readonly Label _infoLabel;
        private bool _hasProtectedStats = false;

        private static System.Net.Http.HttpClient HttpClient => API.ServiceLocator.HttpClient;
        private bool _isDownloadingIcons = false; // Added flag
        private static readonly SemaphoreSlim _downloadSemaphore = new(5); // Max 5 parallel downloads

        private readonly ConcurrentQueue<Stats.AchievementInfo> _IconQueue = new();
        private readonly List<Stats.StatDefinition> _StatDefinitions = new();

        private readonly List<Stats.AchievementDefinition> _AchievementDefinitions = new();

        private readonly BindingList<Stats.StatInfo> _Statistics = new();

        private readonly API.Callbacks.UserStatsReceived _UserStatsReceivedCallback;

        //private API.Callback<APITypes.UserStatsStored> UserStatsStoredCallback;

        public Manager(long gameId, API.Client client)
        {
            this.InitializeComponent();

            // Responsive layout settings
            this.MinimumSize = new Size(640, 480);
            this.DoubleBuffered = true;

            // Setup custom title bar
            this.FormBorderStyle = FormBorderStyle.None;
            _titleBar = new API.StoreTitleBar();
            _titleBar.Title = this.Text;
            if (this.Icon != null)
            {
                _titleBar.TitleIcon = this.Icon.ToBitmap();
            }
            
            // Important: Add title bar and set correct Z-order for docking
            this.Controls.Add(_titleBar);
            _titleBar.Dock = DockStyle.None; // Disable dock for manual positioning with resize border
            _titleBar.SendToBack();

            // Hide old ToolStrip and StatusStrip for modern look (like GamePicker)
            this._MainToolStrip.Visible = false;
            this._MainStatusStrip.Visible = false;

            // Create modern action buttons panel
            var actionPanel = new Panel
            {
                Height = 36,
                BackColor = API.StoreThemeColors.Background
            };
            
            var refreshBtn = CreateActionButton("🔄 Refresh", (s, e) => OnRefresh(s, e));
            refreshBtn.Location = new Point(4, 4);
            actionPanel.Controls.Add(refreshBtn);
            
            var unlockBtn = CreateActionButton("🔓 Unlock All", (s, e) => OnUnlockAll(s, e));
            unlockBtn.Location = new Point(refreshBtn.Right + 8, 4);
            actionPanel.Controls.Add(unlockBtn);
            
            var lockBtn = CreateActionButton("🔒 Lock All", (s, e) => OnLockAll(s, e));
            lockBtn.Location = new Point(unlockBtn.Right + 8, 4);
            actionPanel.Controls.Add(lockBtn);
            
            var commitBtn = CreateActionButton("✓ Commit", (s, e) => OnStore(s, e));
            commitBtn.Location = new Point(lockBtn.Right + 8, 4);
            commitBtn.BackColor = API.StoreThemeColors.Accent;
            actionPanel.Controls.Add(commitBtn);
            
            actionPanel.Tag = "actionPanel";
            this.Controls.Add(actionPanel);

            // Create info label (top right, like GamePicker's game count)
            _infoLabel = new Label
            {
                Font = new Font("Segoe UI", 10F),
                ForeColor = API.StoreThemeColors.ForegroundDim,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                Height = 30
            };
            this.Controls.Add(_infoLabel);

            // Add custom scrollbar for achievements list
            _achievementScrollBar = new API.StoreScrollBar
            {
                Width = 10,
                Minimum = 0,
                SmallChange = 1,
                LargeChange = 10
            };
            _achievementScrollBar.Scroll += OnAchievementScrollBarScroll;
            this.Controls.Add(_achievementScrollBar);
            _achievementScrollBar.BringToFront();

            // Handle achievement list scroll events - AGGRESSIVELY hide native scrollbar
            this._AchievementListView.Scroll += (s, e) => { UpdateAchievementScrollBar(); HideNativeScrollBar(); };
            this._AchievementListView.SizeChanged += (s, e) => { UpdateAchievementScrollBar(); HideNativeScrollBar(); };
            this._AchievementListView.ItemSelectionChanged += (s, e) => HideNativeScrollBar();
            this._AchievementListView.MouseWheel += (s, e) => HideNativeScrollBar();
            this._AchievementListView.VirtualItemsSelectionRangeChanged += (s, e) => HideNativeScrollBar();
            this._AchievementListView.VisibleChanged += (s, e) => HideNativeScrollBar();
            this._AchievementListView.HandleCreated += (s, e) => HideNativeScrollBar();

            // Hide achievements toolstrip inside tab (we have action panel now)
            this._AchievementsToolStrip.Visible = false;

            // AGGRESSIVE FIX: Hide TabControl completely and use the ListView directly on a panel
            // The TabControl 3D borders are impossible to fully remove
            this._MainTabControl.Visible = false;
            
            // Create a container panel for the achievement list (no borders!)
            var achievementContainer = new Panel
            {
                BackColor = API.StoreThemeColors.ListBackground,
                Tag = "achievementContainer"
            };
            this.Controls.Add(achievementContainer);
            
            // Move ListView from TabPage to container panel
            this._AchievementsTabPage.Controls.Remove(this._AchievementListView);
            achievementContainer.Controls.Add(this._AchievementListView);
            this._AchievementListView.Dock = DockStyle.Fill;
            
            // RIGHT-SIDE cover panel - make it WIDE to cover everything
            var rightCoverPanel = new Panel
            {
                BackColor = API.StoreThemeColors.ListBackground,
                Width = 120,  // VERY WIDE to ensure full coverage
                Dock = DockStyle.Right,
                Tag = "rightCover"
            };
            achievementContainer.Controls.Add(rightCoverPanel);
            rightCoverPanel.BringToFront();
            
            // Add a dark header strip at top of the right cover panel
            var rightHeaderStrip = new Panel
            {
                BackColor = API.StoreThemeColors.BackgroundDark,
                Height = 22,
                Dock = DockStyle.Top
            };
            rightCoverPanel.Controls.Add(rightHeaderStrip);
            
            // ADDITIONAL: Floating header cover that sits OVER the ListView header area
            var floatingHeaderCover = new Panel
            {
                BackColor = API.StoreThemeColors.BackgroundDark,
                Height = 22,
                Tag = "floatingHeaderCover"
            };
            achievementContainer.Controls.Add(floatingHeaderCover);
            floatingHeaderCover.BringToFront();
            
            // Update floating header cover position based on column widths
            Action updateFloatingCover = () =>
            {
                if (this._AchievementListView.Columns.Count == 0) return;
                
                int totalColWidth = 0;
                foreach (ColumnHeader col in this._AchievementListView.Columns)
                {
                    totalColWidth += col.Width;
                }
                
                // Position from end of columns to end of container
                floatingHeaderCover.Location = new Point(totalColWidth, 0);
                floatingHeaderCover.Width = Math.Max(200, achievementContainer.Width - totalColWidth);
                floatingHeaderCover.BringToFront();
            };
            
            this._AchievementListView.Resize += (s, e) => updateFloatingCover();
            this._AchievementListView.ColumnWidthChanged += (s, e) => updateFloatingCover();
            
            // Initialize everything on Load and Shown
            this.Load += (s, e) => { 
                achievementContainer.BringToFront();
                rightCoverPanel.BringToFront();
                floatingHeaderCover.BringToFront();
                updateFloatingCover();
                HideNativeScrollBar();
                UpdateAchievementScrollBar();
            };
            
            this.Shown += (s, e) => {
                // Re-initialize after form is shown
                achievementContainer.BringToFront();
                rightCoverPanel.BringToFront();
                floatingHeaderCover.BringToFront();
                updateFloatingCover();
                _achievementScrollBar.BringToFront();
                HideNativeScrollBar();
                UpdateAchievementScrollBar();
                this._AchievementListView.Invalidate();
            };

            // Apply Store theme
            ApplyStoreTheme();

            this._MainTabControl.SelectedTab = this._AchievementsTabPage;
            //this.statisticsList.Enabled = this.checkBox1.Checked;

            // Create blank icon with Store theme color
            var blankBitmap = new Bitmap(64, 64);
            using (var g = Graphics.FromImage(blankBitmap))
            {
                g.Clear(API.StoreThemeColors.ControlBackground);
            }
            this._AchievementImageList.Images.Add("Blank", blankBitmap);

            this._StatisticsDataGridView.AutoGenerateColumns = false;

            this._StatisticsDataGridView.Columns.Add("name", "Name");
            this._StatisticsDataGridView.Columns[0].ReadOnly = true;
            this._StatisticsDataGridView.Columns[0].Width = 200;
            this._StatisticsDataGridView.Columns[0].DataPropertyName = nameof(Stats.StatInfo.DisplayName);

            this._StatisticsDataGridView.Columns.Add("value", "Value");
            this._StatisticsDataGridView.Columns[1].ReadOnly = this._EnableStatsEditingCheckBox.Checked == false;
            this._StatisticsDataGridView.Columns[1].Width = 90;
            this._StatisticsDataGridView.Columns[1].DataPropertyName = nameof(Stats.StatInfo.Value);

            this._StatisticsDataGridView.Columns.Add("extra", "Extra");
            this._StatisticsDataGridView.Columns[2].ReadOnly = true;
            this._StatisticsDataGridView.Columns[2].Width = 200;
            this._StatisticsDataGridView.Columns[2].DataPropertyName = nameof(Stats.StatInfo.Extra);

            this._StatisticsDataGridView.DataSource = new BindingSource()
            {
                DataSource = this._Statistics,
            };

            this._GameId = gameId;
            this._SteamClient = client;

            // Removed _IconDownloader event subscription

            string name = this._SteamClient.SteamApps001.GetAppData((uint)this._GameId, "name");
            if (name != null)
            {
                base.Text += " | " + name;
            }
            else
            {
                base.Text += " | " + this._GameId.ToString(CultureInfo.InvariantCulture);
            }

            this._UserStatsReceivedCallback = client.CreateAndRegisterCallback<API.Callbacks.UserStatsReceived>();
            this._UserStatsReceivedCallback.OnRun += this.OnUserStatsReceived;

            // Setup keyboard shortcuts for accessibility
            SetupKeyboardShortcuts();

            //this.UserStatsStoredCallback = new API.Callback(1102, new API.Callback.CallbackFunction(this.OnUserStatsStored));

            // Defer heavy loading to after form is shown (async init)
            this.Shown += OnFormShownAsync;
        }

        private async void OnFormShownAsync(object sender, EventArgs e)
        {
            // Unsubscribe to prevent multiple calls
            this.Shown -= OnFormShownAsync;

            // Brief delay to let the UI render first
            await Task.Delay(50);

            // Now load stats asynchronously
            this.RefreshStats();
        }

        private void AddAchievementIcon(Stats.AchievementInfo info, Image icon)
        {
            if (icon == null)
            {
                info.ImageIndex = 0;
            }
            else
            {
                info.ImageIndex = this._AchievementImageList.Images.Count;
                this._AchievementImageList.Images.Add(info.IsAchieved == true ? info.IconNormal : info.IconLocked, icon);
            }
        }

        private async void DownloadNextIcon()
        {
            if (this._IconQueue.IsEmpty)
            {
                this._DownloadStatusLabel.Visible = false;
                return;
            }

            if (this._isDownloadingIcons)
            {
                return;
            }
            
            this._isDownloadingIcons = true;

            try
            {
                // Collect all items to download
                var itemsToDownload = new List<Stats.AchievementInfo>();
                while (this._IconQueue.TryDequeue(out var item))
                {
                    itemsToDownload.Add(item);
                }

                if (itemsToDownload.Count == 0) return;

                this._DownloadStatusLabel.Text = $"Downloading {itemsToDownload.Count} icons...";
                this._DownloadStatusLabel.Visible = true;

                // Download in parallel with throttling
                var downloadTasks = itemsToDownload.Select(info => DownloadIconAsync(info)).ToList();
                await Task.WhenAll(downloadTasks);

                if (!this.IsDisposed)
                {
                    this._AchievementListView.Update();
                }
            }
            finally
            {
                this._isDownloadingIcons = false;
                if (!this.IsDisposed)
                {
                    this._DownloadStatusLabel.Visible = false;
                    // Force GC after large download batch
                    if (GC.GetTotalMemory(false) > 100_000_000) // > 100MB
                    {
                        GC.Collect(2, GCCollectionMode.Optimized, false);
                    }
                }
            }
        }

        private async Task DownloadIconAsync(Stats.AchievementInfo info)
        {
            await _downloadSemaphore.WaitAsync();
            try
            {
                if (this.IsDisposed) return;

                var iconKey = info.IsAchieved == true ? info.IconNormal : info.IconLocked;
                var url = _($"{API.AppConfig.SteamCdnBaseUrl}/{this._GameId}/{iconKey}");

                // Check cache first
                var cachedBitmap = API.IconCache.Get(url);
                if (cachedBitmap != null)
                {
                    if (!this.IsDisposed)
                    {
                        this.Invoke(() => this.AddAchievementIcon(info, cachedBitmap));
                    }
                    return;
                }

                // Download and cache
                var data = await HttpClient.GetByteArrayAsync(url);
                var bitmap = API.IconCache.Store(url, data);
                
                if (!this.IsDisposed && bitmap != null)
                {
                    this.Invoke(() => this.AddAchievementIcon(info, bitmap));
                }
            }
            catch
            {
                if (!this.IsDisposed)
                {
                    this.Invoke(() => this.AddAchievementIcon(info, null));
                }
            }
            finally
            {
                _downloadSemaphore.Release();
            }
        }

        private static string TranslateError(int id) => id switch
        {
            2 => "generic error -- this usually means you don't own the game",
            _ => _($"{id}"),
        };

        private static string GetLocalizedString(KeyValue kv, string language, string defaultValue)
        {
            var name = kv[language].AsString("");
            if (string.IsNullOrEmpty(name) == false)
            {
                return name;
            }

            if (language != "english")
            {
                name = kv["english"].AsString("");
                if (string.IsNullOrEmpty(name) == false)
                {
                    return name;
                }
            }

            name = kv.AsString("");
            if (string.IsNullOrEmpty(name) == false)
            {
                return name;
            }

            return defaultValue;
        }

        private bool LoadUserGameStatsSchema()
        {
            string path;
            try
            {
                string fileName = _($"UserGameStatsSchema_{this._GameId}.bin");
                path = API.Steam.GetInstallPath();
                path = Path.Combine(path, "appcache", "stats", fileName);
                if (File.Exists(path) == false)
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

            var kv = KeyValue.LoadAsBinary(path);
            if (kv == null)
            {
                return false;
            }

            var currentLanguage = this._SteamClient.SteamApps008.GetCurrentGameLanguage();

            this._AchievementDefinitions.Clear();
            this._StatDefinitions.Clear();

            var stats = kv[this._GameId.ToString(CultureInfo.InvariantCulture)]["stats"];
            if (stats.Valid == false || stats.Children == null)
            {
                return false;
            }

            foreach (var stat in stats.Children)
            {
                if (stat.Valid == false)
                {
                    continue;
                }

                var rawType = stat["type_int"].Valid
                                  ? stat["type_int"].AsInteger(0)
                                  : stat["type"].AsInteger(0);
                var type = (APITypes.UserStatType)rawType;
                switch (type)
                {
                    case APITypes.UserStatType.Invalid:
                    {
                        break;
                    }

                    case APITypes.UserStatType.Integer:
                    {
                        var id = stat["name"].AsString("");
                        string name = GetLocalizedString(stat["display"]["name"], currentLanguage, id);

                        this._StatDefinitions.Add(new Stats.IntegerStatDefinition()
                        {
                            Id = stat["name"].AsString(""),
                            DisplayName = name,
                            MinValue = stat["min"].AsInteger(int.MinValue),
                            MaxValue = stat["max"].AsInteger(int.MaxValue),
                            MaxChange = stat["maxchange"].AsInteger(0),
                            IncrementOnly = stat["incrementonly"].AsBoolean(false),
                            SetByTrustedGameServer = stat["bSetByTrustedGS"].AsBoolean(false),
                            DefaultValue = stat["default"].AsInteger(0),
                            Permission = stat["permission"].AsInteger(0),
                        });
                        break;
                    }

                    case APITypes.UserStatType.Float:
                    case APITypes.UserStatType.AverageRate:
                    {
                        var id = stat["name"].AsString("");
                        string name = GetLocalizedString(stat["display"]["name"], currentLanguage, id);

                        this._StatDefinitions.Add(new Stats.FloatStatDefinition()
                        {
                            Id = stat["name"].AsString(""),
                            DisplayName = name,
                            MinValue = stat["min"].AsFloat(float.MinValue),
                            MaxValue = stat["max"].AsFloat(float.MaxValue),
                            MaxChange = stat["maxchange"].AsFloat(0.0f),
                            IncrementOnly = stat["incrementonly"].AsBoolean(false),
                            DefaultValue = stat["default"].AsFloat(0.0f),
                            Permission = stat["permission"].AsInteger(0),
                        });
                        break;
                    }

                    case APITypes.UserStatType.Achievements:
                    case APITypes.UserStatType.GroupAchievements:
                    {
                        if (stat.Children != null)
                        {
                            foreach (var bits in stat.Children.Where(
                                b => string.Compare(b.Name, "bits", StringComparison.InvariantCultureIgnoreCase) == 0))
                            {
                                if (bits.Valid == false ||
                                    bits.Children == null)
                                {
                                    continue;
                                }

                                foreach (var bit in bits.Children)
                                {
                                    string id = bit["name"].AsString("");
                                    string name = GetLocalizedString(bit["display"]["name"], currentLanguage, id);
                                    string desc = GetLocalizedString(bit["display"]["desc"], currentLanguage, "");

                                    this._AchievementDefinitions.Add(new()
                                    {
                                        Id = id,
                                        Name = name,
                                        Description = desc,
                                        IconNormal = bit["display"]["icon"].AsString(""),
                                        IconLocked = bit["display"]["icon_gray"].AsString(""),
                                        IsHidden = bit["display"]["hidden"].AsBoolean(false),
                                        Permission = bit["permission"].AsInteger(0),
                                    });
                                }
                            }
                        }

                        break;
                    }

                    default:
                    {
                        throw new InvalidOperationException("invalid stat type");
                    }
                }
            }

            return true;
        }

        private void OnUserStatsReceived(APITypes.UserStatsReceived param)
        {
            if (param.Result != 1)
            {
                this._GameStatusLabel.Text = $"Error while retrieving stats: {TranslateError(param.Result)}";
                this.EnableInput();
                return;
            }

            if (this.LoadUserGameStatsSchema() == false)
            {
                this._GameStatusLabel.Text = "Failed to load schema.";
                this.EnableInput();
                return;
            }

            try
            {
                this.GetAchievements();
            }
            catch (Exception e)
            {
                this._GameStatusLabel.Text = "Error when handling achievements retrieval.";
                this.EnableInput();
                MessageBox.Show(
                    "Error when handling achievements retrieval:\n" + e,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            try
            {
                this.GetStatistics();
            }
            catch (Exception e)
            {
                this._GameStatusLabel.Text = "Error when handling stats retrieval.";
                this.EnableInput();
                MessageBox.Show(
                    "Error when handling stats retrieval:\n" + e,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            this._GameStatusLabel.Text = $"Retrieved {this._AchievementListView.Items.Count} achievements and {this._StatisticsDataGridView.Rows.Count} statistics.";
            
            // Update info label
            if (_infoLabel != null)
            {
                _infoLabel.Text = $"{this._AchievementListView.Items.Count} Achievements";
            }
            
            // Update scrollbar
            UpdateAchievementScrollBar();
            HideNativeScrollBar();
            
            // Check for protected achievements/stats based on Steam schema
            CheckForProtectedStats();
            
            this.EnableInput();
        }

        private void RefreshStats()
        {
            this._AchievementListView.Items.Clear();
            this._StatisticsDataGridView.Rows.Clear();

            var steamId = this._SteamClient.SteamUser.GetSteamId();

            // This still triggers the UserStatsReceived callback, in addition to the callresult.
            // No need to implement callresults for the time being.
            var callHandle = this._SteamClient.SteamUserStats.RequestUserStats(steamId);
            if (callHandle == API.CallHandle.Invalid)
            {
                MessageBox.Show(this, "Failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this._GameStatusLabel.Text = "Retrieving stat information...";
            this.DisableInput();
        }

        /// <summary>
        /// Checks if any achievements or stats have the Protected permission flag set.
        /// This is based on Steam's schema, not external detection.
        /// </summary>
        private void CheckForProtectedStats()
        {
            _hasProtectedStats = false;
            int protectedAchievements = 0;
            int protectedStats = 0;

            // Check achievements for protected flag (permission & 2)
            foreach (var achievement in this._AchievementDefinitions)
            {
                if ((achievement.Permission & 2) != 0)
                {
                    protectedAchievements++;
                }
            }

            // Check stats for protected flag (permission & 2)
            foreach (var stat in this._StatDefinitions)
            {
                if ((stat.Permission & 2) != 0)
                {
                    protectedStats++;
                }
            }

            // Update protection cache for Game Picker to use
            API.ProtectionCache.UpdateProtectionStatus(
                (uint)this._GameId,
                protectedAchievements,
                protectedStats,
                this._AchievementDefinitions.Count,
                this._StatDefinitions.Count);

            _hasProtectedStats = protectedAchievements > 0 || protectedStats > 0;

            if (_hasProtectedStats)
            {
                // Show warning in info label
                if (_infoLabel != null)
                {
                    _infoLabel.Text = $"⚠️ {protectedAchievements} protected";
                    _infoLabel.ForeColor = Color.FromArgb(255, 193, 7);
                }

                // Update status
                this._GameStatusLabel.Text = $"Retrieved {this._AchievementListView.Items.Count} achievements ({protectedAchievements} protected) and {this._StatisticsDataGridView.Rows.Count} statistics ({protectedStats} protected).";

                // Show one-time warning
                MessageBox.Show(
                    this,
                    $"This game has protected achievements/stats:\n\n" +
                    $"• {protectedAchievements} protected achievements\n" +
                    $"• {protectedStats} protected statistics\n\n" +
                    "Protected items cannot be modified. Steam's servers will reject any changes to these items.\n\n" +
                    "You can still modify unprotected items.",
                    "⚠️ Protected Stats Detected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private bool _IsUpdatingAchievementList;

        private void GetAchievements()
        {
            var textSearch = this._MatchingStringTextBox.Text.Length > 0
                ? this._MatchingStringTextBox.Text
                : null;

            this._IsUpdatingAchievementList = true;

            this._AchievementListView.Items.Clear();
            this._AchievementListView.BeginUpdate();
            //this.Achievements.Clear();

            bool wantLocked = this._DisplayLockedOnlyButton.Checked == true;
            bool wantUnlocked = this._DisplayUnlockedOnlyButton.Checked == true;
            bool wantHidden = this._DisplayHiddenOnlyButton.Checked == true;

            foreach (var def in this._AchievementDefinitions)
            {
                if (string.IsNullOrEmpty(def.Id) == true)
                {
                    continue;
                }

                // Filter hidden achievements
                if (wantHidden && !def.IsHidden)
                {
                    continue;
                }

                if (this._SteamClient.SteamUserStats.GetAchievementAndUnlockTime(
                    def.Id,
                    out bool isAchieved,
                    out var unlockTime) == false)
                {
                    continue;
                }

                bool wanted = (wantLocked == false && wantUnlocked == false) || isAchieved switch
                {
                    true => wantUnlocked,
                    false => wantLocked,
                };
                if (wanted == false)
                {
                    continue;
                }

                if (textSearch != null)
                {
                    if (def.Name.IndexOf(textSearch, StringComparison.OrdinalIgnoreCase) < 0 &&
                        def.Description.IndexOf(textSearch, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                }

                Stats.AchievementInfo info = new()
                {
                    Id = def.Id,
                    IsAchieved = isAchieved,
                    UnlockTime = isAchieved == true && unlockTime > 0
                        ? DateTimeOffset.FromUnixTimeSeconds(unlockTime).LocalDateTime
                        : null,
                    IconNormal = string.IsNullOrEmpty(def.IconNormal) ? null : def.IconNormal,
                    IconLocked = string.IsNullOrEmpty(def.IconLocked) ? def.IconNormal : def.IconLocked,
                    Permission = def.Permission,
                    Name = def.Name,
                    Description = def.Description,
                };

                ListViewItem item = new()
                {
                    Checked = isAchieved,
                    Tag = info,
                    Text = info.Name,
                    BackColor = (def.Permission & 2) == 0 ? Color.Black : Color.FromArgb(64, 0, 0),
                    ForeColor = (def.Permission & 2) == 0 ? Color.White : Color.FromArgb(255, 193, 7),
                };

                info.Item = item;

                // Mark protected achievements in name
                bool isProtected = (def.Permission & 2) != 0;
                if (item.Text.StartsWith("#", StringComparison.InvariantCulture) == true)
                {
                    item.Text = isProtected ? $"🔒 {info.Id}" : info.Id;
                    item.SubItems.Add(isProtected ? "[PROTECTED]" : "");
                }
                else
                {
                    item.Text = isProtected ? $"🔒 {info.Name}" : info.Name;
                    item.SubItems.Add(isProtected ? $"[PROTECTED] {info.Description}" : info.Description);
                }

                item.SubItems.Add(info.UnlockTime.HasValue == true
                    ? info.UnlockTime.Value.ToString()
                    : "");

                info.ImageIndex = 0;

                this.AddAchievementToIconQueue(info, false);
                this._AchievementListView.Items.Add(item);
            }

            this._AchievementListView.EndUpdate();
            this._IsUpdatingAchievementList = false;

            this.DownloadNextIcon();
        }

        private void GetStatistics()
        {
            this._Statistics.Clear();
            foreach (var stat in this._StatDefinitions)
            {
                if (string.IsNullOrEmpty(stat.Id) == true)
                {
                    continue;
                }

                if (stat is Stats.IntegerStatDefinition intStat)
                {
                    if (this._SteamClient.SteamUserStats.GetStatValue(intStat.Id, out int value) == false)
                    {
                        continue;
                    }
                    this._Statistics.Add(new Stats.IntStatInfo()
                    {
                        Id = intStat.Id,
                        DisplayName = intStat.DisplayName,
                        IntValue = value,
                        OriginalValue = value,
                        IsIncrementOnly = intStat.IncrementOnly,
                        Permission = intStat.Permission,
                    });
                }
                else if (stat is Stats.FloatStatDefinition floatStat)
                {
                    if (this._SteamClient.SteamUserStats.GetStatValue(floatStat.Id, out float value) == false)
                    {
                        continue;
                    }
                    this._Statistics.Add(new Stats.FloatStatInfo()
                    {
                        Id = floatStat.Id,
                        DisplayName = floatStat.DisplayName,
                        FloatValue = value,
                        OriginalValue = value,
                        IsIncrementOnly = floatStat.IncrementOnly,
                        Permission = floatStat.Permission,
                    });
                }
            }
        }

        private void AddAchievementToIconQueue(Stats.AchievementInfo info, bool startDownload)
        {
            int imageIndex = this._AchievementImageList.Images.IndexOfKey(
                info.IsAchieved == true ? info.IconNormal : info.IconLocked);

            if (imageIndex >= 0)
            {
                info.ImageIndex = imageIndex;
            }
            else
            {
                this._IconQueue.Enqueue(info);

                if (startDownload == true)
                {
                    this.DownloadNextIcon();
                }
            }
        }

        private int StoreAchievements()
        {
            if (this._AchievementListView.Items.Count == 0)
            {
                return 0;
            }

            List<Stats.AchievementInfo> achievements = new();
            foreach (ListViewItem item in this._AchievementListView.Items)
            {
                if (item.Tag is not Stats.AchievementInfo achievementInfo ||
                    achievementInfo.IsAchieved == item.Checked)
                {
                    continue;
                }

                achievementInfo.IsAchieved = item.Checked;
                achievements.Add(achievementInfo);
            }

            if (achievements.Count == 0)
            {
                return 0;
            }

            foreach (var info in achievements)
            {
                if (this._SteamClient.SteamUserStats.SetAchievement(info.Id, info.IsAchieved) == false)
                {
                    MessageBox.Show(
                        this,
                        $"An error occurred while setting the state for {info.Id}, aborting store.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return -1;
                }
            }

            return achievements.Count;
        }

        private int StoreStatistics()
        {
            if (this._Statistics.Count == 0)
            {
                return 0;
            }

            var statistics = this._Statistics.Where(stat => stat.IsModified == true).ToList();
            if (statistics.Count == 0)
            {
                return 0;
            }

            foreach (var stat in statistics)
            {
                if (stat is Stats.IntStatInfo intStat)
                {
                    if (this._SteamClient.SteamUserStats.SetStatValue(
                        intStat.Id,
                        intStat.IntValue) == false)
                    {
                        MessageBox.Show(
                            this,
                            $"An error occurred while setting the value for {stat.Id}, aborting store.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return -1;
                    }
                }
                else if (stat is Stats.FloatStatInfo floatStat)
                {
                    if (this._SteamClient.SteamUserStats.SetStatValue(
                        floatStat.Id,
                        floatStat.FloatValue) == false)
                    {
                        MessageBox.Show(
                            this,
                            $"An error occurred while setting the value for {stat.Id}, aborting store.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return -1;
                    }
                }
                else
                {
                    throw new InvalidOperationException("unsupported stat type");
                }
            }

            return statistics.Count;
        }

        private void DisableInput()
        {
            this._ReloadButton.Enabled = false;
            this._StoreButton.Enabled = false;
        }

        private void EnableInput()
        {
            this._ReloadButton.Enabled = true;
            this._StoreButton.Enabled = true;
        }

        private void OnTimer(object sender, EventArgs e)
        {
            this._CallbackTimer.Enabled = false;
            this._SteamClient.RunCallbacks(false);
            
            // CONTINUOUSLY hide native scrollbar to ensure it never appears
            HideNativeScrollBar();
            
            this._CallbackTimer.Enabled = true;
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            this.RefreshStats();
        }

        private void OnLockAll(object sender, EventArgs e)
        {
            foreach (ListViewItem item in this._AchievementListView.Items)
            {
                item.Checked = false;
            }
        }

        private void OnInvertAll(object sender, EventArgs e)
        {
            foreach (ListViewItem item in this._AchievementListView.Items)
            {
                item.Checked = !item.Checked;
            }
        }

        private void OnUnlockAll(object sender, EventArgs e)
        {
            foreach (ListViewItem item in this._AchievementListView.Items)
            {
                item.Checked = true;
            }
        }

        private bool Store()
        {
            if (this._SteamClient.SteamUserStats.StoreStats() == false)
            {
                MessageBox.Show(
                    this,
                    "An error occurred while storing, aborting.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        private void OnStore(object sender, EventArgs e)
        {
            // Show warning if protected stats exist
            if (_hasProtectedStats)
            {
                var result = MessageBox.Show(
                    this,
                    "This game has protected achievements/stats that cannot be modified.\n\n" +
                    "Any changes to protected items will be rejected by Steam's servers.\n" +
                    "Unprotected items can still be saved.\n\n" +
                    "Do you want to continue?",
                    "⚠️ Protected Stats Warning",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                {
                    return;
                }
            }

            int achievements = this.StoreAchievements();
            if (achievements < 0)
            {
                this.RefreshStats();
                return;
            }

            int stats = this.StoreStatistics();
            if (stats < 0)
            {
                this.RefreshStats();
                return;
            }

            if (this.Store() == false)
            {
                this.RefreshStats();
                return;
            }

            MessageBox.Show(
                this,
                $"Stored {achievements} achievements and {stats} statistics.",
                "Information",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            this.RefreshStats();
        }

        private void OnStatDataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            if (e.Context != DataGridViewDataErrorContexts.Commit)
            {
                return;
            }

            var view = (DataGridView)sender;
            if (e.Exception is Stats.StatIsProtectedException)
            {
                e.ThrowException = false;
                e.Cancel = true;
                view.Rows[e.RowIndex].ErrorText = "Stat is protected! -- you can't modify it";
            }
            else
            {
                e.ThrowException = false;
                e.Cancel = true;
                view.Rows[e.RowIndex].ErrorText = "Invalid value";
            }
        }

        private void OnStatAgreementChecked(object sender, EventArgs e)
        {
            this._StatisticsDataGridView.Columns[1].ReadOnly = this._EnableStatsEditingCheckBox.Checked == false;
        }

        private void OnStatCellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var view = (DataGridView)sender;
            view.Rows[e.RowIndex].ErrorText = "";
        }

        private void OnResetAllStats(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                "Are you absolutely sure you want to reset stats?",
                "Warning",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) == DialogResult.No)
            {
                return;
            }

            bool achievementsToo = DialogResult.Yes == MessageBox.Show(
                "Do you want to reset achievements too?",
                "Question",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (MessageBox.Show(
                "Really really sure?",
                "Warning",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Error) == DialogResult.No)
            {
                return;
            }

            if (this._SteamClient.SteamUserStats.ResetAllStats(achievementsToo) == false)
            {
                MessageBox.Show(this, "Failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.RefreshStats();
        }

        private void OnCheckAchievement(object sender, ItemCheckEventArgs e)
        {
            if (sender != this._AchievementListView)
            {
                return;
            }

            if (this._IsUpdatingAchievementList == true)
            {
                return;
            }

            if (this._AchievementListView.Items[e.Index].Tag is not Stats.AchievementInfo info)
            {
                return;
            }

            if ((info.Permission & 3) != 0)
            {
                MessageBox.Show(
                    this,
                    "Sorry, but this is a protected achievement and cannot be managed with Steam Achievement Manager.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                e.NewValue = e.CurrentValue;
            }
        }

        private void OnDisplayUncheckedOnly(object sender, EventArgs e)
        {
            if ((sender as ToolStripButton).Checked == true)
            {
                this._DisplayLockedOnlyButton.Checked = false;
            }

            this.GetAchievements();
        }

        private void OnDisplayCheckedOnly(object sender, EventArgs e)
        {
            if ((sender as ToolStripButton).Checked == true)
            {
                this._DisplayUnlockedOnlyButton.Checked = false;
            }

            this.GetAchievements();
        }

        private void OnDisplayHiddenOnly(object sender, EventArgs e)
        {
            this.GetAchievements();
        }

        private void OnFilterUpdate(object sender, KeyEventArgs e)
        {
            this.GetAchievements();
        }

        private Button CreateActionButton(string text, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = API.StoreThemeColors.ControlBackground,
                ForeColor = API.StoreThemeColors.Foreground,
                Font = new Font("Segoe UI", 9F),
                Height = 28,
                AutoSize = true,
                Padding = new Padding(8, 0, 8, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = API.StoreThemeColors.ControlBorder;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = API.StoreThemeColors.ListHover;
            btn.Click += onClick;
            return btn;
        }

        /// <summary>
        /// Applies the Store theme to all controls.
        /// </summary>
        private void ApplyStoreTheme()
        {
            // Form background
            this.BackColor = API.StoreThemeColors.Background;
            this.ForeColor = API.StoreThemeColors.Foreground;

            // Title bar styling
            _titleBar?.ApplyTheme();

            // Main ToolStrip
            ApplyStoreThemeToToolStrip(this._MainToolStrip);

            // Achievements ToolStrip  
            ApplyStoreThemeToToolStrip(this._AchievementsToolStrip);

            // Tab Control - AGGRESSIVE: Hide default tabs and use custom buttons
            this._MainTabControl.BackColor = API.StoreThemeColors.Background;
            this._MainTabControl.ForeColor = API.StoreThemeColors.Foreground;
            
            // Move tabs off screen - we'll use custom buttons instead
            this._MainTabControl.ItemSize = new Size(0, 1);
            this._MainTabControl.SizeMode = TabSizeMode.Fixed;
            this._MainTabControl.Appearance = TabAppearance.FlatButtons; // Flat removes 3D border
            this._MainTabControl.Margin = new Padding(0);
            this._MainTabControl.Padding = new Point(0, 0);
            
            // Create custom tab buttons panel (positioned above tab content)
            var tabButtonPanel = this.Controls.OfType<Panel>().FirstOrDefault(p => (string)p.Tag == "tabButtons");
            if (tabButtonPanel == null)
            {
                tabButtonPanel = new Panel
                {
                    BackColor = API.StoreThemeColors.BackgroundDark,
                    Height = 30,
                    Tag = "tabButtons"
                };
                
                var achievementsBtn = new Button
                {
                    Text = "Achievements",
                    FlatStyle = FlatStyle.Flat,
                    BackColor = API.StoreThemeColors.Background,
                    ForeColor = API.StoreThemeColors.Foreground,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Height = 28,
                    Width = 100,
                    Location = new Point(2, 1),
                    Tag = "achievementsTab"
                };
                achievementsBtn.FlatAppearance.BorderSize = 0;
                achievementsBtn.Click += (s, e) =>
                {
                    // Show achievement container, hide TabControl
                    var achContainer = this.Controls.OfType<Panel>().FirstOrDefault(p => (string)p.Tag == "achievementContainer");
                    if (achContainer != null) achContainer.Visible = true;
                    this._MainTabControl.Visible = false;
                    _achievementScrollBar.Visible = true;
                    
                    achievementsBtn.BackColor = API.StoreThemeColors.Background;
                    achievementsBtn.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    if (tabButtonPanel.Controls.OfType<Button>().FirstOrDefault(b => (string)b.Tag == "statisticsTab") is Button statsBtn)
                    {
                        statsBtn.BackColor = API.StoreThemeColors.BackgroundDark;
                        statsBtn.Font = new Font("Segoe UI", 9F);
                    }
                };
                tabButtonPanel.Controls.Add(achievementsBtn);
                
                var statisticsBtn = new Button
                {
                    Text = "Statistics",
                    FlatStyle = FlatStyle.Flat,
                    BackColor = API.StoreThemeColors.BackgroundDark,
                    ForeColor = API.StoreThemeColors.Foreground,
                    Font = new Font("Segoe UI", 9F),
                    Height = 28,
                    Width = 80,
                    Location = new Point(104, 1),
                    Tag = "statisticsTab"
                };
                statisticsBtn.FlatAppearance.BorderSize = 0;
                statisticsBtn.Click += (s, e) =>
                {
                    // Hide achievement container, show TabControl with Statistics
                    var achContainer = this.Controls.OfType<Panel>().FirstOrDefault(p => (string)p.Tag == "achievementContainer");
                    if (achContainer != null) achContainer.Visible = false;
                    this._MainTabControl.Visible = true;
                    this._MainTabControl.SelectedTab = this._StatisticsTabPage;
                    _achievementScrollBar.Visible = false;
                    
                    statisticsBtn.BackColor = API.StoreThemeColors.Background;
                    statisticsBtn.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    if (tabButtonPanel.Controls.OfType<Button>().FirstOrDefault(b => (string)b.Tag == "achievementsTab") is Button achBtn)
                    {
                        achBtn.BackColor = API.StoreThemeColors.BackgroundDark;
                        achBtn.Font = new Font("Segoe UI", 9F);
                    }
                };
                tabButtonPanel.Controls.Add(statisticsBtn);
                
                this.Controls.Add(tabButtonPanel);
                tabButtonPanel.BringToFront();
            }

            // Tab Pages - set padding to 0 to minimize borders
            this._AchievementsTabPage.BackColor = API.StoreThemeColors.ListBackground;
            this._AchievementsTabPage.ForeColor = API.StoreThemeColors.Foreground;
            this._AchievementsTabPage.Padding = new Padding(0);
            this._AchievementsTabPage.Margin = new Padding(0);
            this._AchievementsTabPage.UseVisualStyleBackColor = false;
            // Paint event to cover any system borders
            this._AchievementsTabPage.Paint -= OnTabPagePaint;
            this._AchievementsTabPage.Paint += OnTabPagePaint;
            this._StatisticsTabPage.BackColor = API.StoreThemeColors.Background;
            this._StatisticsTabPage.ForeColor = API.StoreThemeColors.Foreground;
            this._StatisticsTabPage.Padding = new Padding(0);
            this._StatisticsTabPage.Margin = new Padding(0);
            this._StatisticsTabPage.UseVisualStyleBackColor = false;
            this._StatisticsTabPage.Paint -= OnTabPagePaint;
            this._StatisticsTabPage.Paint += OnTabPagePaint;

            // Achievement ListView - FULL owner draw for dark theme
            this._AchievementListView.BackColor = API.StoreThemeColors.ListBackground;
            this._AchievementListView.ForeColor = API.StoreThemeColors.Foreground;
            this._AchievementListView.BorderStyle = BorderStyle.None;
            this._AchievementListView.GridLines = false; // Remove white grid lines
            this._AchievementListView.Margin = new Padding(0);
            this._AchievementListView.OwnerDraw = true;
            this._AchievementListView.DrawColumnHeader -= OnListViewDrawColumnHeader;
            this._AchievementListView.DrawColumnHeader += OnListViewDrawColumnHeader;
            this._AchievementListView.DrawItem -= OnListViewDrawItem;
            this._AchievementListView.DrawItem += OnListViewDrawItem;
            this._AchievementListView.DrawSubItem -= OnListViewDrawSubItem;
            this._AchievementListView.DrawSubItem += OnListViewDrawSubItem;
            
            // AGGRESSIVE: Add Paint event to cover any remaining white areas
            this._AchievementListView.Paint -= OnListViewPaint;
            this._AchievementListView.Paint += OnListViewPaint;
            
            // Make ListView fill (but leave room for our custom scrollbar)
            this._AchievementListView.Dock = DockStyle.Fill;

            // Statistics DataGridView
            ApplyStoreThemeToDataGridView(this._StatisticsDataGridView);

            // Enable Stats Checkbox
            this._EnableStatsEditingCheckBox.BackColor = API.StoreThemeColors.Background;
            this._EnableStatsEditingCheckBox.ForeColor = API.StoreThemeColors.Foreground;

            // Status Strip
            this._MainStatusStrip.BackColor = API.StoreThemeColors.BackgroundDark;
            this._MainStatusStrip.ForeColor = API.StoreThemeColors.Foreground;
            foreach (ToolStripItem item in this._MainStatusStrip.Items)
            {
                item.BackColor = API.StoreThemeColors.BackgroundDark;
                item.ForeColor = API.StoreThemeColors.ForegroundDim;
            }
        }

        private static void ApplyStoreThemeToToolStrip(ToolStrip toolStrip)
        {
            toolStrip.BackColor = API.StoreThemeColors.BackgroundLight;
            toolStrip.ForeColor = API.StoreThemeColors.Foreground;
            toolStrip.RenderMode = ToolStripRenderMode.Professional;
            toolStrip.Renderer = new StoreToolStripRenderer();

            foreach (ToolStripItem item in toolStrip.Items)
            {
                item.BackColor = API.StoreThemeColors.BackgroundLight;
                item.ForeColor = API.StoreThemeColors.Foreground;

                if (item is ToolStripTextBox txt)
                {
                    txt.BackColor = API.StoreThemeColors.ControlBackground;
                    txt.ForeColor = API.StoreThemeColors.Foreground;
                }
            }
        }

        private static void ApplyStoreThemeToDataGridView(DataGridView dgv)
        {
            dgv.BackgroundColor = API.StoreThemeColors.ListBackground;
            dgv.ForeColor = API.StoreThemeColors.Foreground;
            dgv.GridColor = API.StoreThemeColors.ControlBorder;
            dgv.BorderStyle = BorderStyle.None;
            
            // Default cell style
            dgv.DefaultCellStyle.BackColor = API.StoreThemeColors.ListBackground;
            dgv.DefaultCellStyle.ForeColor = API.StoreThemeColors.Foreground;
            dgv.DefaultCellStyle.SelectionBackColor = API.StoreThemeColors.Accent;
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;

            // Alternating rows
            dgv.AlternatingRowsDefaultCellStyle.BackColor = API.StoreThemeColors.ListAlternate;
            dgv.AlternatingRowsDefaultCellStyle.ForeColor = API.StoreThemeColors.Foreground;

            // Column headers
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = API.StoreThemeColors.BackgroundLight;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = API.StoreThemeColors.Foreground;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = API.StoreThemeColors.BackgroundLight;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

            // Row headers
            dgv.RowHeadersDefaultCellStyle.BackColor = API.StoreThemeColors.BackgroundLight;
            dgv.RowHeadersDefaultCellStyle.ForeColor = API.StoreThemeColors.Foreground;
            dgv.RowHeadersDefaultCellStyle.SelectionBackColor = API.StoreThemeColors.BackgroundLight;
            dgv.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        }

        /// <summary>
        /// Sets up keyboard shortcuts for accessibility.
        /// </summary>
        private void SetupKeyboardShortcuts()
        {
            this.KeyPreview = true;
            this.KeyDown += OnManagerKeyDown;
        }

        private void OnManagerKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+S = Store/Commit changes
            if (e.Control && e.KeyCode == Keys.S)
            {
                if (this._StoreButton.Enabled)
                {
                    OnStore(this, EventArgs.Empty);
                }
                e.Handled = true;
            }
            // F5 or Ctrl+R = Refresh
            else if (e.KeyCode == Keys.F5 || (e.Control && e.KeyCode == Keys.R))
            {
                if (this._ReloadButton.Enabled)
                {
                    OnRefresh(this, EventArgs.Empty);
                }
                e.Handled = true;
            }
            // Ctrl+F = Focus filter
            else if (e.Control && e.KeyCode == Keys.F)
            {
                this._MatchingStringTextBox.Focus();
                e.Handled = true;
            }
            // Ctrl+A = Unlock all (with confirmation in OnUnlockAll)
            else if (e.Control && e.KeyCode == Keys.A && this._AchievementsTabPage.Visible)
            {
                OnUnlockAll(this, EventArgs.Empty);
                e.Handled = true;
            }
            // Ctrl+L = Lock all
            else if (e.Control && e.KeyCode == Keys.L && this._AchievementsTabPage.Visible)
            {
                OnLockAll(this, EventArgs.Empty);
                e.Handled = true;
            }
            // Escape = Clear filter
            else if (e.KeyCode == Keys.Escape)
            {
                this._MatchingStringTextBox.Text = "";
                GetAchievements();
                e.Handled = true;
            }
        }

        // Window resize constants for custom title bar
        private const int RESIZE_BORDER = 8;
        private const int WM_NCHITTEST = 0x84;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST && this.FormBorderStyle == FormBorderStyle.None)
            {
                // Allow resizing from edges - extract X/Y from packed LParam
                int lParam = m.LParam.ToInt32();
                int screenX = (short)(lParam & 0xFFFF);
                int screenY = (short)(lParam >> 16);
                Point pos = this.PointToClient(new Point(screenX, screenY));
                
                if (pos.X <= RESIZE_BORDER)
                {
                    if (pos.Y <= RESIZE_BORDER)
                        m.Result = (IntPtr)HTTOPLEFT;
                    else if (pos.Y >= this.ClientSize.Height - RESIZE_BORDER)
                        m.Result = (IntPtr)HTBOTTOMLEFT;
                    else
                        m.Result = (IntPtr)HTLEFT;
                    return;
                }
                else if (pos.X >= this.ClientSize.Width - RESIZE_BORDER)
                {
                    if (pos.Y <= RESIZE_BORDER)
                        m.Result = (IntPtr)HTTOPRIGHT;
                    else if (pos.Y >= this.ClientSize.Height - RESIZE_BORDER)
                        m.Result = (IntPtr)HTBOTTOMRIGHT;
                    else
                        m.Result = (IntPtr)HTRIGHT;
                    return;
                }
                else if (pos.Y <= RESIZE_BORDER)
                {
                    m.Result = (IntPtr)HTTOP;
                    return;
                }
                else if (pos.Y >= this.ClientSize.Height - RESIZE_BORDER)
                {
                    m.Result = (IntPtr)HTBOTTOM;
                    return;
                }
            }

            base.WndProc(ref m);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateLayoutForTitleBar();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UpdateLayoutForTitleBar();
        }

        private void UpdateLayoutForTitleBar()
        {
            if (_titleBar == null) return;
            
            int border = RESIZE_BORDER;
            int titleBarHeight = _titleBar.Height;
            int clientWidth = this.ClientSize.Width;
            int clientHeight = this.ClientSize.Height;
            int infoLabelWidth = 180;
            int actionPanelHeight = 36;
            int tabButtonHeight = 30;
            
            // Position title bar with resize border margin
            _titleBar.Location = new Point(border, border);
            _titleBar.Width = clientWidth - (border * 2);
            
            // Find and position action panel
            Panel actionPanel = null;
            Panel tabButtonPanel = null;
            foreach (Control c in this.Controls)
            {
                if (c is Panel p)
                {
                    if (p.Tag?.ToString() == "actionPanel")
                        actionPanel = p;
                    else if (p.Tag?.ToString() == "tabButtons")
                        tabButtonPanel = p;
                }
            }
            
            int contentTop = border + titleBarHeight;
            
            if (actionPanel != null)
            {
                actionPanel.Location = new Point(border, contentTop);
                actionPanel.Width = clientWidth - (border * 2) - infoLabelWidth - 8;
                contentTop += actionPanelHeight;
            }
            
            // Position info label (next to action panel or below title bar)
            if (_infoLabel != null)
            {
                _infoLabel.Location = new Point(clientWidth - border - infoLabelWidth, border + titleBarHeight + 3);
                _infoLabel.Width = infoLabelWidth;
            }
            
            // Position tab button panel
            if (tabButtonPanel != null)
            {
                tabButtonPanel.Location = new Point(border, contentTop);
                tabButtonPanel.Width = clientWidth - (border * 2);
                contentTop += tabButtonHeight;
            }
            
            // Find and position the achievement container panel
            Panel achievementContainer = null;
            foreach (Control c in this.Controls)
            {
                if (c is Panel p && p.Tag?.ToString() == "achievementContainer")
                {
                    achievementContainer = p;
                    break;
                }
            }
            
            if (achievementContainer != null)
            {
                // FULL WIDTH - no extra space, the right cover panel inside handles coverage
                achievementContainer.Location = new Point(border, contentTop);
                achievementContainer.Size = new Size(clientWidth - (border * 2), clientHeight - contentTop - border);
                achievementContainer.BringToFront();
                
                // Update floating header cover
                foreach (Control c in achievementContainer.Controls)
                {
                    if (c is Panel p && p.Tag?.ToString() == "floatingHeaderCover")
                    {
                        int totalColWidth = 0;
                        foreach (ColumnHeader col in this._AchievementListView.Columns)
                        {
                            totalColWidth += col.Width;
                        }
                        p.Location = new Point(totalColWidth, 0);
                        p.Width = Math.Max(200, achievementContainer.Width - totalColWidth);
                        p.BringToFront();
                        break;
                    }
                }
            }
            
            // Keep TabControl positioned (hidden, but needed for Statistics tab switching)
            this._MainTabControl.Location = new Point(border, contentTop);
            this._MainTabControl.Size = new Size(clientWidth - (border * 2), clientHeight - contentTop - border);
            
            // Position achievement scrollbar at the right edge, OVER the right cover panel
            if (_achievementScrollBar != null && this._AchievementListView != null && this._AchievementListView.IsHandleCreated && achievementContainer != null)
            {
                _achievementScrollBar.Location = new Point(
                    achievementContainer.Right - _achievementScrollBar.Width - 2,
                    achievementContainer.Top + 22);  // Below header
                _achievementScrollBar.Height = achievementContainer.Height - 22;
                _achievementScrollBar.BringToFront();
                
                UpdateAchievementScrollBar();
                HideNativeScrollBar();
            }
        }

        private void UpdateAchievementScrollBar()
        {
            if (_achievementScrollBar == null || this._AchievementListView == null) return;
            
            int itemCount = this._AchievementListView.Items.Count;
            int visibleItems = this._AchievementListView.ClientSize.Height / 
                Math.Max(1, this._AchievementListView.Items.Count > 0 ? 
                    this._AchievementListView.GetItemRect(0).Height : 20);
            
            _achievementScrollBar.Minimum = 0;
            _achievementScrollBar.Maximum = Math.Max(0, itemCount);
            _achievementScrollBar.LargeChange = Math.Max(1, visibleItems);
            _achievementScrollBar.SmallChange = 1;
            
            // Get current scroll position
            if (this._AchievementListView.TopItem != null)
            {
                _achievementScrollBar.Value = Math.Min(this._AchievementListView.TopItem.Index, 
                    Math.Max(0, _achievementScrollBar.Maximum - _achievementScrollBar.LargeChange + 1));
            }
        }

        private void OnAchievementScrollBarScroll(object sender, ScrollEventArgs e)
        {
            if (this._AchievementListView.Items.Count > 0 && e.NewValue < this._AchievementListView.Items.Count)
            {
                this._AchievementListView.EnsureVisible(e.NewValue);
                try
                {
                    this._AchievementListView.TopItem = this._AchievementListView.Items[e.NewValue];
                }
                catch { }
            }
        }

        private void HideNativeScrollBar()
        {
            if (this._AchievementListView != null && this._AchievementListView.IsHandleCreated)
            {
                // VERY AGGRESSIVE: Hide scrollbar multiple ways
                ShowScrollBar(this._AchievementListView.Handle, SB_BOTH, false);
                ShowScrollBar(this._AchievementListView.Handle, SB_VERT, false);
                ShowScrollBar(this._AchievementListView.Handle, SB_HORZ, false);
                
                // Also remove from window style
                int style = GetWindowLong(this._AchievementListView.Handle, GWL_STYLE);
                bool needsUpdate = false;
                if ((style & WS_VSCROLL) != 0)
                {
                    style &= ~WS_VSCROLL;
                    needsUpdate = true;
                }
                if ((style & WS_HSCROLL) != 0)
                {
                    style &= ~WS_HSCROLL;
                    needsUpdate = true;
                }
                if (needsUpdate)
                {
                    SetWindowLong(this._AchievementListView.Handle, GWL_STYLE, style);
                }
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            if (_titleBar != null)
                _titleBar.Title = this.Text;
        }

        #region Owner Draw for Dark Theme

        private void OnTabControlDrawItem(object sender, DrawItemEventArgs e)
        {
            var tabControl = sender as TabControl;
            if (tabControl == null) return;

            var tabPage = tabControl.TabPages[e.Index];
            var tabBounds = tabControl.GetTabRect(e.Index);
            
            // Expand bounds slightly to cover any gaps
            var fillBounds = new Rectangle(tabBounds.X - 2, tabBounds.Y - 2, tabBounds.Width + 4, tabBounds.Height + 4);
            
            // Background color based on selection
            Color backColor = e.Index == tabControl.SelectedIndex 
                ? API.StoreThemeColors.Background 
                : API.StoreThemeColors.BackgroundDark;
            Color foreColor = API.StoreThemeColors.Foreground;

            using (var brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, fillBounds);
            }

            // Draw text centered
            var textSize = e.Graphics.MeasureString(tabPage.Text, e.Font);
            var textX = tabBounds.X + (tabBounds.Width - textSize.Width) / 2;
            var textY = tabBounds.Y + (tabBounds.Height - textSize.Height) / 2 + 2;

            using (var brush = new SolidBrush(foreColor))
            {
                e.Graphics.DrawString(tabPage.Text, e.Font, brush, textX, textY);
            }
        }

        private void OnTabControlPaint(object sender, PaintEventArgs e)
        {
            var tabControl = sender as TabControl;
            if (tabControl == null) return;

            // Since tabs are hidden, just fill everything with dark background
            using (var bgBrush = new SolidBrush(API.StoreThemeColors.Background))
            {
                // Fill entire control background
                e.Graphics.FillRectangle(bgBrush, 0, 0, tabControl.Width, tabControl.Height);
            }
        }

        private void OnTabPagePaint(object sender, PaintEventArgs e)
        {
            var tabPage = sender as TabPage;
            if (tabPage == null) return;

            // Fill the entire page with dark background to cover any system borders
            using (var brush = new SolidBrush(tabPage.BackColor))
            {
                e.Graphics.FillRectangle(brush, 0, 0, tabPage.Width, tabPage.Height);
            }
        }

        private void OnListViewDrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            var listView = sender as ListView;
            
            // Dark background for column headers - extend to fill any empty space on the right
            using (var brush = new SolidBrush(API.StoreThemeColors.BackgroundDark))
            {
                // Fill normal column header area
                e.Graphics.FillRectangle(brush, e.Bounds);
                
                // If this is the last visible column, fill remaining space to the right
                if (listView != null && e.ColumnIndex == listView.Columns.Count - 1)
                {
                    var remainingWidth = listView.ClientSize.Width - e.Bounds.Right;
                    if (remainingWidth > 0)
                    {
                        var remainingRect = new Rectangle(e.Bounds.Right, e.Bounds.Top, remainingWidth + 50, e.Bounds.Height);
                        e.Graphics.FillRectangle(brush, remainingRect);
                    }
                }
            }

            // Draw border
            using (var pen = new Pen(API.StoreThemeColors.ControlBorder))
            {
                e.Graphics.DrawLine(pen, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom);
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }

            // Draw text
            var textBounds = new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, e.Header.Text, e.Font, textBounds, 
                API.StoreThemeColors.Foreground, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        private void OnListViewDrawItem(object sender, DrawListViewItemEventArgs e)
        {
            // Fill background with dark color
            var bgColor = e.Item.Selected 
                ? API.StoreThemeColors.ListSelected 
                : API.StoreThemeColors.ListBackground;
            using (var brush = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }
            
            // Draw dark separator line at bottom
            using (var pen = new Pen(API.StoreThemeColors.ControlBorder))
            {
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }
            
            // Don't let system draw - we'll handle everything in DrawSubItem
            // e.DrawDefault = false would skip sub-items, so we need DrawDefault for sub-item trigger
        }

        private void OnListViewDrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            var listView = sender as ListView;
            
            // Fill background with dark color
            var bgColor = e.Item.Selected 
                ? API.StoreThemeColors.ListSelected 
                : API.StoreThemeColors.ListBackground;
            using (var brush = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }
            
            // First column (index 0) - draw checkbox and icon
            if (e.ColumnIndex == 0 && listView != null && listView.CheckBoxes)
            {
                int checkboxSize = 16;
                int padding = 4;
                int checkboxX = e.Bounds.X + padding;
                int checkboxY = e.Bounds.Y + (e.Bounds.Height - checkboxSize) / 2;
                
                // Draw dark checkbox background
                var checkboxRect = new Rectangle(checkboxX, checkboxY, checkboxSize, checkboxSize);
                using (var brush = new SolidBrush(API.StoreThemeColors.ControlBackground))
                {
                    e.Graphics.FillRectangle(brush, checkboxRect);
                }
                using (var pen = new Pen(API.StoreThemeColors.ControlBorder))
                {
                    e.Graphics.DrawRectangle(pen, checkboxRect);
                }
                
                // Draw checkmark if checked
                if (e.Item.Checked)
                {
                    using (var pen = new Pen(API.StoreThemeColors.Accent, 2))
                    {
                        // Draw checkmark
                        e.Graphics.DrawLine(pen, checkboxX + 3, checkboxY + 8, checkboxX + 6, checkboxY + 11);
                        e.Graphics.DrawLine(pen, checkboxX + 6, checkboxY + 11, checkboxX + 12, checkboxY + 4);
                    }
                }
                
                // Draw icon if present
                int iconOffset = checkboxX + checkboxSize + padding;
                if (listView.SmallImageList != null && e.Item.ImageIndex >= 0 && e.Item.ImageIndex < listView.SmallImageList.Images.Count)
                {
                    var img = listView.SmallImageList.Images[e.Item.ImageIndex];
                    e.Graphics.DrawImage(img, iconOffset, e.Bounds.Y + (e.Bounds.Height - img.Height) / 2);
                    iconOffset += img.Width + padding;
                }
                else if (listView.SmallImageList != null && !string.IsNullOrEmpty(e.Item.ImageKey) && listView.SmallImageList.Images.ContainsKey(e.Item.ImageKey))
                {
                    var img = listView.SmallImageList.Images[e.Item.ImageKey];
                    e.Graphics.DrawImage(img, iconOffset, e.Bounds.Y + (e.Bounds.Height - img.Height) / 2);
                    iconOffset += img.Width + padding;
                }
                
                // Draw text
                var textBounds = new Rectangle(iconOffset, e.Bounds.Y, e.Bounds.Right - iconOffset - 4, e.Bounds.Height);
                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, e.Item.Font ?? listView.Font, textBounds,
                    API.StoreThemeColors.Foreground, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            else
            {
                // Other columns - just draw text
                var textBounds = new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, e.Item.Font ?? (sender as ListView)?.Font, textBounds,
                    API.StoreThemeColors.Foreground, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            
            // Mark as handled - don't let system draw
            e.DrawDefault = false;
        }

        private void OnListViewPaint(object sender, PaintEventArgs e)
        {
            var listView = sender as ListView;
            if (listView == null) return;
            
            // Calculate total column width
            int totalColWidth = 0;
            foreach (ColumnHeader col in listView.Columns)
            {
                totalColWidth += col.Width;
            }
            
            // AGGRESSIVE: Paint over any white areas on the right side
            int headerHeight = 20; // Approximate header height
            
            // Fill the entire right side from column end to control edge
            if (totalColWidth < listView.ClientSize.Width)
            {
                // Header area (dark)
                using (var brush = new SolidBrush(API.StoreThemeColors.BackgroundDark))
                {
                    var headerRect = new Rectangle(totalColWidth, 0, listView.ClientSize.Width - totalColWidth + 50, headerHeight);
                    e.Graphics.FillRectangle(brush, headerRect);
                }
                
                // Body area (list background)
                using (var brush = new SolidBrush(API.StoreThemeColors.ListBackground))
                {
                    var bodyRect = new Rectangle(totalColWidth, headerHeight, listView.ClientSize.Width - totalColWidth + 50, listView.ClientSize.Height - headerHeight + 50);
                    e.Graphics.FillRectangle(brush, bodyRect);
                }
            }
            
            // Paint the bottom area if there are fewer items than can fit
            int itemCount = listView.Items.Count;
            int itemHeight = itemCount > 0 ? listView.GetItemRect(0).Height : 50;
            int totalItemsHeight = itemCount * itemHeight + headerHeight;
            
            if (totalItemsHeight < listView.ClientSize.Height)
            {
                using (var brush = new SolidBrush(API.StoreThemeColors.ListBackground))
                {
                    var bottomRect = new Rectangle(0, totalItemsHeight, listView.ClientSize.Width + 50, listView.ClientSize.Height - totalItemsHeight + 50);
                    e.Graphics.FillRectangle(brush, bottomRect);
                }
            }
        }

        #endregion
    }
}
