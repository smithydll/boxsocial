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

namespace BoxSocial.Groups
{
    /// <summary>
    /// 
    /// </summary>
    [AccountSubModule("groups", "groups", true)]
    public class AccountGroupsManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Manage Groups";
            }
        }

        public override int Order
        {
            get
            {
                return 1;
            }
        }

        public AccountGroupsManage()
        {
            this.Load += new EventHandler(AccountGroupsManage_Load);
            this.Show += new EventHandler(AccountGroupsManage_Show);
        }

        void AccountGroupsManage_Load(object sender, EventArgs e)
        {
        }

        void AccountGroupsManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_group_manage");

            template.Parse("U_CREATE_GROUP", Linker.AppendSid("/groups/create"));

            DataTable groupsTable = db.Query(string.Format("SELECT {1} FROM group_operators go INNER JOIN group_keys gk ON go.group_id = gk.group_id INNER JOIN group_info gi ON gk.group_id = gi.group_id WHERE go.user_id = {0}",
                loggedInMember.UserId, UserGroup.GROUP_INFO_FIELDS));

            for (int i = 0; i < groupsTable.Rows.Count; i++)
            {
                VariableCollection groupVariableCollection = template.CreateChild("group_list");

                UserGroup thisGroup = new UserGroup(core, groupsTable.Rows[i]);

                groupVariableCollection.Parse("GROUP_DISPLAY_NAME", thisGroup.DisplayName);
                groupVariableCollection.Parse("MEMBERS", thisGroup.Members.ToString());

                groupVariableCollection.Parse("U_VIEW", thisGroup.Uri);
                groupVariableCollection.Parse("U_MEMBERLIST", thisGroup.MemberlistUri);
                groupVariableCollection.Parse("U_EDIT", thisGroup.EditUri);
                groupVariableCollection.Parse("U_DELETE", thisGroup.DeleteUri);

                switch (thisGroup.GroupType)
                {
                    case "OPEN":
                        groupVariableCollection.Parse("GROUP_TYPE", "Open");
                        break;
                    case "CLOSED":
                        groupVariableCollection.Parse("GROUP_TYPE", "Closed");
                        break;
                    case "PRIVATE":
                        groupVariableCollection.Parse("GROUP_TYPE", "Private");
                        break;
                }
            }
        }
    }
}
