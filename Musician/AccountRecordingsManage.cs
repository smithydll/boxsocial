/*
 * Box Social™
 * http://boxsocial.net/
 * Copyright © 2007, David Lachlan Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
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
    [AccountSubModule(AppPrimitives.Musician, "music", "recordings", true)]
    public class AccountRecordingsManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Manage Recordings";
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
        /// Initializes a new instance of the AccountRecordingsManage class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountRecordingsManage(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountRecordingsManage_Load);
            this.Show += new EventHandler(AccountRecordingsManage_Show);
        }

        void AccountRecordingsManage_Load(object sender, EventArgs e)
        {
            AddModeHandler("add", new ModuleModeHandler(AccountRecordingsManage_Add));
            AddModeHandler("edit", new ModuleModeHandler(AccountRecordingsManage_Add));
        }

        void AccountRecordingsManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_recordings");

            List<Recording> recordings = ((Musician)Owner).GetRecordings();

            foreach (Recording recording in recordings)
            {
                VariableCollection recordingVariableCollection = template.CreateChild("recordings_list");
            }
        }

        void AccountRecordingsManage_Add(object sender, ModuleModeEventArgs e)
        {
        }

        void AccountRecordingsManage_Add_Save(object sender, ModuleModeEventArgs e)
        {
        }
    }
}
