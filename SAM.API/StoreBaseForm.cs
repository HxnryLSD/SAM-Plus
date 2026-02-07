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
using System.Windows.Forms;
using ReaLTaiizor.Forms;

namespace SAM.API
{
    /// <summary>
    /// Base form class using ReaLTaiizor Store theme.
    /// Inherit from this class for consistent Store theme styling.
    /// </summary>
    public class StoreBaseForm : Form
    {
        public StoreBaseForm()
        {
            // Apply Store theme colors
            this.BackColor = StoreThemeColors.Background;
            this.ForeColor = StoreThemeColors.Foreground;
            
            // Modern form styling
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            
            // Enable double buffering to reduce flicker
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                          ControlStyles.AllPaintingInWmPaint | 
                          ControlStyles.UserPaint, true);
            
            // Apply theme on load
            this.Load += OnFormLoad;
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            ApplyStoreTheme();
        }

        /// <summary>
        /// Applies Store theme to this form and all controls.
        /// </summary>
        protected virtual void ApplyStoreTheme()
        {
            ThemeManager.ApplyTheme(this);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
        }

        /// <summary>
        /// Creates a Store-styled button.
        /// </summary>
        protected Button CreateStoreButton(string text, EventHandler onClick = null)
        {
            var button = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = StoreThemeColors.ControlBackground,
                ForeColor = StoreThemeColors.Foreground,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand,
                Padding = new Padding(10, 5, 10, 5)
            };
            
            button.FlatAppearance.BorderColor = StoreThemeColors.ControlBorder;
            button.FlatAppearance.MouseOverBackColor = StoreThemeColors.ListHover;
            button.FlatAppearance.MouseDownBackColor = StoreThemeColors.AccentPressed;
            
            if (onClick != null)
            {
                button.Click += onClick;
            }
            
            return button;
        }

        /// <summary>
        /// Creates a Store-styled accent button (primary action).
        /// </summary>
        protected Button CreateStoreAccentButton(string text, EventHandler onClick = null)
        {
            var button = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = StoreThemeColors.Accent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9F),
                Cursor = Cursors.Hand,
                Padding = new Padding(10, 5, 10, 5)
            };
            
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = StoreThemeColors.AccentHover;
            button.FlatAppearance.MouseDownBackColor = StoreThemeColors.AccentPressed;
            
            if (onClick != null)
            {
                button.Click += onClick;
            }
            
            return button;
        }

        /// <summary>
        /// Creates a Store-styled TextBox.
        /// </summary>
        protected TextBox CreateStoreTextBox()
        {
            return new TextBox
            {
                BackColor = StoreThemeColors.ControlBackground,
                ForeColor = StoreThemeColors.Foreground,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F)
            };
        }

        /// <summary>
        /// Creates a Store-styled Panel.
        /// </summary>
        protected Panel CreateStorePanel()
        {
            return new Panel
            {
                BackColor = StoreThemeColors.BackgroundLight,
                ForeColor = StoreThemeColors.Foreground,
                BorderStyle = BorderStyle.None
            };
        }
    }
}
