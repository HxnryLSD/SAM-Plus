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
using System.Windows.Forms;

namespace SAM.API
{
    /// <summary>
    /// Modern thin scrollbar with transparent background.
    /// </summary>
    public class StoreScrollBar : Control
    {
        private int _minimum = 0;
        private int _maximum = 100;
        private int _value = 0;
        private int _largeChange = 10;
        private int _smallChange = 1;
        
        private bool _isHovered;
        private bool _isDragging;
        private int _dragStartY;
        private int _dragStartValue;
        
        private const int THUMB_MIN_HEIGHT = 30;
        private const int SCROLLBAR_WIDTH = 8;
        private const int SCROLLBAR_WIDTH_HOVER = 10;

        public event EventHandler ValueChanged;
        public event ScrollEventHandler Scroll;

        public int Minimum
        {
            get => _minimum;
            set
            {
                _minimum = value;
                Invalidate();
            }
        }

        public int Maximum
        {
            get => _maximum;
            set
            {
                _maximum = value;
                Invalidate();
            }
        }

        public int Value
        {
            get => _value;
            set
            {
                int newValue = Math.Max(_minimum, Math.Min(value, _maximum - _largeChange + 1));
                if (_value != newValue)
                {
                    _value = newValue;
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        public int LargeChange
        {
            get => _largeChange;
            set
            {
                _largeChange = Math.Max(1, value);
                Invalidate();
            }
        }

        public int SmallChange
        {
            get => _smallChange;
            set => _smallChange = Math.Max(1, value);
        }

        public StoreScrollBar()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer, true);
            this.BackColor = StoreThemeColors.ListBackground;
            this.Width = SCROLLBAR_WIDTH_HOVER;
            this.Cursor = Cursors.Default;
        }

        private int GetThumbHeight()
        {
            if (_maximum <= _minimum) return this.Height;
            
            int range = _maximum - _minimum;
            int trackHeight = this.Height;
            int thumbHeight = (int)((float)_largeChange / range * trackHeight);
            return Math.Max(THUMB_MIN_HEIGHT, Math.Min(thumbHeight, trackHeight));
        }

        private int GetThumbTop()
        {
            if (_maximum <= _minimum + _largeChange) return 0;
            
            int trackHeight = this.Height - GetThumbHeight();
            int range = _maximum - _minimum - _largeChange + 1;
            return (int)((float)(_value - _minimum) / range * trackHeight);
        }

        private Rectangle GetThumbRectangle()
        {
            int thumbHeight = GetThumbHeight();
            int thumbTop = GetThumbTop();
            int width = _isHovered || _isDragging ? SCROLLBAR_WIDTH_HOVER : SCROLLBAR_WIDTH;
            int left = this.Width - width;
            return new Rectangle(left, thumbTop, width, thumbHeight);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Clear with list background color
            g.Clear(StoreThemeColors.ListBackground);
            
            // Draw thumb only if there's something to scroll
            if (_maximum > _minimum + _largeChange)
            {
                var thumbRect = GetThumbRectangle();
                
                // Thumb color based on state
                Color thumbColor;
                if (_isDragging)
                    thumbColor = StoreThemeColors.Accent;
                else if (_isHovered)
                    thumbColor = Color.FromArgb(180, StoreThemeColors.ForegroundDim);
                else
                    thumbColor = Color.FromArgb(100, StoreThemeColors.ForegroundDim);

                // Draw rounded thumb
                using var brush = new SolidBrush(thumbColor);
                int radius = Math.Min(thumbRect.Width, thumbRect.Height) / 2;
                
                using var path = CreateRoundedRectangle(thumbRect, radius);
                g.FillPath(brush, path);
            }
        }

        private GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            
            return path;
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
            if (!_isDragging)
            {
                _isHovered = false;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            
            if (e.Button == MouseButtons.Left)
            {
                var thumbRect = GetThumbRectangle();
                
                if (thumbRect.Contains(e.Location))
                {
                    // Start dragging thumb
                    _isDragging = true;
                    _dragStartY = e.Y;
                    _dragStartValue = _value;
                    Capture = true;
                }
                else
                {
                    // Click above/below thumb - page up/down
                    if (e.Y < thumbRect.Top)
                        Value -= _largeChange;
                    else if (e.Y > thumbRect.Bottom)
                        Value += _largeChange;
                    
                    Scroll?.Invoke(this, new ScrollEventArgs(ScrollEventType.LargeIncrement, _value));
                }
                
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            if (_isDragging)
            {
                int deltaY = e.Y - _dragStartY;
                int trackHeight = this.Height - GetThumbHeight();
                
                if (trackHeight > 0)
                {
                    int range = _maximum - _minimum - _largeChange + 1;
                    int deltaValue = (int)((float)deltaY / trackHeight * range);
                    Value = _dragStartValue + deltaValue;
                    Scroll?.Invoke(this, new ScrollEventArgs(ScrollEventType.ThumbTrack, _value));
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            
            if (_isDragging)
            {
                _isDragging = false;
                Capture = false;
                
                if (!ClientRectangle.Contains(e.Location))
                    _isHovered = false;
                    
                Scroll?.Invoke(this, new ScrollEventArgs(ScrollEventType.EndScroll, _value));
                Invalidate();
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            
            int delta = e.Delta > 0 ? -_smallChange * 3 : _smallChange * 3;
            Value += delta;
            Scroll?.Invoke(this, new ScrollEventArgs(ScrollEventType.SmallIncrement, _value));
        }

        public void ApplyTheme()
        {
            Invalidate();
        }
    }

    /// <summary>
    /// ListView with hidden native scrollbar, designed to work with StoreScrollBar.
    /// </summary>
    public class StoreListView : ListView
    {
        private const int WS_VSCROLL = 0x00200000;
        private const int GWL_STYLE = -16;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private StoreScrollBar _customScrollBar;

        public StoreScrollBar CustomScrollBar
        {
            get => _customScrollBar;
            set
            {
                _customScrollBar = value;
                if (_customScrollBar != null)
                {
                    _customScrollBar.Scroll += OnCustomScroll;
                    UpdateScrollBar();
                }
            }
        }

        public StoreListView()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.OwnerDraw = false;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            HideNativeScrollBar();
        }

        private void HideNativeScrollBar()
        {
            if (this.IsHandleCreated)
            {
                int style = GetWindowLong(this.Handle, GWL_STYLE);
                style &= ~WS_VSCROLL;
                SetWindowLong(this.Handle, GWL_STYLE, style);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateScrollBar();
        }

        protected override void OnVirtualItemsSelectionRangeChanged(ListViewVirtualItemsSelectionRangeChangedEventArgs e)
        {
            base.OnVirtualItemsSelectionRangeChanged(e);
            UpdateScrollBar();
        }

        public void UpdateScrollBar()
        {
            if (_customScrollBar == null) return;

            int itemCount = this.VirtualMode ? this.VirtualListSize : this.Items.Count;
            int visibleItems = this.ClientSize.Height / (this.TileSize.Height > 0 ? this.TileSize.Height : 
                              (this.View == View.LargeIcon ? 100 : 20));
            
            if (visibleItems <= 0) visibleItems = 1;

            _customScrollBar.Minimum = 0;
            _customScrollBar.Maximum = Math.Max(0, itemCount);
            _customScrollBar.LargeChange = visibleItems;
            _customScrollBar.SmallChange = 1;

            // Get current scroll position
            if (itemCount > 0 && this.TopItem != null)
            {
                _customScrollBar.Value = this.TopItem.Index;
            }
        }

        private void OnCustomScroll(object sender, ScrollEventArgs e)
        {
            if (this.VirtualListSize > 0 || this.Items.Count > 0)
            {
                int index = Math.Max(0, Math.Min(e.NewValue, 
                    (this.VirtualMode ? this.VirtualListSize : this.Items.Count) - 1));
                
                this.EnsureVisible(index);
                
                // Try to make this item the top item
                if (this.Items.Count > index || this.VirtualListSize > index)
                {
                    try
                    {
                        var item = this.Items[index];
                        if (item != null)
                        {
                            this.TopItem = item;
                        }
                    }
                    catch { }
                }
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            
            // Sync custom scrollbar after native scroll
            if (_customScrollBar != null)
            {
                this.BeginInvoke((Action)UpdateScrollBar);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            // Sync after keyboard navigation
            if (_customScrollBar != null && 
                (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || 
                 e.KeyCode == Keys.PageUp || e.KeyCode == Keys.PageDown ||
                 e.KeyCode == Keys.Home || e.KeyCode == Keys.End))
            {
                this.BeginInvoke((Action)UpdateScrollBar);
            }
        }
    }
}
