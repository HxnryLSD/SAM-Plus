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
using ReaLTaiizor.Colors;

namespace SAM.API
{
    /// <summary>
    /// Store Theme color palette based on ReaLTaiizor Store theme.
    /// </summary>
    public static class StoreThemeColors
    {
        // Primary colors
        public static Color Background => Color.FromArgb(28, 28, 28);          // Main background
        public static Color BackgroundLight => Color.FromArgb(38, 38, 38);     // Secondary background
        public static Color BackgroundDark => Color.FromArgb(18, 18, 18);      // Darker areas
        
        // Text colors
        public static Color Foreground => Color.FromArgb(243, 243, 243);       // Primary text
        public static Color ForegroundDim => Color.FromArgb(180, 180, 180);    // Secondary text
        public static Color ForegroundDisabled => Color.FromArgb(100, 100, 100); // Disabled text
        
        // Accent colors (Steam-like blue)
        public static Color Accent => Color.FromArgb(66, 133, 244);            // Primary accent
        public static Color AccentHover => Color.FromArgb(86, 153, 255);       // Hover state
        public static Color AccentPressed => Color.FromArgb(46, 113, 224);     // Pressed state
        
        // Control colors
        public static Color ControlBackground => Color.FromArgb(45, 45, 45);   // Button, TextBox background
        public static Color ControlBorder => Color.FromArgb(70, 70, 70);       // Control borders
        public static Color ControlBorderHover => Color.FromArgb(90, 90, 90);  // Border hover
        
        // Status colors
        public static Color Success => Color.FromArgb(76, 175, 80);            // Green for unlocked
        public static Color Warning => Color.FromArgb(255, 193, 7);            // Yellow for warning
        public static Color Error => Color.FromArgb(244, 67, 54);              // Red for errors
        
        // List/Grid colors
        public static Color ListBackground => Color.FromArgb(33, 33, 33);
        public static Color ListAlternate => Color.FromArgb(40, 40, 40);
        public static Color ListSelected => Color.FromArgb(66, 133, 244, 80);  // Semi-transparent accent
        public static Color ListHover => Color.FromArgb(55, 55, 55);
    }

    /// <summary>
    /// Theme colors for the application (Store Dark theme only).
    /// </summary>
    public class Theme
    {
        public Color BackColor { get; set; }
        public Color ForeColor { get; set; }
        public Color ControlBackColor { get; set; }
        public Color ControlForeColor { get; set; }
        public Color ListBackColor { get; set; }
        public Color ListForeColor { get; set; }
        public Color AccentColor { get; set; }
        public Color BorderColor { get; set; }

        /// <summary>
        /// Store Theme - Modern dark theme based on ReaLTaiizor Store design.
        /// </summary>
        public static Theme Dark => new()
        {
            BackColor = StoreThemeColors.Background,
            ForeColor = StoreThemeColors.Foreground,
            ControlBackColor = StoreThemeColors.ControlBackground,
            ControlForeColor = StoreThemeColors.Foreground,
            ListBackColor = StoreThemeColors.ListBackground,
            ListForeColor = StoreThemeColors.Foreground,
            AccentColor = StoreThemeColors.Accent,
            BorderColor = StoreThemeColors.ControlBorder
        };
    }

    /// <summary>
    /// Manages application theming - fixed to Store dark theme.
    /// </summary>
    public static class ThemeManager
    {
        private static readonly Theme _currentTheme = Theme.Dark;

        public static bool IsDarkMode => true;

        public static Theme Current => _currentTheme;

        /// <summary>
        /// Initializes the theme (Store dark mode).
        /// </summary>
        public static void Initialize()
        {
            Logger.Info("Theme initialized: Store Dark mode (ReaLTaiizor)");
        }

        /// <summary>
        /// Applies the current theme to a form and all its controls.
        /// </summary>
        public static void ApplyTheme(Form form)
        {
            if (form == null) return;

            var theme = Current;
            form.BackColor = theme.BackColor;
            form.ForeColor = theme.ForeColor;

            ApplyThemeToControls(form.Controls, theme);
        }

        private static void ApplyThemeToControls(Control.ControlCollection controls, Theme theme)
        {
            foreach (Control control in controls)
            {
                ApplyThemeToControl(control, theme);
                
                if (control.HasChildren)
                {
                    ApplyThemeToControls(control.Controls, theme);
                }
            }
        }

        private static void ApplyThemeToControl(Control control, Theme theme)
        {
            switch (control)
            {
                case ListView lv:
                    lv.BackColor = theme.ListBackColor;
                    lv.ForeColor = theme.ListForeColor;
                    break;

                case DataGridView dgv:
                    dgv.BackgroundColor = theme.ControlBackColor;
                    dgv.DefaultCellStyle.BackColor = theme.ControlBackColor;
                    dgv.DefaultCellStyle.ForeColor = theme.ControlForeColor;
                    dgv.ColumnHeadersDefaultCellStyle.BackColor = theme.BackColor;
                    dgv.ColumnHeadersDefaultCellStyle.ForeColor = theme.ForeColor;
                    dgv.EnableHeadersVisualStyles = false;
                    dgv.GridColor = theme.BorderColor;
                    break;

                case TextBox tb:
                    tb.BackColor = theme.ControlBackColor;
                    tb.ForeColor = theme.ControlForeColor;
                    break;

                case ComboBox cb:
                    cb.BackColor = theme.ControlBackColor;
                    cb.ForeColor = theme.ControlForeColor;
                    break;

                case Button btn:
                    btn.BackColor = StoreThemeColors.ControlBackground;
                    btn.ForeColor = theme.ControlForeColor;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderColor = StoreThemeColors.ControlBorder;
                    btn.FlatAppearance.MouseOverBackColor = StoreThemeColors.ListHover;
                    btn.FlatAppearance.MouseDownBackColor = StoreThemeColors.AccentPressed;
                    break;

                case CheckBox chk:
                    chk.BackColor = theme.BackColor;
                    chk.ForeColor = theme.ForeColor;
                    break;

                case TabControl tc:
                    tc.BackColor = theme.BackColor;
                    foreach (TabPage tp in tc.TabPages)
                    {
                        tp.BackColor = theme.BackColor;
                        tp.ForeColor = theme.ForeColor;
                        ApplyThemeToControls(tp.Controls, theme);
                    }
                    break;

                case Panel pnl:
                    pnl.BackColor = theme.BackColor;
                    pnl.ForeColor = theme.ForeColor;
                    break;

                case StatusStrip ss:
                    ss.BackColor = theme.BackColor;
                    ss.ForeColor = theme.ForeColor;
                    foreach (ToolStripItem item in ss.Items)
                    {
                        item.BackColor = theme.BackColor;
                        item.ForeColor = theme.ForeColor;
                    }
                    break;

                case MenuStrip ms:
                    ms.BackColor = theme.BackColor;
                    ms.ForeColor = theme.ForeColor;
                    break;

                case ToolStrip ts:
                    ts.BackColor = theme.BackColor;
                    ts.ForeColor = theme.ForeColor;
                    ts.RenderMode = ToolStripRenderMode.System;
                    foreach (ToolStripItem item in ts.Items)
                    {
                        item.BackColor = theme.BackColor;
                        item.ForeColor = theme.ForeColor;
                    }
                    break;

                default:
                    control.BackColor = theme.BackColor;
                    control.ForeColor = theme.ForeColor;
                    break;
            }
        }
    }
}
