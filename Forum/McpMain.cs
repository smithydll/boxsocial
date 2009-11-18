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
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Forum
{
    [AccountSubModule(AppPrimitives.Group, "dashboard", "main", true)]
    public class McpMain : ModeratorControlPanelSubModule
    {
        public override string Title
        {
            get
            {
                return "Main";
            }
        }

        public override int Order
        {
            get
            {
                return 1;
            }
        }

        public McpMain()
        {
            this.Load += new EventHandler(AccountOverview_Load);
            this.Show += new EventHandler(AccountOverview_Show);
        }

        void AccountOverview_Load(object sender, EventArgs e)
        {
        }

        void AccountOverview_Show(object sender, EventArgs e)
        {
            template.SetTemplate("account_landing.html");

        }
    }
}
