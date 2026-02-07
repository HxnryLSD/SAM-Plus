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

using System.Drawing;
using System.Windows.Forms;

namespace SAM.Game
{
    /// <summary>
    /// Custom ToolStrip renderer for Store theme styling.
    /// </summary>
    internal class StoreToolStripRenderer : ToolStripProfessionalRenderer
    {
        public StoreToolStripRenderer() : base(new StoreColorTable())
        {
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var brush = new SolidBrush(API.StoreThemeColors.BackgroundLight);
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using var pen = new Pen(API.StoreThemeColors.ControlBorder);
            e.Graphics.DrawLine(pen, 0, e.AffectedBounds.Height - 1, 
                e.AffectedBounds.Width, e.AffectedBounds.Height - 1);
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var bounds = new Rectangle(Point.Empty, e.Item.Size);
            
            if (e.Item.Selected || e.Item.Pressed)
            {
                using var brush = new SolidBrush(
                    e.Item.Pressed ? API.StoreThemeColors.AccentPressed : API.StoreThemeColors.ListHover);
                e.Graphics.FillRectangle(brush, bounds);
            }
            else if (e.Item is ToolStripButton btn && btn.Checked)
            {
                using var brush = new SolidBrush(API.StoreThemeColors.Accent);
                e.Graphics.FillRectangle(brush, bounds);
            }
        }

        protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var bounds = new Rectangle(Point.Empty, e.Item.Size);
            
            if (e.Item.Selected || e.Item.Pressed)
            {
                using var brush = new SolidBrush(
                    e.Item.Pressed ? API.StoreThemeColors.AccentPressed : API.StoreThemeColors.ListHover);
                e.Graphics.FillRectangle(brush, bounds);
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var bounds = new Rectangle(Point.Empty, e.Item.Size);
            
            if (e.Item.Selected)
            {
                using var brush = new SolidBrush(API.StoreThemeColors.ListHover);
                e.Graphics.FillRectangle(brush, bounds);
            }
            else
            {
                using var brush = new SolidBrush(API.StoreThemeColors.BackgroundLight);
                e.Graphics.FillRectangle(brush, bounds);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using var pen = new Pen(API.StoreThemeColors.ControlBorder);
            int y = e.Item.Height / 2;
            e.Graphics.DrawLine(pen, 4, y, e.Item.Width - 4, y);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = API.StoreThemeColors.Foreground;
            base.OnRenderItemText(e);
        }
    }

    /// <summary>
    /// Color table for Store theme ToolStrip.
    /// </summary>
    internal class StoreColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => API.StoreThemeColors.BackgroundLight;
        public override Color MenuItemSelected => API.StoreThemeColors.ListHover;
        public override Color MenuItemSelectedGradientBegin => API.StoreThemeColors.ListHover;
        public override Color MenuItemSelectedGradientEnd => API.StoreThemeColors.ListHover;
        public override Color MenuItemPressedGradientBegin => API.StoreThemeColors.AccentPressed;
        public override Color MenuItemPressedGradientEnd => API.StoreThemeColors.AccentPressed;
        public override Color MenuBorder => API.StoreThemeColors.ControlBorder;
        public override Color MenuItemBorder => API.StoreThemeColors.ControlBorder;
        public override Color ImageMarginGradientBegin => API.StoreThemeColors.BackgroundLight;
        public override Color ImageMarginGradientMiddle => API.StoreThemeColors.BackgroundLight;
        public override Color ImageMarginGradientEnd => API.StoreThemeColors.BackgroundLight;
        public override Color SeparatorDark => API.StoreThemeColors.ControlBorder;
        public override Color SeparatorLight => API.StoreThemeColors.BackgroundLight;
        public override Color CheckBackground => API.StoreThemeColors.Accent;
        public override Color CheckSelectedBackground => API.StoreThemeColors.AccentHover;
        public override Color CheckPressedBackground => API.StoreThemeColors.AccentPressed;
        public override Color ButtonSelectedHighlight => API.StoreThemeColors.ListHover;
        public override Color ButtonSelectedHighlightBorder => API.StoreThemeColors.ControlBorder;
        public override Color ButtonPressedHighlight => API.StoreThemeColors.AccentPressed;
        public override Color ButtonPressedHighlightBorder => API.StoreThemeColors.ControlBorder;
        public override Color ButtonCheckedHighlight => API.StoreThemeColors.Accent;
        public override Color ButtonCheckedHighlightBorder => API.StoreThemeColors.Accent;
        public override Color ButtonSelectedBorder => API.StoreThemeColors.ControlBorderHover;
        public override Color ButtonSelectedGradientBegin => API.StoreThemeColors.ListHover;
        public override Color ButtonSelectedGradientMiddle => API.StoreThemeColors.ListHover;
        public override Color ButtonSelectedGradientEnd => API.StoreThemeColors.ListHover;
        public override Color ButtonPressedGradientBegin => API.StoreThemeColors.AccentPressed;
        public override Color ButtonPressedGradientMiddle => API.StoreThemeColors.AccentPressed;
        public override Color ButtonPressedGradientEnd => API.StoreThemeColors.AccentPressed;
        public override Color ButtonCheckedGradientBegin => API.StoreThemeColors.Accent;
        public override Color ButtonCheckedGradientMiddle => API.StoreThemeColors.Accent;
        public override Color ButtonCheckedGradientEnd => API.StoreThemeColors.Accent;
        public override Color GripDark => API.StoreThemeColors.ControlBorder;
        public override Color GripLight => API.StoreThemeColors.BackgroundLight;
        public override Color OverflowButtonGradientBegin => API.StoreThemeColors.BackgroundLight;
        public override Color OverflowButtonGradientMiddle => API.StoreThemeColors.BackgroundLight;
        public override Color OverflowButtonGradientEnd => API.StoreThemeColors.BackgroundLight;
        public override Color ToolStripGradientBegin => API.StoreThemeColors.BackgroundLight;
        public override Color ToolStripGradientMiddle => API.StoreThemeColors.BackgroundLight;
        public override Color ToolStripGradientEnd => API.StoreThemeColors.BackgroundLight;
        public override Color ToolStripBorder => API.StoreThemeColors.ControlBorder;
        public override Color ToolStripContentPanelGradientBegin => API.StoreThemeColors.Background;
        public override Color ToolStripContentPanelGradientEnd => API.StoreThemeColors.Background;
        public override Color ToolStripPanelGradientBegin => API.StoreThemeColors.Background;
        public override Color ToolStripPanelGradientEnd => API.StoreThemeColors.Background;
        public override Color StatusStripGradientBegin => API.StoreThemeColors.BackgroundDark;
        public override Color StatusStripGradientEnd => API.StoreThemeColors.BackgroundDark;
    }
}
