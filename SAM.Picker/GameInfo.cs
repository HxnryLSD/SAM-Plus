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

using System.Globalization;
using System.Windows.Forms;

namespace SAM.Picker
{
    internal class GameInfo
    {
        private string _Name;

        public uint Id;
        public string Type;
        public int ImageIndex;

        public string Name
        {
            get => this._Name;
            set => this._Name = value ?? "App " + this.Id.ToString(CultureInfo.InvariantCulture);
        }

        public string ImageUrl;

        public ListViewItem Item;

        /// <summary>
        /// Gets the protection info for this game from cache.
        /// Returns null if the game hasn't been analyzed yet.
        /// </summary>
        public API.ProtectionCache.ProtectionInfo ProtectionInfo => API.ProtectionCache.GetProtectionInfo(this.Id);

        /// <summary>
        /// Indicates if this game has protected achievements/stats.
        /// Returns true if protected, false if not protected, false if unknown.
        /// </summary>
        public bool IsProtected => API.ProtectionCache.HasProtection(this.Id) ?? false;

        /// <summary>
        /// Indicates if we know the protection status of this game.
        /// </summary>
        public bool HasProtectionInfo => API.ProtectionCache.GetProtectionInfo(this.Id) != null;

        public GameInfo(uint id, string type)
        {
            this.Id = id;
            this.Type = type;
            this.Name = null;
            this.ImageIndex = 0;
            this.ImageUrl = null;
        }
    }
}
