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
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Profile
{
    [AccountSubModule("friends", "family")]
    public class AccountFamilyManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Manage Family";
            }
        }

        public override int Order
        {
            get
            {
                return 3;
            }
        }

        public AccountFamilyManage()
        {
            this.Load += new EventHandler(AccountFamilyManage_Load);
            this.Show += new EventHandler(AccountFamilyManage_Show);
        }

        void AccountFamilyManage_Load(object sender, EventArgs e)
        {
            AddModeHandler("add", AccountFamilyManage_Add);
            AddModeHandler("delete", AccountFamilyManage_Delete);
        }

        void AccountFamilyManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_family_manage");

            DataTable familyTable = db.Query(string.Format("SELECT ur.relation_order, uk.user_name, uk.user_id FROM user_relations ur INNER JOIN user_keys uk ON uk.user_id = ur.relation_you WHERE ur.relation_type = 'FAMILY' AND ur.relation_me = {0} ORDER BY uk.user_name ASC",
                LoggedInMember.UserId));

            for (int i = 0; i < familyTable.Rows.Count; i++)
            {
                VariableCollection familyVariableCollection = template.CreateChild("family_list");

                byte order = (byte)familyTable.Rows[i]["relation_order"];

                familyVariableCollection.Parse("NAME", (string)familyTable.Rows[i]["user_name"]);

                familyVariableCollection.Parse("U_BLOCK", core.Uri.BuildBlockUserUri((long)(int)familyTable.Rows[i]["user_id"]));
                familyVariableCollection.Parse("U_DELETE", core.Uri.BuildDeleteFamilyUri((long)(int)familyTable.Rows[i]["user_id"]));
            }
        }

        void AccountFamilyManage_Add(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            // all ok, add as a friend
            long friendId = 0;

            try
            {
                friendId = long.Parse(core.Http.Query["id"]);
            }
            catch
            {
                core.Display.ShowMessage("Cannot add to family", "No user specified to add as family. Please go back and try again.");
                return;
            }

            // cannot befriend yourself
            if (friendId == LoggedInMember.UserId)
            {
                core.Display.ShowMessage("Cannot add yourself", "You cannot add yourself as a family member.");
                return;
            }

            // check existing friend-foe status
            DataTable relationsTable = db.Query(string.Format("SELECT relation_type FROM user_relations WHERE relation_me = {0} AND relation_you = {1}",
                LoggedInMember.UserId, friendId));

            for (int i = 0; i < relationsTable.Rows.Count; i++)
            {
                if ((string)relationsTable.Rows[i]["relation_type"] == "FAMILY")
                {
                    core.Display.ShowMessage("Already in family", "You have already added this person to your family.");
                    return;
                }
                if ((string)relationsTable.Rows[i]["relation_type"] == "BLOCKED")
                {
                    core.Display.ShowMessage("Person Blocked", "You have blocked this person, to add them to your family you must first unblock them.");
                    return;
                }
            }

            bool isFriend = false;
            if (db.Query(string.Format("SELECT relation_time_ut FROM user_relations WHERE relation_me = {0} AND relation_you = {1} AND relation_type = 'FAMILY';",
                LoggedInMember.UserId, friendId)).Rows.Count == 1)
            {
                isFriend = true;
            }

            db.BeginTransaction();
            long relationId = db.UpdateQuery(string.Format("INSERT INTO user_relations (relation_me, relation_you, relation_time_ut, relation_type) VALUES ({0}, {1}, UNIX_TIMESTAMP(), 'FAMILY');",
                LoggedInMember.UserId, friendId));

            if (!isFriend)
            {
                db.UpdateQuery(string.Format("INSERT INTO friend_notifications (relation_id, notification_time_ut, notification_read) VALUES ({0}, UNIX_TIMESTAMP(), 0)",
                    relationId));
            }

            db.UpdateQuery(string.Format("UPDATE user_info ui SET ui.user_family = ui.user_family + 1 WHERE ui.user_id = {0};",
                LoggedInMember.UserId));

            SetRedirectUri(BuildUri());
            core.Display.ShowMessage("Added family member", "You have added person to your family.");
        }

        void AccountFamilyManage_Delete(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            // all ok, delete from list of friends
            long friendId = 0;

            try
            {
                friendId = long.Parse(core.Http.Query["id"]);
            }
            catch
            {
                core.Display.ShowMessage("Cannot delete family member", "No family member specified to delete. Please go back and try again.");
                return;
            }

            db.BeginTransaction();
            long deletedRows = db.UpdateQuery(string.Format("DELETE FROM user_relations WHERE relation_me = {0} and relation_you = {1} AND relation_type = 'FAMILY'",
                LoggedInMember.UserId, friendId));

            db.UpdateQuery(string.Format("UPDATE user_info ui SET ui.user_family = ui.user_family - {1} WHERE ui.user_id = {0};",
                LoggedInMember.UserId, deletedRows));

            SetRedirectUri(BuildUri());
            core.Display.ShowMessage("Deleted family member", "You have deleted a family member.");
        }
    }
}
