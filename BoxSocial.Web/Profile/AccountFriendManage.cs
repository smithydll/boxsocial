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

namespace BoxSocial.Applications.Profile
{
    [AccountSubModule("friends", "friends", true)]
    public class AccountFriendManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Manage Friends";
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
        /// Initializes a new instance of the AccountFriendManage class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountFriendManage(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountFriendManage_Load);
            this.Show += new EventHandler(AccountFriendManage_Show);
        }

        void AccountFriendManage_Load(object sender, EventArgs e)
        {
            AddModeHandler("add", new ModuleModeHandler(AccountFriendManage_Add));
            AddModeHandler("delete", new ModuleModeHandler(AccountFriendManage_Delete));
            AddModeHandler("promote", new ModuleModeHandler(AccountFriendManage_Promote));
            AddModeHandler("demote", new ModuleModeHandler(AccountFriendManage_Demote));
        }

        void AccountFriendManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_friends_manage");

            List<Friend> friends = LoggedInMember.GetFriends(core.TopLevelPageNumber, 50, null);

            foreach (UserRelation friend in friends)
            {
                VariableCollection friendsVariableCollection = template.CreateChild("friend_list");

                int order = friend.RelationOrder;

                friendsVariableCollection.Parse("ID", friend.Id);
                friendsVariableCollection.Parse("NAME", friend.DisplayName);

                if (order > 0)
                {
                    friendsVariableCollection.Parse("ORDER", order.ToString());
                }

                friendsVariableCollection.Parse("U_PROFILE", friend.Uri);
                friendsVariableCollection.Parse("ICON", friend.Icon);
                friendsVariableCollection.Parse("TILE", friend.Tile);
                friendsVariableCollection.Parse("U_BLOCK", core.Hyperlink.BuildBlockUserUri(friend.Id));
                friendsVariableCollection.Parse("U_DELETE", core.Hyperlink.BuildDeleteFriendUri(friend.Id));
                friendsVariableCollection.Parse("U_PROMOTE", core.Hyperlink.BuildPromoteFriendUri(friend.Id));
                friendsVariableCollection.Parse("U_DEMOTE", core.Hyperlink.BuildDemoteFriendUri(friend.Id));
            }

            core.Display.ParsePagination(template, BuildUri(), 50, LoggedInMember.UserInfo.Friends);
        }

        void AccountFriendManage_Add(object sender, EventArgs e)
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
                SetRedirectUri(BuildUri());
                core.Display.ShowMessage("Cannot add friend", "No friend specified to add. Please go back and try again.");
                return;
            }

            // cannot befriend yourself
            if (friendId == LoggedInMember.UserId)
            {
                SetRedirectUri(BuildUri());
                core.Display.ShowMessage("Cannot add friend", "You cannot add yourself as a friend.");
                return;
            }

            // check existing friend-foe status
            DataTable relationsTable = db.Query(string.Format("SELECT relation_type FROM user_relations WHERE relation_me = {0} AND relation_you = {1}",
                LoggedInMember.UserId, friendId));

            for (int i = 0; i < relationsTable.Rows.Count; i++)
            {
                if ((string)relationsTable.Rows[i]["relation_type"] == "FRIEND")
                {
                    core.Display.ShowMessage("Already friend", "You have already added this person as a friend.");
                    return;
                }
                if ((string)relationsTable.Rows[i]["relation_type"] == "BLOCKED")
                {
                    core.Display.ShowMessage("Person Blocked", "You have blocked this person, to add them as a friend you must first unblock them.");
                    return;
                }
            }

            User friendProfile = new User(core, friendId);

            bool isFriend = friendProfile.IsFriend(session.LoggedInMember.ItemKey);

            db.BeginTransaction();
            long relationId = db.UpdateQuery(string.Format("INSERT INTO user_relations (relation_me, relation_you, relation_time_ut, relation_type) VALUES ({0}, {1}, UNIX_TIMESTAMP(), 'FRIEND');",
                LoggedInMember.UserId, friendId));

            //
            // send notifications
            //

            ApplicationEntry ae = core.GetApplication("Profile");

            if (!isFriend)
            {
                ae.SendNotification(core, LoggedInMember, friendProfile, LoggedInMember.ItemKey, LoggedInMember.ItemKey, "_WANTS_FRIENDSHIP", LoggedInMember.Uri, "friend");
            }
            else
            {
                ae.SendNotification(core, LoggedInMember, friendProfile, LoggedInMember.ItemKey, LoggedInMember.ItemKey, "_ACCEPTED_FRIENDSHIP", LoggedInMember.Uri);
            }

            db.UpdateQuery(string.Format("UPDATE user_info ui SET ui.user_friends = ui.user_friends + 1 WHERE ui.user_id = {0};",
                LoggedInMember.UserId));

            SetRedirectUri(BuildUri());
            core.Display.ShowMessage("Added friend", "You have added a friend.");
        }

        void AccountFriendManage_Delete(object sender, EventArgs e)
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
                SetRedirectUri(BuildUri());
                core.Display.ShowMessage("Cannot delete friend", "No friend specified to delete. Please go back and try again.");
                return;
            }

            db.BeginTransaction();
            long deletedRows = db.UpdateQuery(string.Format("DELETE FROM user_relations WHERE relation_me = {0} and relation_you = {1} AND relation_type = 'FRIEND'",
                LoggedInMember.UserId, friendId));

            db.UpdateQuery(string.Format("UPDATE user_info ui SET ui.user_friends = ui.user_friends - {1} WHERE ui.user_id = {0};",
                LoggedInMember.UserId, deletedRows));

            SetRedirectUri(BuildUri());
            core.Display.ShowMessage("Deleted friend", "You have deleted a friend.");
        }

        void AccountFriendManage_Promote(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            // all ok
            long friendId = 0;

            try
            {
                friendId = long.Parse(core.Http.Query["id"]);
            }
            catch
            {
                SetRedirectUri(BuildUri());
                core.Display.ShowMessage("Cannot promote friend", "No friend specified to promote. Please go back and try again.");
                return;
            }

            DataTable friendTable = db.Query(string.Format("SELECT relation_order FROM user_relations WHERE relation_me = {0} AND relation_you = {1} AND relation_type = 'FRIEND'",
                LoggedInMember.UserId, friendId));

            if (friendTable.Rows.Count == 1)
            {
                int relationOrder = (int)friendTable.Rows[0]["relation_order"];

                if (relationOrder == 1)
                {
                    SetRedirectUri(BuildUri());
                    core.Display.ShowMessage("Cannot promote friend", "Cannot promote higher than the number one position.");
                    return;
                }
                else if (relationOrder > 0)
                {
                    // ordered friend

                    // switch places
                    db.BeginTransaction();
                    db.UpdateQuery(string.Format("UPDATE user_relations SET relation_order = relation_order + 1 WHERE relation_me = {0} AND relation_order = {1} AND relation_type = 'FRIEND'",
                        LoggedInMember.UserId, relationOrder - 1));

                    db.UpdateQuery(string.Format("UPDATE user_relations SET relation_order = relation_order - 1 WHERE relation_me = {0} AND relation_you = {1} AND relation_type = 'FRIEND'",
                        LoggedInMember.UserId, friendId));
                }
                else
                {
                    // unordered friend

                    // select the maximum order
                    int maxOrder = (int)db.Query(string.Format("SELECT MAX(relation_order) as max_order FROM user_relations WHERE relation_me = {0} AND relation_type = 'FRIEND'",
                        LoggedInMember.UserId)).Rows[0]["max_order"];

                    // switch places
                    if (maxOrder > 0)
                    {
                        if (maxOrder == 255)
                        {
                            db.BeginTransaction();
                            db.UpdateQuery(string.Format("UPDATE user_relations SET relation_order = 0 WHERE relation_me = {0} AND relation_order = 255",
                                LoggedInMember.UserId));

                            db.UpdateQuery(string.Format("UPDATE user_relations SET relation_order = {2} WHERE relation_me = {0} AND relation_you = {1} AND relation_type = 'FRIEND'",
                            LoggedInMember.UserId, friendId, maxOrder));
                        }
                        else
                        {
                            db.UpdateQuery(string.Format("UPDATE user_relations SET relation_order = {2} WHERE relation_me = {0} AND relation_you = {1} AND relation_type = 'FRIEND'",
                                LoggedInMember.UserId, friendId, maxOrder + 1));
                        }

                    }
                    else
                    {
                        db.UpdateQuery(string.Format("UPDATE user_relations SET relation_order = {2} WHERE relation_me = {0} AND relation_you = {1} AND relation_type = 'FRIEND'",
                            LoggedInMember.UserId, friendId, 1));
                    }
                }

                SetRedirectUri(BuildUri());
                core.Display.ShowMessage("Friend Promoted", "You have successfully promoted your friend in your social hierarchy.");
                return;
            }
            else
            {
                DisplayGenericError();
                return;
            }
        }

        void AccountFriendManage_Demote(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            // all ok
            long friendId = 0;

            try
            {
                friendId = long.Parse(core.Http.Query["id"]);
            }
            catch
            {
                SetRedirectUri(BuildUri());
                core.Display.ShowMessage("Cannot demote friend", "No friend specified to demote. Please go back and try again.");
                return;
            }

            DataTable friendTable = db.Query(string.Format("SELECT relation_order FROM user_relations WHERE relation_me = {0} AND relation_you = {1} AND relation_type = 'FRIEND'",
                LoggedInMember.UserId, friendId));

            if (friendTable.Rows.Count == 1)
            {
                int relationOrder = (int)friendTable.Rows[0]["relation_order"];

                if (relationOrder == 0)
                {
                    // do nothing, already demoted as far as will go, just wave through
                }
                else if (relationOrder == 255)
                {
                    db.UpdateQuery(string.Format("UPDATE user_relations SET relation_order = 0 WHERE relation_me = {0} AND relation_you = {1}",
                        LoggedInMember.UserId, friendId));
                }
                else if (relationOrder < 255)
                {
                    int maxOrder = (int)db.Query(string.Format("SELECT MAX(relation_order) as max_order FROM user_relations WHERE relation_me = {0} AND relation_type = 'FRIEND'",
                        LoggedInMember.UserId)).Rows[0]["max_order"];

                    if (relationOrder == maxOrder)
                    {
                        db.UpdateQuery(string.Format("UPDATE user_relations SET relation_order = 0 WHERE relation_me = {0} AND relation_you = {1}",
                            LoggedInMember.UserId, friendId));
                    }
                    else
                    {
                        // switch places
                        db.BeginTransaction();
                        db.UpdateQuery(string.Format("UPDATE user_relations SET relation_order = relation_order - 1 WHERE relation_me = {0} AND relation_order = {1} AND relation_type = 'FRIEND'",
                            LoggedInMember.UserId, relationOrder + 1));

                        db.UpdateQuery(string.Format("UPDATE user_relations SET relation_order = relation_order + 1 WHERE relation_me = {0} AND relation_you = {1} AND relation_type = 'FRIEND'",
                            LoggedInMember.UserId, friendId));
                    }
                }

                SetRedirectUri(BuildUri());
                core.Display.ShowMessage("Friend Demoted", "You have successfully demoted your friend in your social hierarchy.");
                return;
            }
            else
            {
                DisplayGenericError();
                return;
            }
        }

        internal static void NotifyFriendRequest(Core core, Job job)
        {
            core.LoadUserProfile(job.ItemId);
            User friendProfile = core.PrimitiveCache[job.ItemId];

            ApplicationEntry ae = core.GetApplication("Profile");
            ae.SendNotification(core, core.Session.LoggedInMember, friendProfile, core.LoggedInMemberItemKey, core.LoggedInMemberItemKey, "_WANTS_FRIENDSHIP", core.Session.LoggedInMember.Uri, "friend");
        }
    }
}
