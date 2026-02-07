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
using Microsoft.Win32;

namespace SAM.API
{
    /// <summary>
    /// Theme colors for the application.
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

        public static Theme Light => new()
        {
            BackColor = SystemColors.Control,
            ForeColor = SystemColors.ControlText,
            ControlBackColor = SystemColors.Window,
            ControlForeColor = SystemColors.WindowText,
            ListBackColor = SystemColors.Window,
            ListForeColor = SystemColors.WindowText,
            AccentColor = Color.FromArgb(0, 120, 215),
            BorderColor = SystemColors.ControlDark
        };

        public static Theme Dark => new()
        {
            BackColor = Color.FromArgb(32, 32, 32),
            ForeColor = Color.FromArgb(240, 240, 240),
            ControlBackColor = Color.FromArgb(45, 45, 45),
            ControlForeColor = Color.FromArgb(240, 240, 240),
            ListBackColor = Color.FromArgb(25, 25, 25),
            ListForeColor = Color.FromArgb(240, 240, 240),
            AccentColor = Color.FromArgb(0, 120, 215),
            BorderColor = Color.FromArgb(60, 60, 60)
        };
    }

    /// <summary>
    /// Manages application theming and dark mode support.
    /// </summary>
    public static class ThemeManager
    {
        private static bool _isDarkMode;
        private static Theme _currentTheme;

        public static event Action ThemeChanged;

        public static bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    _currentTheme = value ? Theme.Dark : Theme.Light;
                    ThemeChanged?.Invoke();
                    Logger.Info($"Theme changed to: {(value ? "Dark" : "Light")}");
                }
            }
        }

        public static Theme Current => _currentTheme ?? Theme.Light;

        /// <summary>
        /// Initializes the theme based on Windows settings.
        /// </summary>
        public static void Initialize()
        {
            _isDarkMode = IsWindowsDarkMode();
            _currentTheme = _isDarkMode ? Theme.Dark : Theme.Light;
            Logger.Info($"Theme initialized: {(_isDarkMode ? "Dark" : "Light")} mode");
        }

        /// <summary>
        /// Detects if Windows is using dark mode.
        /// </summary>
        private static bool IsWindowsDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int i && i == 0;
            }
            catch
            {
                return false;
            }
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
                    btn.BackColor = theme.ControlBackColor;
                    btn.ForeColor = theme.ControlForeColor;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderColor = theme.BorderColor;
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

        /// <summary>
        /// Toggles between dark and light mode.
        /// </summary>
        public static void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
        }
    }
}
