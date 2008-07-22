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

namespace BoxSocial.Applications.Forum
{
    [AccountSubModule("forum", "forum", true)]
    public class AccountForumManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Manage Forum";
            }
        }

        public override int Order
        {
            get
            {
                return 1;
            }
        }

        public AccountForumManage()
        {
            this.Load += new EventHandler(AccountForumManage_Load);
            this.Show += new EventHandler(AccountForumManage_Show);
        }

        void AccountForumManage_Load(object sender, EventArgs e)
        {
            AddModeHandler("new", new ModuleModeHandler(AccountForumManage_New));
            AddModeHandler("edit", new ModuleModeHandler(AccountForumManage_New));
            AddSaveHandler("new", new EventHandler(AccountForumManage_New_Save));
            AddSaveHandler("edit", new EventHandler(AccountForumManage_Edit_Save));
        }

        void AccountForumManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_forum_manage");

            long forumId = Functions.RequestLong("id", 0);
        }

        void AccountForumManage_New(object sender, ModuleModeEventArgs e)
        {

            switch (e.Mode)
            {
                case "new":
                    break;
                case "edit":
                    break;
            }
        }

        void AccountForumManage_New_Save(object sender, EventArgs e)
        {
        }

        void AccountForumManage_Edit_Save(object sender, EventArgs e)
        {
        }
    }
}
