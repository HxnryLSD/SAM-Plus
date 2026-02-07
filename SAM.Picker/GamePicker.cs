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
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net; // Removed WebClient usage
using System.Net.Http; // Added
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks; // Added
using System.Windows.Forms;
using System.Xml.XPath;
using static SAM.Picker.InvariantShorthand;
using APITypes = SAM.API.Types;

namespace SAM.Picker
{
    internal partial class GamePicker : Form, IMessageFilter
    {
        private const int WM_MOUSEWHEEL = 0x020A;
        // Win32 API for hiding scrollbar
        private const int GWL_STYLE = -16;
        private const int WS_VSCROLL = 0x00200000;
        private const int WS_HSCROLL = 0x00100000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool GetScrollInfo(IntPtr hwnd, int nBar, ref SCROLLINFO lpsi);

        [StructLayout(LayoutKind.Sequential)]
        private struct SCROLLINFO
        {
            public uint cbSize;
            public uint fMask;
            public int nMin;
            public int nMax;
            public uint nPage;
            public int nPos;
            public int nTrackPos;
        }
        private const uint SIF_ALL = 0x17;

        private const int SB_VERT = 1;
        private const int LVM_SCROLL = 0x1014;

        private static System.Net.Http.HttpClient HttpClient => API.ServiceLocator.HttpClient;
        private bool _isDownloadingLogos = false; // Added
        private static readonly SemaphoreSlim _downloadSemaphore = new(5); // Max 5 parallel downloads

        private readonly API.StoreTitleBar _titleBar;
        private readonly TextBox _searchBox;
        private readonly Panel _searchPanel;
        private readonly Label _gameCountLabel;
        private readonly API.StoreScrollBar _scrollBar;
        private readonly API.Client _SteamClient;
        private readonly System.Windows.Forms.Timer _searchDebounceTimer;
        
        // Smooth scrolling
        private readonly System.Windows.Forms.Timer _smoothScrollTimer;
        private float _scrollVelocity = 0;
        private float _scrollAccumulator = 0;
        private const float SCROLL_FRICTION = 0.85f;
        private const float SCROLL_MIN_VELOCITY = 0.5f;

        private readonly Dictionary<uint, GameInfo> _Games;
        private readonly List<GameInfo> _FilteredGames;

        private readonly object _LogoLock;
        private readonly HashSet<string> _LogosAttempting;
        private readonly HashSet<string> _LogosAttempted;
        private readonly ConcurrentQueue<GameInfo> _LogoQueue;

        private readonly API.Callbacks.AppDataChanged _AppDataChangedCallback;

        public GamePicker(API.Client client)
        {
            this._Games = new();
            this._FilteredGames = new();
            this._LogoLock = new();
            this._LogosAttempting = new();
            this._LogosAttempted = new();
            this._LogoQueue = new();

            this.InitializeComponent();

            // Enable double buffering for smooth resizing
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true);

            // Set minimum size for the form
            this.MinimumSize = new Size(500, 400);

            // Setup custom title bar
            this.FormBorderStyle = FormBorderStyle.None;
            _titleBar = new API.StoreTitleBar();
            _titleBar.Title = this.Text;
            if (this.Icon != null)
            {
                _titleBar.TitleIcon = this.Icon.ToBitmap();
            }
            this.Controls.Add(_titleBar);
            _titleBar.Dock = DockStyle.None; // Disable dock for manual positioning with resize border
            _titleBar.SendToBack();

            // Hide the old ToolStrip
            this._PickerToolStrip.Visible = false;

            // Create modern search box
            _searchBox = new TextBox
            {
                Font = new Font("Segoe UI", 11F),
                BackColor = API.StoreThemeColors.ControlBackground,
                ForeColor = API.StoreThemeColors.Foreground,
                BorderStyle = BorderStyle.None,
                Height = 32,
                Location = new Point(12, _titleBar.Height + 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                PlaceholderText = "🔍 Spiel suchen... (Ctrl+F)"
            };
            _searchBox.Width = this.ClientSize.Width - 24;
            _searchBox.TextChanged += OnSearchTextChanged;

            // Initialize debounce timer (150ms delay)
            _searchDebounceTimer = new System.Windows.Forms.Timer { Interval = 150 };
            _searchDebounceTimer.Tick += OnSearchDebounceTimerTick;
            
            // Initialize smooth scroll timer (~60fps)
            _smoothScrollTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _smoothScrollTimer.Tick += OnSmoothScrollTick;

            _searchBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    _searchBox.Text = "";
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Enter && this._FilteredGames.Count > 0)
                {
                    // Select first game on Enter
                    if (this._GameListView.VirtualListSize > 0)
                    {
                        this._GameListView.SelectedIndices.Clear();
                        this._GameListView.SelectedIndices.Add(0);
                        OnActivateGame(this, EventArgs.Empty);
                    }
                    e.Handled = true;
                }
            };

