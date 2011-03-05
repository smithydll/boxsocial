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

namespace BoxSocial.Applications.Profile
{
    [AccountSubModule("profile", "status")]
    public class AccountStatus : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return null;
            }
        }

        public override int Order
        {
            get
            {
                return -1;
            }
        }

        public AccountStatus(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountStatus_Load);
            this.Show += new EventHandler(AccountStatus_Show);
        }

        void AccountStatus_Load(object sender, EventArgs e)
        {
        }

        void AccountStatus_Show(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            string message = core.Http.Form["message"];

            StatusFeed.SaveMessage(core, message);

            core.Ajax.SendRawText("Success", message);
        }
    }
}
