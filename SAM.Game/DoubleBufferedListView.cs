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

namespace SAM.Game
{
	internal class DoubleBufferedListView : ListView
	{
		public event ScrollEventHandler Scroll;

		public DoubleBufferedListView()
		{
			base.DoubleBuffered = true;
		}

		protected virtual void OnScroll(ScrollEventArgs e)
		{
			this.Scroll?.Invoke(this, e);
		}

		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);

			// WM_VSCROLL or WM_MOUSEWHEEL
			if (m.Msg == 0x0115 || m.Msg == 0x020A)
			{
				this.OnScroll(new ScrollEventArgs(ScrollEventType.EndScroll, GetScrollPos()));
			}
		}

		[DllImport("user32.dll")]
		private static extern int GetScrollPos(IntPtr hWnd, int nBar);

		private int GetScrollPos()
		{
			return GetScrollPos(this.Handle, 1); // SB_VERT = 1
		}
	}
}