            // Create search container panel for better styling
            _searchPanel = new Panel
            {
                BackColor = API.StoreThemeColors.ControlBackground,
                Location = new Point(12, _titleBar.Height + 8),
                Height = 36,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _searchPanel.Width = this.ClientSize.Width - 24;
            _searchPanel.Padding = new Padding(10, 8, 10, 8);
            
            _searchBox.Dock = DockStyle.Fill;
            _searchBox.Location = Point.Empty;
            _searchPanel.Controls.Add(_searchBox);
            this.Controls.Add(_searchPanel);

            // Create game count label (top right next to search)
            _gameCountLabel = new Label
            {
                Font = new Font("Segoe UI", 10F),
                ForeColor = API.StoreThemeColors.ForegroundDim,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                Height = 36
            };
            this.Controls.Add(_gameCountLabel);

            // Hide status strip (game count moved to top)
            this._PickerStatusStrip.Visible = false;

            // Adjust game list position
            int contentTop = _titleBar.Height + 8 + _searchPanel.Height + 8;
            this._GameListView.Location = new Point(0, contentTop);
            this._GameListView.Height = this.ClientSize.Height - contentTop - this._PickerStatusStrip.Height;
            this._GameListView.Width = this.ClientSize.Width - 12; // Leave space for custom scrollbar
            this._GameListView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // Enable built-in scrollbar hiding in MyListView
            this._GameListView.HideVerticalScrollBar = true;

            // Add custom scrollbar
            _scrollBar = new API.StoreScrollBar
            {
                Location = new Point(this.ClientSize.Width - 10, contentTop),
                Height = this._GameListView.Height
            };
            _scrollBar.Scroll += OnCustomScrollBarScroll;
            this.Controls.Add(_scrollBar);
            _scrollBar.BringToFront();

            // Handle mouse wheel on ListView - redirect to custom scrollbar
            this._GameListView.MouseWheel += OnListViewMouseWheel;
            this._GameListView.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down ||
                    e.KeyCode == Keys.PageUp || e.KeyCode == Keys.PageDown ||
                    e.KeyCode == Keys.Home || e.KeyCode == Keys.End)
                {
                    this.BeginInvoke((Action)UpdateScrollBar);
                }
            };
            
            // Apply Store theme
            ApplyStoreTheme();

            // Handle mouse wheel at form level for scrolling
            Application.AddMessageFilter(this);

            Bitmap blank = new(this._LogoImageList.ImageSize.Width, this._LogoImageList.ImageSize.Height);
            using (var g = Graphics.FromImage(blank))
            {
                // Use Store theme color for blank game icons
                g.Clear(API.StoreThemeColors.ControlBackground);
            }

            this._LogoImageList.Images.Add("Blank", blank);

            this._SteamClient = client;

            this._AppDataChangedCallback = client.CreateAndRegisterCallback<API.Callbacks.AppDataChanged>();
            this._AppDataChangedCallback.OnRun += this.OnAppDataChanged;

            // Create Store-styled context menu
            CreateContextMenu();

            // Setup keyboard shortcuts
            SetupKeyboardShortcuts();

            // Defer heavy loading to after form is shown (async init)
            this.Shown += OnFormShownAsync;
        }

        private async void OnFormShownAsync(object sender, EventArgs e)
        {
            // Unsubscribe to prevent multiple calls
            this.Shown -= OnFormShownAsync;

            // Brief delay to let the UI render first
            await Task.Delay(50);

            // Now load games asynchronously
            this.AddGames();
        }

        /// <summary>
        /// Sets up keyboard shortcuts for accessibility.
        /// </summary>
        private void SetupKeyboardShortcuts()
        {
            this.KeyPreview = true;
            this.KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // F5 = Refresh
            if (e.KeyCode == Keys.F5)
            {
                OnRefresh(this, EventArgs.Empty);
                e.Handled = true;
            }
            // Ctrl+F = Focus search
            else if (e.Control && e.KeyCode == Keys.F)
            {
                this._searchBox.Focus();
                this._searchBox.SelectAll();
                e.Handled = true;
            }
            // Enter = Open selected game
            else if (e.KeyCode == Keys.Enter && this._GameListView.Focused)
            {
                OnActivateGame(this._GameListView, EventArgs.Empty);
                e.Handled = true;
            }
            // Escape = Clear search
            else if (e.KeyCode == Keys.Escape)
            {
                this._searchBox.Text = "";
                RefreshGames();
                e.Handled = true;
            }
        }

        private void OnSearchTextChanged(object sender, EventArgs e)
        {
            // Debounce: Reset timer on each keystroke, search after 150ms pause
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        private void OnSearchDebounceTimerTick(object sender, EventArgs e)
        {
            _searchDebounceTimer.Stop();
            RefreshGames();
        }

        private void HideNativeScrollBar()
        {
            if (this._GameListView.IsHandleCreated)
            {
                // Hide the vertical scrollbar
                ShowScrollBar(this._GameListView.Handle, SB_VERT, false);
                
                // Also remove the scrollbar style to prevent it from reappearing
                int style = GetWindowLong(this._GameListView.Handle, GWL_STYLE);
                style &= ~WS_VSCROLL;
                SetWindowLong(this._GameListView.Handle, GWL_STYLE, style);
            }
        }

        private void OnCustomScrollBarScroll(object sender, ScrollEventArgs e)
        {
            if (this._FilteredGames.Count > 0 && e.NewValue < this._FilteredGames.Count)
            {
                this._GameListView.EnsureVisible(e.NewValue);
                // Try to make this the top item
                if (this._GameListView.VirtualListSize > e.NewValue)
                {
                    try
                    {
                        this._GameListView.TopItem = this._GameListView.Items[e.NewValue];
                    }
                    catch { }
                }
            }
        }

        private void UpdateScrollBar()
        {
            if (_scrollBar == null) return;

            int itemCount = this._FilteredGames.Count;
            int itemHeight = this._LogoImageList.ImageSize.Height + 4; // Approximate item height
            int visibleItems = Math.Max(1, this._GameListView.ClientSize.Height / itemHeight);

            _scrollBar.Minimum = 0;
            _scrollBar.Maximum = Math.Max(0, itemCount);
            _scrollBar.LargeChange = visibleItems;
            _scrollBar.SmallChange = 1;

            // TopItem is only available in Details/List view, not in Tile/LargeIcon
            // Keep current value or reset to 0
            if (_scrollBar.Value > itemCount)
            {
                _scrollBar.Value = 0;
            }
        }

        private void OnListViewMouseWheel(object sender, MouseEventArgs e)
        {
            if (_scrollBar == null || this._FilteredGames.Count == 0) return;

            // Add velocity for smooth scrolling (negative because wheel up = scroll up)
            float delta = -e.Delta * 0.4f;
            _scrollVelocity += delta;
            
            // Start smooth scroll timer if not running
            if (!_smoothScrollTimer.Enabled)
            {
                _smoothScrollTimer.Start();
            }

            // Mark event as handled to prevent default scrolling
            if (e is HandledMouseEventArgs hme)
            {
                hme.Handled = true;
            }
        }

        private void OnSmoothScrollTick(object sender, EventArgs e)
        {
            if (Math.Abs(_scrollVelocity) < SCROLL_MIN_VELOCITY)
            {
                _smoothScrollTimer.Stop();
                _scrollVelocity = 0;
                _scrollAccumulator = 0;
                return;
            }

            // Add velocity to accumulator
            _scrollAccumulator += _scrollVelocity;
            
            // Extract whole pixels to scroll
            int pixelsToScroll = (int)_scrollAccumulator;
            if (pixelsToScroll != 0)
            {
                _scrollAccumulator -= pixelsToScroll;
                
                // Use LVM_SCROLL for pixel-based scrolling
                if (this._GameListView != null && this._GameListView.IsHandleCreated)
                {
                    SendMessage(this._GameListView.Handle, LVM_SCROLL, IntPtr.Zero, (IntPtr)pixelsToScroll);
                    
                    // Update custom scrollbar to match current scroll position
                    UpdateScrollBarFromListView();
                    
                    // Hide native scrollbar
                    HideNativeScrollBar();
                }
            }

            // Apply friction
            _scrollVelocity *= SCROLL_FRICTION;
        }

        private void UpdateScrollBarFromListView()
        {
            if (_scrollBar == null || this._GameListView == null || !this._GameListView.IsHandleCreated) return;
            
            // Get scroll info from the ListView's internal scrollbar
            var si = new SCROLLINFO { cbSize = (uint)Marshal.SizeOf<SCROLLINFO>(), fMask = SIF_ALL };
            if (GetScrollInfo(this._GameListView.Handle, SB_VERT, ref si))
            {
                // Calculate relative position
                int range = Math.Max(1, si.nMax - si.nMin - (int)si.nPage + 1);
                int scrollbarRange = Math.Max(1, _scrollBar.Maximum - _scrollBar.Minimum - _scrollBar.LargeChange + 1);
                int newValue = _scrollBar.Minimum + (int)((float)si.nPos / range * scrollbarRange);
                
                // Clamp and set scrollbar value
                newValue = Math.Max(_scrollBar.Minimum, Math.Min(newValue, _scrollBar.Maximum - _scrollBar.LargeChange + 1));
                if (newValue != _scrollBar.Value)
                {
                    _scrollBar.Value = newValue;
                }
            }
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_MOUSEWHEEL)
            {
                // Check if mouse is over our form
                Point screenPoint = Cursor.Position;
                if (this.Bounds.Contains(screenPoint))
                {
                    // Get the delta from the message
                    int delta = (int)(m.WParam.ToInt64() >> 16);
                    
                    // Create MouseEventArgs and call our handler
                    var e = new HandledMouseEventArgs(MouseButtons.None, 0, 0, 0, delta);
                    OnListViewMouseWheel(this, e);
                    
                    return true; // Message handled, don't pass to other controls
                }
            }
            return false;
        }

        private ContextMenuStrip _gameContextMenu;

        /// <summary>
        /// Creates a Store-themed context menu for the game list.
        /// </summary>
        private void CreateContextMenu()
        {
            _gameContextMenu = new ContextMenuStrip
            {
                BackColor = API.StoreThemeColors.BackgroundLight,
                ForeColor = API.StoreThemeColors.Foreground,
                ShowImageMargin = true,
                Renderer = new StoreToolStripRenderer()
            };

            var openItem = new ToolStripMenuItem("🎮  Open Achievement Manager")
            {
                BackColor = API.StoreThemeColors.BackgroundLight,
                ForeColor = API.StoreThemeColors.Foreground,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            openItem.Click += (s, e) => OnActivateGame(this._GameListView, EventArgs.Empty);

            var copyIdItem = new ToolStripMenuItem("📋  Copy Game ID")
            {
                BackColor = API.StoreThemeColors.BackgroundLight,
                ForeColor = API.StoreThemeColors.Foreground
            };
            copyIdItem.Click += OnCopyGameId;

            var copyNameItem = new ToolStripMenuItem("📝  Copy Game Name")
            {
                BackColor = API.StoreThemeColors.BackgroundLight,
                ForeColor = API.StoreThemeColors.Foreground
            };
            copyNameItem.Click += OnCopyGameName;

            var separator = new ToolStripSeparator();

            var steamPageItem = new ToolStripMenuItem("🌐  Open Steam Store Page")
            {
                BackColor = API.StoreThemeColors.BackgroundLight,
                ForeColor = API.StoreThemeColors.Foreground
            };
            steamPageItem.Click += OnOpenSteamPage;

            _gameContextMenu.Items.AddRange(new ToolStripItem[] 
            { 
                openItem, 
                new ToolStripSeparator(),
                copyIdItem, 
                copyNameItem,
                new ToolStripSeparator(),
                steamPageItem
            });

            this._GameListView.ContextMenuStrip = _gameContextMenu;
        }

        private GameInfo GetSelectedGame()
        {
            var focusedItem = this._GameListView.FocusedItem;
            var index = focusedItem?.Index ?? -1;
            if (index < 0 || index >= this._FilteredGames.Count)
                return null;
            return this._FilteredGames[index];
        }

        private void OnCopyGameId(object sender, EventArgs e)
        {
            var game = GetSelectedGame();
            if (game != null)
            {
                Clipboard.SetText(game.Id.ToString(CultureInfo.InvariantCulture));
            }
        }

        private void OnCopyGameName(object sender, EventArgs e)
        {
            var game = GetSelectedGame();
            if (game?.Name != null)
            {
                Clipboard.SetText(game.Name);
            }
        }

        private void OnOpenSteamPage(object sender, EventArgs e)
        {
            var game = GetSelectedGame();
            if (game != null)
            {
                var url = $"https://store.steampowered.com/app/{game.Id}";
                try
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch
                {
                    // Failed to open URL
                }
            }
        }

        private void OnAppDataChanged(APITypes.AppDataChanged param)
        {
            if (param.Result == false)
            {
                return;
            }

            if (this._Games.TryGetValue(param.Id, out var game) == false)
            {
                return;
            }

            game.Name = this._SteamClient.SteamApps001.GetAppData(game.Id, "name");

            this.AddGameToLogoQueue(game);
            this.DownloadNextLogo();
        }

        private void DoDownloadList() { } // obsolete
        
        private async void LoadGamesAsync()
        {
            this._PickerStatusLabel.Text = "Loading game list...";

            try 
            {
                List<KeyValuePair<uint, string>> pairs = new();

                // Try to load from local cache first
                var cachedGames = await API.GameListCache.TryLoadFromCacheAsync();
                if (cachedGames != null)
                {
                    this._PickerStatusLabel.Text = "Loaded from cache...";
                    foreach (var entry in cachedGames)
                    {
                        pairs.Add(new(entry.Id, entry.Type));
                    }
                }
                else
                {
                    // Download fresh game list
                    this._PickerStatusLabel.Text = "Downloading game list...";
                    var bytes = await HttpClient.GetByteArrayAsync(API.AppConfig.GamesListUrl);
                    
                    using (MemoryStream stream = new(bytes, false))
                    {
                        XPathDocument document = new(stream);
                        var navigator = document.CreateNavigator();
                        var nodes = navigator.Select("/games/game");
                        while (nodes.MoveNext() == true)
                        {
                            string type = nodes.Current.GetAttribute("type", "");
                            if (string.IsNullOrEmpty(type) == true)
                            {
                                type = "normal";
                            }
                            pairs.Add(new((uint)nodes.Current.ValueAsLong, type));
                        }
                    }

                    // Save to cache for next time (fire and forget)
                    var cacheEntries = pairs.ConvertAll(p => new API.GameListCache.GameCacheEntry(p.Key, p.Value));
                    #pragma warning disable CS4014 // Fire and forget intentionally
                    API.GameListCache.SaveToCacheAsync(cacheEntries);
                    #pragma warning restore CS4014
                }

                this._PickerStatusLabel.Text = "Checking game ownership...";
                foreach (var kv in pairs)
                {
                    this.AddGame(kv.Key, kv.Value);
                }
            }
            catch (Exception e)
            {
                this.AddDefaultGames();
                MessageBox.Show(e.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            this.RefreshGames();
            this._RefreshGamesButton.Enabled = true;
            this.DownloadNextLogo();
        }

        /*
        private void DoDownloadList(object sender, DoWorkEventArgs e)
        {
           // Removed
        }
        */

        /*
        private void OnDownloadList(object sender, RunWorkerCompletedEventArgs e)
        {
           // Removed
        }
        */

        private void RefreshGames()
        {
            var nameSearch = this._searchBox?.Text?.Length > 0
                ? this._searchBox.Text
                : null;

            // Show all game types (no filter needed)
            var wantNormals = true;
            var wantDemos = true;
            var wantMods = true;
            var wantJunk = true;

            this._FilteredGames.Clear();
            foreach (var info in this._Games.Values.OrderBy(gi => gi.Name))
            {
                if (nameSearch != null &&
                    info.Name.IndexOf(nameSearch, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                bool wanted = info.Type switch
                {
                    "normal" => wantNormals,
                    "demo" => wantDemos,
                    "mod" => wantMods,
                    "junk" => wantJunk,
                    _ => true,
                };
                if (wanted == false)
                {
                    continue;
                }

                this._FilteredGames.Add(info);
            }

            this._GameListView.VirtualListSize = this._FilteredGames.Count;
            
            // Update game count label with simple text
            if (_gameCountLabel != null)
            {
                int displayed = this._GameListView.Items.Count;
                int total = this._Games.Count;
                _gameCountLabel.Text = displayed == total 
                    ? $"{total} Spiele" 
                    : $"{displayed} / {total} Spiele";
            }

            if (this._GameListView.Items.Count > 0)
            {
                this._GameListView.Items[0].Selected = true;
                // Don't steal focus from search box
            }

            // Update custom scrollbar
            UpdateScrollBar();
            HideNativeScrollBar();
        }

        private void OnGameListViewRetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            var info = this._FilteredGames[e.ItemIndex];
            e.Item = info.Item = new()
            {
                Text = info.Name,
                ImageIndex = info.ImageIndex,
            };
        }

        private void OnGameListViewSearchForVirtualItem(object sender, SearchForVirtualItemEventArgs e)
        {
            if (e.Direction != SearchDirectionHint.Down || e.IsTextSearch == false)
            {
                return;
            }

            var count = this._FilteredGames.Count;
            if (count < 2)
            {
                return;
            }

            var text = e.Text;
            int startIndex = e.StartIndex;

            Predicate<GameInfo> predicate;
            /*if (e.IsPrefixSearch == true)*/
            {
                predicate = gi => gi.Name != null && gi.Name.StartsWith(text, StringComparison.CurrentCultureIgnoreCase);
            }
            /*else
            {
                predicate = gi => gi.Name != null && string.Compare(gi.Name, text, StringComparison.CurrentCultureIgnoreCase) == 0;
            }*/

            int index;
            if (e.StartIndex >= count)
            {
                // starting from the last item in the list
                index = this._FilteredGames.FindIndex(0, startIndex - 1, predicate);
            }
            else if (startIndex <= 0)
            {
                // starting from the first item in the list
                index = this._FilteredGames.FindIndex(0, count, predicate);
            }
            else
            {
                index = this._FilteredGames.FindIndex(startIndex, count - startIndex, predicate);
                if (index < 0)
                {
                    index = this._FilteredGames.FindIndex(0, startIndex - 1, predicate);
                }
            }

            e.Index = index < 0 ? -1 : index;
        }

        /*
        private void DoDownloadLogo(object sender, DoWorkEventArgs e)
        {
             // Removed
        }
        */

        /*
        private void OnDownloadLogo(object sender, RunWorkerCompletedEventArgs e)
        {
            // Removed
        }
        */

        private async void DownloadNextLogo()
        {
            if (this._isDownloadingLogos) return;
            this._isDownloadingLogos = true;

            try 
            {
                // Collect visible items to download
                var itemsToDownload = new List<GameInfo>();
                while (this._LogoQueue.TryDequeue(out var info))
                {
                    if (info.Item == null) continue;
                    if (this._FilteredGames.Contains(info) == false ||
                        info.Item.Bounds.IntersectsWith(this._GameListView.ClientRectangle) == false)
                    {
                        this._LogosAttempting.Remove(info.ImageUrl);
                        continue;
                    }
                    itemsToDownload.Add(info);
                }

                if (itemsToDownload.Count == 0)
                {
                    this._DownloadStatusLabel.Visible = false;
                    return;
                }

                this._DownloadStatusLabel.Text = $"Downloading {itemsToDownload.Count} game icons...";
                this._DownloadStatusLabel.Visible = true;

                // Download in parallel with throttling
                var downloadTasks = itemsToDownload.Select(info => DownloadLogoAsync(info)).ToList();
                await Task.WhenAll(downloadTasks);
            }
            finally
            {
                this._isDownloadingLogos = false;
                if (!IsDisposed)
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

        private async Task DownloadLogoAsync(GameInfo info)
        {
            await _downloadSemaphore.WaitAsync();
            try
            {
                if (this.IsDisposed) return;

                this._LogosAttempted.Add(info.ImageUrl);

                // Check cache first
                var cachedBitmap = API.IconCache.Get(info.ImageUrl);
                if (cachedBitmap != null)
                {
                    if (!this.IsDisposed)
                    {
                        this.Invoke(() =>
                        {
                            this._GameListView.BeginUpdate();
                            var imageIndex = this._LogoImageList.Images.Count;
                            this._LogoImageList.Images.Add(info.ImageUrl, cachedBitmap);
                            info.ImageIndex = imageIndex;
                            this._GameListView.EndUpdate();
                        });
                    }
                    return;
                }

                // Download and cache
                var data = await HttpClient.GetByteArrayAsync(info.ImageUrl);
                var bitmap = API.IconCache.Store(info.ImageUrl, data);

                if (!this.IsDisposed && bitmap != null)
                {
                    this.Invoke(() =>
                    {
                        this._GameListView.BeginUpdate();
                        var imageIndex = this._LogoImageList.Images.Count;
                        this._LogoImageList.Images.Add(info.ImageUrl, bitmap);
                        info.ImageIndex = imageIndex;
                        this._GameListView.EndUpdate();
                    });
                }
            }
            catch
            {
                // Failed - logo stays blank
            }
            finally
            {
                _downloadSemaphore.Release();
            }
        }

        private string GetGameImageUrl(uint id)
        {
            string candidate;

            var currentLanguage = this._SteamClient.SteamApps008.GetCurrentGameLanguage();

            candidate = this._SteamClient.SteamApps001.GetAppData(id, _($"small_capsule/{currentLanguage}"));
            if (string.IsNullOrEmpty(candidate) == false)
            {
                return _($"{API.AppConfig.SteamCloudflareBaseUrl}/{id}/{candidate}");
            }

            if (currentLanguage != "english")
            {
                candidate = this._SteamClient.SteamApps001.GetAppData(id, "small_capsule/english");
                if (string.IsNullOrEmpty(candidate) == false)
                {
                    return _($"{API.AppConfig.SteamCloudflareBaseUrl}/{id}/{candidate}");
                }
            }

            candidate = this._SteamClient.SteamApps001.GetAppData(id, "logo");
            if (string.IsNullOrEmpty(candidate) == false)
            {
                return _($"{API.AppConfig.SteamCdnBaseUrl}/{id}/{candidate}.jpg");
            }

            return null;
        }

        private void AddGameToLogoQueue(GameInfo info)
        {
            if (info.ImageIndex > 0)
            {
                return;
            }

            var imageUrl = GetGameImageUrl(info.Id);
            if (string.IsNullOrEmpty(imageUrl) == true)
            {
                return;
            }

            info.ImageUrl = imageUrl;

            int imageIndex = this._LogoImageList.Images.IndexOfKey(imageUrl);
            if (imageIndex >= 0)
            {
                info.ImageIndex = imageIndex;
            }
            else if (
                this._LogosAttempting.Contains(imageUrl) == false &&
                this._LogosAttempted.Contains(imageUrl) == false)
            {
                this._LogosAttempting.Add(imageUrl);
                this._LogoQueue.Enqueue(info);
            }
        }

        private bool OwnsGame(uint id)
        {
            return this._SteamClient.SteamApps008.IsSubscribedApp(id);
        }

        private void AddGame(uint id, string type)
        {
            if (this._Games.ContainsKey(id) == true)
            {
                return;
            }

            if (this.OwnsGame(id) == false)
            {
                return;
            }

            GameInfo info = new(id, type);
            info.Name = this._SteamClient.SteamApps001.GetAppData(info.Id, "name");
            this._Games.Add(id, info);
        }

        private void AddGames()
        {
            this._Games.Clear();
            this._RefreshGamesButton.Enabled = false;
            //this._ListWorker.RunWorkerAsync();
            this.LoadGamesAsync();
        }

        private void AddDefaultGames()
        {
            this.AddGame(480, "normal"); // Spacewar
        }

        private void OnTimer(object sender, EventArgs e)
        {
            this._CallbackTimer.Enabled = false;
            this._SteamClient.RunCallbacks(false);
            this._CallbackTimer.Enabled = true;
        }

        private void OnActivateGame(object sender, EventArgs e)
        {
            var focusedItem = (sender as MyListView)?.FocusedItem;
            var index = focusedItem != null ? focusedItem.Index : -1;
            if (index < 0 || index >= this._FilteredGames.Count)
            {
                return;
            }

            var info = this._FilteredGames[index];
            if (info == null)
            {
                return;
            }

            try
            {
                Process.Start("SAM.Game.exe", info.Id.ToString(CultureInfo.InvariantCulture));
            }
            catch (Win32Exception)
            {
                MessageBox.Show(
                    this,
                    "Failed to start SAM.Game.exe.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            this._AddGameTextBox.Text = "";
            // Invalidate cache to force fresh download
            API.GameListCache.Invalidate();
            this.AddGames();
        }

        private void OnAddGame(object sender, EventArgs e)
        {
            uint id;

            if (uint.TryParse(this._AddGameTextBox.Text, out id) == false)
            {
                MessageBox.Show(
                    this,
                    "Please enter a valid game ID.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (this.OwnsGame(id) == false)
            {
                MessageBox.Show(this, "You don't own that game.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            while (this._LogoQueue.TryDequeue(out var logo) == true)
            {
                // clear the download queue because we will be showing only one app
                this._LogosAttempted.Remove(logo.ImageUrl);
            }

            this._AddGameTextBox.Text = "";
            this._Games.Clear();
            this.AddGame(id, "normal");
            this._FilterGamesMenuItem.Checked = true;
            this.RefreshGames();
            this.DownloadNextLogo();
        }

        private void OnFilterUpdate(object sender, EventArgs e)
        {
            this.RefreshGames();
        }

        /// <summary>
        /// Applies the Store theme to all controls.
        /// </summary>
        private void ApplyStoreTheme()
        {
            var colors = typeof(API.StoreThemeColors);
            
            // Form background
            this.BackColor = API.StoreThemeColors.Background;
            this.ForeColor = API.StoreThemeColors.Foreground;

            // Title bar styling
            _titleBar?.ApplyTheme();
            
            // Search box styling
            if (_searchBox != null)
            {
                _searchBox.BackColor = API.StoreThemeColors.ControlBackground;
                _searchBox.ForeColor = API.StoreThemeColors.Foreground;
                if (_searchBox.Parent is Panel searchPanel)
                {
                    searchPanel.BackColor = API.StoreThemeColors.ControlBackground;
                }
            }
            
            // Game count label styling
            if (_gameCountLabel != null)
            {
                _gameCountLabel.ForeColor = API.StoreThemeColors.ForegroundDim;
            }
            
            // ListView (Game List) styling
            this._GameListView.BackColor = API.StoreThemeColors.ListBackground;
            this._GameListView.ForeColor = API.StoreThemeColors.Foreground;

            // Custom scrollbar styling
            _scrollBar?.ApplyTheme();
            
            // StatusStrip styling
            this._PickerStatusStrip.BackColor = API.StoreThemeColors.BackgroundDark;
            this._PickerStatusStrip.ForeColor = API.StoreThemeColors.Foreground;
            foreach (ToolStripItem item in this._PickerStatusStrip.Items)
            {
                item.BackColor = API.StoreThemeColors.BackgroundDark;
                item.ForeColor = API.StoreThemeColors.ForegroundDim;
            }
        }

        private void OnGameListViewDrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;

            if (e.Item.Bounds.IntersectsWith(this._GameListView.ClientRectangle) == false)
            {
                return;
            }

            var info = this._FilteredGames[e.ItemIndex];
            if (info.ImageIndex <= 0)
            {
                this.AddGameToLogoQueue(info);
                this.DownloadNextLogo();
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
            int padding = 12;
            
            // Position title bar with resize border margin (top, left, right)
            _titleBar.Location = new Point(border, border);
            _titleBar.Width = this.ClientSize.Width - (border * 2);
            
            int searchAreaTop = border + titleBarHeight + 8;
            int searchAreaHeight = 36;
            int countLabelWidth = 120;
            
            // Update search panel position and size (leave space for count label)
            if (_searchPanel != null)
            {
                _searchPanel.Location = new Point(border + padding, searchAreaTop);
                _searchPanel.Width = this.ClientSize.Width - (border * 2) - (padding * 2) - countLabelWidth - 8;
            }
            
            // Position game count label (top right next to search)
            if (_gameCountLabel != null)
            {
                _gameCountLabel.Location = new Point(this.ClientSize.Width - border - padding - countLabelWidth, searchAreaTop);
                _gameCountLabel.Width = countLabelWidth;
            }
            
            // Calculate content area (no status strip anymore)
            int contentTop = searchAreaTop + searchAreaHeight + 8;
            int scrollbarWidth = 10;
            
            // Update game list position and size with resize border margins (full width)
            if (this._GameListView != null)
            {
                this._GameListView.Location = new Point(border, contentTop);
                this._GameListView.Width = this.ClientSize.Width - (border * 2);
                this._GameListView.Height = Math.Max(50, this.ClientSize.Height - contentTop - border);
                
                // Hide native scrollbar after resize
                HideNativeScrollBar();
            }

            // Update custom scrollbar position (inside game list area, overlapping)
            if (_scrollBar != null && this._GameListView != null)
            {
                _scrollBar.Location = new Point(this._GameListView.Right - scrollbarWidth - 2, contentTop + 2);
                _scrollBar.Height = this._GameListView.Height - 4;
                _scrollBar.BringToFront();
                UpdateScrollBar();
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            if (_titleBar != null)
                _titleBar.Title = this.Text;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Clean up timers
            _searchDebounceTimer?.Stop();
            _searchDebounceTimer?.Dispose();
            _smoothScrollTimer?.Stop();
            _smoothScrollTimer?.Dispose();
            
            // Remove message filter
            Application.RemoveMessageFilter(this);
            
            base.OnFormClosed(e);
        }
    }
}
