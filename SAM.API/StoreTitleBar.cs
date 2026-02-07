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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SAM.API
{
    /// <summary>
    /// Modern custom title bar control with Store theme styling.
    /// Features smooth hover effects and custom-drawn window control buttons.
    /// </summary>
    public class StoreTitleBar : Panel
    {
        // Win32 constants for window dragging
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        private Form _parentForm;
        private Label _titleLabel;
        private PictureBox _iconBox;
        private TitleBarButton _minimizeButton;
        private TitleBarButton _maximizeButton;
        private TitleBarButton _closeButton;
        private bool _showIcon = true;
        private bool _showMinimize = true;
        private bool _showMaximize = true;
        
        // For snap-away feature
        private Point _dragStartCursor;
        private Point _dragStartLocation;
        private bool _isDragging;
        private Rectangle _restoreBounds;

        /// <summary>
        /// Title text displayed in the title bar.
        /// </summary>
        public string Title
        {
            get => _titleLabel?.Text ?? "";
            set
            {
                if (_titleLabel != null)
                    _titleLabel.Text = value;
            }
        }

        /// <summary>
        /// Icon displayed in the title bar.
        /// </summary>
        public Image TitleIcon
        {
            get => _iconBox?.Image;
            set
            {
                if (_iconBox != null)
                    _iconBox.Image = value;
            }
        }

        /// <summary>
        /// Show/hide the minimize button.
        /// </summary>
        public bool ShowMinimizeButton
        {
            get => _showMinimize;
            set
            {
                _showMinimize = value;
                if (_minimizeButton != null)
                    _minimizeButton.Visible = value;
            }
        }

        /// <summary>
        /// Show/hide the maximize button.
        /// </summary>
        public bool ShowMaximizeButton
        {
            get => _showMaximize;
            set
            {
                _showMaximize = value;
                if (_maximizeButton != null)
                    _maximizeButton.Visible = value;
            }
        }

        /// <summary>
        /// Show/hide the icon.
        /// </summary>
        public bool ShowIcon
        {
            get => _showIcon;
            set
            {
                _showIcon = value;
                if (_iconBox != null)
                    _iconBox.Visible = value;
            }
        }

        public StoreTitleBar()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Panel settings - taller for modern look
            this.Height = 34;
            this.Dock = DockStyle.Top;
            this.BackColor = StoreThemeColors.BackgroundDark;
            this.Padding = new Padding(8, 0, 0, 0);

            // Icon
            _iconBox = new PictureBox
            {
                Size = new Size(18, 18),
                Location = new Point(10, 8),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            this.Controls.Add(_iconBox);

            // Title label
            _titleLabel = new Label
            {
                AutoSize = false,
                Location = new Point(34, 0),
                Height = 34,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = StoreThemeColors.Foreground,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            _titleLabel.MouseDown += OnTitleBarMouseDown;
            _titleLabel.MouseMove += OnTitleBarMouseMove;
            _titleLabel.MouseUp += OnTitleBarMouseUp;
            _titleLabel.DoubleClick += OnTitleBarDoubleClick;
            this.Controls.Add(_titleLabel);

            // Close button (X) - red hover
            _closeButton = new TitleBarButton(TitleBarButtonType.Close)
            {
                HoverBackColor = Color.FromArgb(232, 17, 35)
            };
            _closeButton.Click += (s, e) => _parentForm?.Close();
            this.Controls.Add(_closeButton);

            // Maximize button (□)
            _maximizeButton = new TitleBarButton(TitleBarButtonType.Maximize);
            _maximizeButton.Click += OnMaximizeClick;
            this.Controls.Add(_maximizeButton);

            // Minimize button (─)
            _minimizeButton = new TitleBarButton(TitleBarButtonType.Minimize);
            _minimizeButton.Click += (s, e) =>
            {
                if (_parentForm != null)
                    _parentForm.WindowState = FormWindowState.Minimized;
            };
            this.Controls.Add(_minimizeButton);

            // Enable dragging on the panel itself
            this.MouseDown += OnTitleBarMouseDown;
            this.MouseMove += OnTitleBarMouseMove;
            this.MouseUp += OnTitleBarMouseUp;
            this.DoubleClick += OnTitleBarDoubleClick;

            this.ResumeLayout(false);
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);

            if (this.Parent is Form form)
            {
                _parentForm = form;
                
                // Remove default title bar
                form.FormBorderStyle = FormBorderStyle.None;
                
                // Set initial title from form
                if (string.IsNullOrEmpty(_titleLabel.Text))
                    _titleLabel.Text = form.Text;

                // Set icon from form
                if (_iconBox.Image == null && form.Icon != null)
                    _iconBox.Image = form.Icon.ToBitmap();

                // Handle form resize to reposition buttons
                form.Resize += OnFormResize;
                
                // Initial positioning
                UpdateLayout();
            }
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            UpdateLayout();
        }

        private void OnFormResize(object sender, EventArgs e)
        {
            UpdateLayout();
            UpdateMaximizeButton();
        }

        private void UpdateLayout()
        {
            if (_closeButton == null) return;

            int buttonWidth = 46;
            int rightOffset = 0;

            // Position close button
            _closeButton.Location = new Point(this.Width - buttonWidth - rightOffset, 0);
            rightOffset += buttonWidth;

            // Position maximize button
            if (_showMaximize)
            {
                _maximizeButton.Location = new Point(this.Width - buttonWidth - rightOffset, 0);
                rightOffset += buttonWidth;
            }

            // Position minimize button
            if (_showMinimize)
            {
                _minimizeButton.Location = new Point(this.Width - buttonWidth - rightOffset, 0);
                rightOffset += buttonWidth;
            }

            // Update title label width
            int titleStart = _showIcon ? 34 : 10;
            _titleLabel.Location = new Point(titleStart, 0);
            _titleLabel.Width = this.Width - titleStart - rightOffset - 8;
        }

        private void UpdateMaximizeButton()
        {
            if (_parentForm == null || _maximizeButton == null) return;
            _maximizeButton.IsMaximized = _parentForm.WindowState == FormWindowState.Maximized;
        }

        private void OnTitleBarMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _parentForm != null)
            {
                _isDragging = true;
                _dragStartCursor = Cursor.Position;
                _dragStartLocation = _parentForm.Location;
                
                // Store restore bounds when starting to drag from maximized
                if (_parentForm.WindowState == FormWindowState.Maximized)
                {
                    _restoreBounds = _parentForm.RestoreBounds;
                }
            }
        }

        private void OnTitleBarMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || _parentForm == null) return;

            Point currentCursor = Cursor.Position;
            int deltaX = currentCursor.X - _dragStartCursor.X;
            int deltaY = currentCursor.Y - _dragStartCursor.Y;

            // Snap-away from maximized state
            if (_parentForm.WindowState == FormWindowState.Maximized)
            {
                // Only snap away if we've moved enough
                if (Math.Abs(deltaY) > 5)
                {
                    _parentForm.WindowState = FormWindowState.Normal;
                    
                    // Calculate new position so cursor is proportionally on title bar
                    float cursorRatioX = (float)_dragStartCursor.X / Screen.PrimaryScreen.WorkingArea.Width;
                    int newX = currentCursor.X - (int)(_restoreBounds.Width * cursorRatioX);
                    int newY = currentCursor.Y - e.Y;
                    
                    _parentForm.Location = new Point(newX, newY);
                    _dragStartLocation = _parentForm.Location;
                    _dragStartCursor = currentCursor;
                    
                    UpdateMaximizeButton();
                }
                return;
            }

            // Normal dragging
            _parentForm.Location = new Point(
                _dragStartLocation.X + deltaX,
                _dragStartLocation.Y + deltaY
            );
        }

        private void OnTitleBarMouseUp(object sender, MouseEventArgs e)
        {
            if (!_isDragging || _parentForm == null)
            {
                _isDragging = false;
                return;
            }

            _isDragging = false;

            // Snap to top = maximize
            if (Cursor.Position.Y <= 0 && _parentForm.WindowState != FormWindowState.Maximized)
            {
                _parentForm.WindowState = FormWindowState.Maximized;
                UpdateMaximizeButton();
            }
        }

        private void OnTitleBarDoubleClick(object sender, EventArgs e)
        {
            OnMaximizeClick(sender, e);
        }

        private void OnMaximizeClick(object sender, EventArgs e)
        {
            if (_parentForm == null) return;

            if (_parentForm.WindowState == FormWindowState.Maximized)
            {
                _parentForm.WindowState = FormWindowState.Normal;
            }
            else
            {
                _parentForm.WindowState = FormWindowState.Maximized;
            }

            UpdateMaximizeButton();
        }

        /// <summary>
        /// Applies theme colors to the title bar.
        /// </summary>
        public void ApplyTheme()
        {
            this.BackColor = StoreThemeColors.BackgroundDark;
            _titleLabel.ForeColor = StoreThemeColors.Foreground;
            _closeButton?.ApplyTheme();
            _maximizeButton?.ApplyTheme();
            _minimizeButton?.ApplyTheme();
        }
    }

    /// <summary>
    /// Button types for title bar.
    /// </summary>
    public enum TitleBarButtonType
    {
        Close,
        Maximize,
        Minimize
    }

    /// <summary>
    /// Custom-drawn title bar button with smooth hover effects.
    /// </summary>
    public class TitleBarButton : Control
    {
        private readonly TitleBarButtonType _buttonType;
        private bool _isHovered;
        private bool _isPressed;
        private bool _isMaximized;
        
        public Color HoverBackColor { get; set; } = StoreThemeColors.ListHover;
        public Color PressBackColor { get; set; } = StoreThemeColors.ControlBackground;
        public Color IconColor { get; set; } = StoreThemeColors.Foreground;
        public Color HoverIconColor { get; set; } = Color.White;

        public bool IsMaximized
        {
            get => _isMaximized;
            set
            {
                if (_isMaximized != value)
                {
                    _isMaximized = value;
                    Invalidate();
                }
            }
        }

        public TitleBarButton(TitleBarButtonType buttonType)
        {
            _buttonType = buttonType;
            
            this.Size = new Size(46, 34);
            this.SetStyle(ControlStyles.SupportsTransparentBackColor |
                          ControlStyles.AllPaintingInWmPaint | 
                          ControlStyles.UserPaint | 
                          ControlStyles.OptimizedDoubleBuffer, true);
            this.BackColor = Color.Transparent;
            this.DoubleBuffered = true;
            this.Cursor = Cursors.Default;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            // Background
            Color bgColor = Color.Transparent;
            if (_isPressed)
                bgColor = PressBackColor;
            else if (_isHovered)
                bgColor = HoverBackColor;
            
            if (bgColor != Color.Transparent)
            {
                using var brush = new SolidBrush(bgColor);
                g.FillRectangle(brush, this.ClientRectangle);
            }

            // Icon color (white on hover for close button)
            Color iconColor = IconColor;
            if (_isHovered && _buttonType == TitleBarButtonType.Close)
                iconColor = HoverIconColor;

            // Draw the icon
            using var pen = new Pen(iconColor, 1f);
            
            int centerX = this.Width / 2;
            int centerY = this.Height / 2;

            switch (_buttonType)
            {
                case TitleBarButtonType.Close:
                    // X icon
                    int offset = 5;
                    g.DrawLine(pen, centerX - offset, centerY - offset, centerX + offset, centerY + offset);
                    g.DrawLine(pen, centerX + offset, centerY - offset, centerX - offset, centerY + offset);
                    break;

                case TitleBarButtonType.Maximize:
                    int boxSize = 9;
                    if (_isMaximized)
                    {
                        // Restore icon (two overlapping rectangles)
                        int smallOffset = 2;
                        // Back rectangle (top-right)
                        g.DrawRectangle(pen, centerX - boxSize/2 + smallOffset, centerY - boxSize/2 - smallOffset, boxSize - smallOffset, boxSize - smallOffset);
                        // Front rectangle (bottom-left) with fill to cover
                        using var fillBrush = new SolidBrush(_isHovered ? HoverBackColor : this.Parent?.BackColor ?? StoreThemeColors.BackgroundDark);
                        g.FillRectangle(fillBrush, centerX - boxSize/2 - 1, centerY - boxSize/2 + smallOffset - 1, boxSize - smallOffset + 2, boxSize - smallOffset + 2);
                        g.DrawRectangle(pen, centerX - boxSize/2, centerY - boxSize/2 + smallOffset, boxSize - smallOffset, boxSize - smallOffset);
                    }
                    else
                    {
                        // Maximize icon (single rectangle)
                        g.DrawRectangle(pen, centerX - boxSize/2, centerY - boxSize/2, boxSize, boxSize);
                    }
                    break;

                case TitleBarButtonType.Minimize:
                    // Horizontal line
                    int lineWidth = 10;
                    g.DrawLine(pen, centerX - lineWidth/2, centerY, centerX + lineWidth/2, centerY);
                    break;
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            _isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isPressed = false;
            Invalidate();
        }

        public void ApplyTheme()
        {
            IconColor = StoreThemeColors.Foreground;
            Invalidate();
        }
    }

    /// <summary>
    /// Base form with custom Store-styled title bar.
    /// </summary>
    public class StoreTitleBarForm : Form
    {
        protected StoreTitleBar TitleBar { get; private set; }

        // For window resizing
        private const int RESIZE_BORDER = 6;
        private const int WM_NCHITTEST = 0x84;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

        public StoreTitleBarForm()
        {
            // Initialize custom title bar
            TitleBar = new StoreTitleBar();
            this.Controls.Add(TitleBar);

            // Apply Store theme to form
            this.BackColor = StoreThemeColors.Background;
            this.ForeColor = StoreThemeColors.Foreground;
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            if (TitleBar != null)
                TitleBar.Title = this.Text;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST && this.FormBorderStyle == FormBorderStyle.None)
            {
                // Allow resizing from edges
                Point pos = this.PointToClient(new Point(m.LParam.ToInt32()));
                
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

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
        }
    }
}
