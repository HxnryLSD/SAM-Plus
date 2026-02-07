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
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SAM.Picker
{
    internal class MyListView : ListView
    {
        public event ScrollEventHandler Scroll;

        /// <summary>
        /// When true, the native vertical scrollbar will be hidden.
        /// </summary>
        public bool HideVerticalScrollBar { get; set; } = false;

        /// <summary>
        /// Enable smooth scrolling animation.
        /// Note: When used with GamePicker, smooth scrolling is handled by GamePicker's message filter.
        /// </summary>
        public bool SmoothScrolling { get; set; } = false;

        private const int WS_VSCROLL = 0x00200000;
        private const int WS_HSCROLL = 0x00100000;
        private const int GWL_STYLE = -16;
        private const int SB_VERT = 1;
        private const int SB_BOTH = 3;
        private const int WM_NCCALCSIZE = 0x0083;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int LVM_SCROLL = 0x1014;

        // Smooth scrolling state
        private readonly Timer _smoothScrollTimer;
        private float _scrollVelocity = 0;
        private float _scrollAccumulator = 0;
        private const float SCROLL_FRICTION = 0.85f;
        private const float SCROLL_MIN_VELOCITY = 0.5f;
        private const int SCROLL_TIMER_INTERVAL = 16; // ~60fps

        [DllImport("user32.dll")]
        private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NCCALCSIZE_PARAMS
        {
            public RECT rgrc0, rgrc1, rgrc2;
            public IntPtr lppos;
        }

        public MyListView()
        {
            base.DoubleBuffered = true;
            
            // Initialize smooth scrolling timer
            _smoothScrollTimer = new Timer { Interval = SCROLL_TIMER_INTERVAL };
            _smoothScrollTimer.Tick += OnSmoothScrollTick;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _smoothScrollTimer?.Stop();
                _smoothScrollTimer?.Dispose();
            }
            base.Dispose(disposing);
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
                
                // Use LVM_SCROLL for pixel-based scrolling (dx=0, dy=pixelsToScroll)
                SendMessage(this.Handle, LVM_SCROLL, IntPtr.Zero, (IntPtr)pixelsToScroll);
                
                this.OnScroll(new ScrollEventArgs(ScrollEventType.ThumbTrack, Win32.GetScrollPos(this.Handle, SB_VERT)));
            }

            // Apply friction
            _scrollVelocity *= SCROLL_FRICTION;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (SmoothScrolling && this.VirtualListSize > 0)
            {
                // Calculate scroll velocity based on wheel delta
                // Negative because wheel up = scroll up (negative pixels)
                float delta = -e.Delta * 0.5f;
                
                // Add to velocity for momentum effect
                _scrollVelocity += delta;
                
                // Start timer if not running
                if (!_smoothScrollTimer.Enabled)
                {
                    _smoothScrollTimer.Start();
                }
                
                // Mark as handled
                ((HandledMouseEventArgs)e).Handled = true;
            }
            else
            {
                base.OnMouseWheel(e);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (HideVerticalScrollBar)
            {
                ShowScrollBar(this.Handle, SB_BOTH, false);
                HideScrollBar();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (HideVerticalScrollBar && this.IsHandleCreated)
            {
                ShowScrollBar(this.Handle, SB_BOTH, false);
            }
        }

        protected virtual void OnScroll(ScrollEventArgs e)
        {
            this.Scroll?.Invoke(this, e);
        }

        protected override void WndProc(ref Message m)
        {
            // Intercept WM_NCCALCSIZE to remove scrollbar space entirely
            if (m.Msg == WM_NCCALCSIZE && HideVerticalScrollBar && m.WParam != IntPtr.Zero)
            {
                // Let the base handle it first
                base.WndProc(ref m);
                
                // Then hide the scrollbar immediately
                ShowScrollBar(this.Handle, SB_BOTH, false);
                return;
            }

            base.WndProc(ref m);

            switch (m.Msg)
            {
                case 0x0100: // WM_KEYDOWN
                {
                    ScrollEventType type;
                    if (TranslateKeyScrollEvent((Keys)m.WParam.ToInt32(), out type) == true)
                    {
                        this.OnScroll(new(type, Win32.GetScrollPos(this.Handle, 1 /*SB_VERT*/)));
                    }
                    break;
                }

                case 0x0115: // WM_VSCROLL
                case 0x020A: // WM_MOUSEWHEEL
                {
                    this.OnScroll(new(ScrollEventType.EndScroll, Win32.GetScrollPos(this.Handle, 1 /*SB_VERT*/)));
                    // Hide scrollbar after scroll events
                    if (HideVerticalScrollBar)
                    {
                        ShowScrollBar(this.Handle, SB_BOTH, false);
                    }
                    break;
                }

                case 0x000F: // WM_PAINT
                case 0x0014: // WM_ERASEBKGND
                case 0x0085: // WM_NCPAINT
                case 0x0047: // WM_WINDOWPOSCHANGED
                case 0x0005: // WM_SIZE
                case 0x0046: // WM_WINDOWPOSCHANGING
                case 0x100C: // LVM_SETITEMCOUNT
                {
                    // Hide scrollbar after any repaint/resize event
                    if (HideVerticalScrollBar && this.IsHandleCreated)
                    {
                        ShowScrollBar(this.Handle, SB_BOTH, false);
                        // Also hide with a slight delay as a failsafe
                        this.BeginInvoke((Action)(() => ShowScrollBar(this.Handle, SB_BOTH, false)));
                    }
                    break;
                }
            }
        }

        private void HideScrollBar()
        {
            ShowScrollBar(this.Handle, SB_BOTH, false);
            
            // Also remove from window style
            int style = GetWindowLong(this.Handle, GWL_STYLE);
            if ((style & (WS_VSCROLL | WS_HSCROLL)) != 0)
            {
                style &= ~WS_VSCROLL;
                style &= ~WS_HSCROLL;
                SetWindowLong(this.Handle, GWL_STYLE, style);
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                if (HideVerticalScrollBar)
                {
                    // Remove scrollbars from initial window style
                    cp.Style &= ~WS_VSCROLL;
                    cp.Style &= ~WS_HSCROLL;
                }
                return cp;
            }
        }

        private static bool TranslateKeyScrollEvent(Keys keys, out ScrollEventType type)
        {
            switch (keys)
            {
                case Keys.Down:
                {
                    type = ScrollEventType.SmallIncrement;
                    return true;
                }

                case Keys.Up:
                {
                    type = ScrollEventType.SmallDecrement;
                    return true;
                }

                case Keys.PageDown:
                {
                    type = ScrollEventType.LargeIncrement;
                    return true;
                }

                case Keys.PageUp:
                {
                    type = ScrollEventType.SmallDecrement;
                    return true;
                }

                case Keys.Home:
                {
                    type = ScrollEventType.First;
                    return true;
                }

                case Keys.End:
                {
                    type = ScrollEventType.Last;
                    return true;
                }
            }

            type = default;
            return false;
        }

        private static class Win32
        {
            [DllImport("user32.dll", SetLastError = true)]
            public static extern int GetScrollPos(IntPtr hWnd, int nBar);
        }
    }
}
