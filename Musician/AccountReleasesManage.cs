/*
 * Box Social™
 * http://boxsocial.net/
  * Copyright © 2007, David Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 2 of
 * the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Musician
{
    [AccountSubModule(AppPrimitives.Musician, "music", "releases", true)]
    public class AccountReleasesManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Manage Releases";
            }
        }

        public override int Order
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountReleasesManage class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountReleasesManage(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountReleasesManage_Load);
            this.Show += new EventHandler(AccountReleasesManage_Show);
        }

        void AccountReleasesManage_Load(object sender, EventArgs e)
        {
            AddModeHandler("add", new ModuleModeHandler(AccountReleasesManage_Add));
            AddModeHandler("edit", new ModuleModeHandler(AccountReleasesManage_Add));
        }

        void AccountReleasesManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_releases");
        }

        void AccountReleasesManage_Add(object sender, ModuleModeEventArgs e)
        {
        }

        void AccountReleasesManage_Add_Save(object sender, ModuleModeEventArgs e)
        {
        }
    }
}
