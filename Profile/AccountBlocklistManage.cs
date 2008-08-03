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
    [AccountSubModule("friends", "block")]
    public class AccountBlocklistManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Manage Block List";
            }
        }

        public override int Order
        {
            get
            {
                return 4;
            }
        }

        public AccountBlocklistManage()
        {
            this.Load += new EventHandler(AccountBlocklistManage_Load);
            this.Show += new EventHandler(AccountBlocklistManage_Show);
        }

        void AccountBlocklistManage_Load(object sender, EventArgs e)
        {
            AddModeHandler("block", new ModuleModeHandler(AccountBlocklistManage_Block));
            AddModeHandler("unblock", new ModuleModeHandler(AccountBlocklistManage_Unblock));
        }

        void AccountBlocklistManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_blocklist_manage");

            DataTable blockTable = db.Query(string.Format("SELECT ur.relation_order, uk.user_name, uk.user_id FROM user_relations ur INNER JOIN user_keys uk ON uk.user_id = ur.relation_you WHERE ur.relation_type = 'BLOCKED' AND ur.relation_me = {0} ORDER BY uk.user_name ASC",
                LoggedInMember.UserId));

            for (int i = 0; i < blockTable.Rows.Count; i++)
            {
                VariableCollection friendsVariableCollection = template.CreateChild("block_list");

                byte order = (byte)blockTable.Rows[i]["relation_order"];

                friendsVariableCollection.Parse("NAME", (string)blockTable.Rows[i]["user_name"]);

                friendsVariableCollection.Parse("U_UNBLOCK", Linker.BuildUnBlockUserUri((long)blockTable.Rows[i]["user_id"]));
            }
        }

        void AccountBlocklistManage_Block(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            // all ok
            long blockId = 0;

            try
            {
                blockId = long.Parse(Request["id"]);
            }
            catch
            {
                Display.ShowMessage("Cannot block person", "No person specified to block. Please go back and try again.");
                return;
            }

            // cannot befriend yourself
            if (blockId == LoggedInMember.UserId)
            {
                Display.ShowMessage("Cannot block person", "You cannot block yourself.");
                return;
            }

            // check existing friend-foe status
            DataTable relationsTable = db.Query(string.Format("SELECT relation_type FROM user_relations WHERE relation_me = {0} AND relation_you = {1}",
                LoggedInMember.UserId, blockId));

            for (int i = 0; i < relationsTable.Rows.Count; i++)
            {
                if ((string)relationsTable.Rows[i]["relation_type"] == "FRIEND")
                {
                    switch (Display.GetConfirmBoxResult())
                    {
                        case ConfirmBoxResult.None:
                            Dictionary<string, string> hiddenFieldList = new Dictionary<string, string>();
                            hiddenFieldList.Add("module", "friends");
                            hiddenFieldList.Add("sub", "block");
                            hiddenFieldList.Add("mode", "block");
                            hiddenFieldList.Add("id", blockId.ToString());

                            Display.ShowConfirmBox(HttpUtility.HtmlEncode(Linker.AppendSid("/account/", true)),
                                "Delete as friend?",
                                "Do you also want to delete this person from your friends list?",
                                hiddenFieldList);
                            return;
                        case ConfirmBoxResult.Yes:
                            // remove from friends
                            db.BeginTransaction();
                            long deletedRows = db.UpdateQuery(string.Format("DELETE FROM user_relations WHERE relation_me = {0} and relation_you = {1} AND relation_type = 'FRIEND';",
                                LoggedInMember.UserId, blockId));

                            db.UpdateQuery(string.Format("UPDATE user_info ui SET ui.user_friends = ui.user_friends - 1 WHERE ui.user_id = {0};",
                                LoggedInMember.UserId));
                            break;
                        case ConfirmBoxResult.No:
                            // don't do anything
                            break;
                    }
                }
                if ((string)relationsTable.Rows[i]["relation_type"] == "BLOCKED")
                {
                    Display.ShowMessage("Person Already Blocked", "You have already blocked this person.");
                    return;
                }
            }

            db.BeginTransaction();
            long relationId = db.UpdateQuery(string.Format("INSERT INTO user_relations (relation_me, relation_you, relation_time_ut, relation_type) VALUES ({0}, {1}, UNIX_TIMESTAMP(), 'BLOCKED');",
                LoggedInMember.UserId, blockId));

            // do not notify

            db.UpdateQuery(string.Format("UPDATE user_info ui SET ui.user_block = ui.user_block + 1 WHERE ui.user_id = {0};",
                LoggedInMember.UserId));

            SetRedirectUri(BuildUri());
            Display.ShowMessage("Blocked Person", "You have blocked a person.");
        }

        void AccountBlocklistManage_Unblock(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            // all ok
            long blockId = 0;

            try
            {
                blockId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Cannot unblock person", "No person specified to unblock. Please go back and try again.");
                return;
            }

            // check existing friend-foe status
            DataTable relationsTable = db.Query(string.Format("SELECT relation_type FROM user_relations WHERE relation_me = {0} AND relation_you = {1}",
                LoggedInMember.UserId, blockId));

            for (int i = 0; i < relationsTable.Rows.Count; i++)
            {
                if ((string)relationsTable.Rows[i]["relation_type"] == "BLOCKED")
                {
                    break;
                }
                else if (i == relationsTable.Rows.Count - 1)
                {
                    Display.ShowMessage("Cannot unblock person", "This person is not blocked, cannot unlock.");
                    return;
                }
            }

            db.BeginTransaction();
            db.UpdateQuery(string.Format("DELETE FROM user_relations WHERE relation_me = {0} AND relation_you = {1} AND relation_type = 'BLOCKED';",
                    LoggedInMember.UserId, blockId));

            // do not notify

            db.UpdateQuery(string.Format("UPDATE user_info ui SET ui.user_block = ui.user_block - 1 WHERE ui.user_id = {0};",
                LoggedInMember.UserId));

            SetRedirectUri(BuildUri());
            Display.ShowMessage("Unblocked Person", "You have unblocked a person.");
        }
    }
}
