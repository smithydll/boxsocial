﻿/*
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

namespace BoxSocial.Groups
{
    /// <summary>
    /// 
    /// </summary>
    [AccountSubModule("groups", "invite")]
    public class AccountGroupsInvite : AccountSubModule
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

        /// <summary>
        /// Initializes a new instance of the AccountGroupsInvite class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountGroupsInvite(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountGroupsInvite_Load);
            this.Show += new EventHandler(AccountGroupsInvite_Show);
        }

        void AccountGroupsInvite_Load(object sender, EventArgs e)
        {
        }

        void AccountGroupsInvite_Show(object sender, EventArgs e)
        {
            SetTemplate("account_group_invite");

            long groupId = core.Functions.RequestLong("id", 0);

            try
            {
                UserGroup thisGroup = new UserGroup(core, groupId);

                if (!thisGroup.IsGroupMember(LoggedInMember.ItemKey))
                {
                    core.Display.ShowMessage("Error", "You must be a member of a group to invite someone to it.");
                    return;
                }

                switch (thisGroup.GroupType)
                {
                    case "OPEN":
                    case "REQUEST":
                    case "CLOSED":
                    case "PRIVATE":
                        break;
                }

                UserSelectBox inviteUserSelectBox = new UserSelectBox(core, "username");
                inviteUserSelectBox.SelectMultiple = false;

                template.Parse("S_USERNAME", inviteUserSelectBox);
                template.Parse("S_ID", groupId.ToString());
            }
            catch (InvalidGroupException)
            {
                DisplayGenericError();
            }

            Save(new EventHandler(AccountGroupsInvite_Save));
        }

        void AccountGroupsInvite_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long groupId = core.Functions.FormLong("id", 0);

            try
            {
                UserGroup thisGroup = new UserGroup(core, groupId);

                try
                {
                    long userId = UserSelectBox.FormUser(core, "username", 0);

                    core.LoadUserProfile(userId);
                    User inviteMember = core.PrimitiveCache[userId];

                    if (!inviteMember.IsFriend(LoggedInMember.ItemKey))
                    {
                        core.Display.ShowMessage("Error", "You can only invite mutual friends to groups.");
                        return;
                    }

                    if (!thisGroup.IsGroupMember(LoggedInMember.ItemKey))
                    {
                        core.Display.ShowMessage("Error", "You must be a member of a group to invite someone to it.");
                        return;
                    }

                    if (!thisGroup.IsGroupMember(inviteMember.ItemKey))
                    {
                        // use their relation, otherwise you could just create a billion pending friends and still SPAM them with group invites
                        DataTable friendsTable = db.Query(string.Format("SELECT relation_time_ut FROM user_relations WHERE relation_me = {0} AND relation_you = {1} AND relation_type = 'FRIEND';",
                            inviteMember.UserId, LoggedInMember.Id));

                        if (friendsTable.Rows.Count > 0)
                        {
                            ApplicationEntry ae = Application.GetExecutingApplication(core, LoggedInMember);
                            ae.SendNotification(core, LoggedInMember, inviteMember, thisGroup.ItemKey, thisGroup.ItemKey, "_INVITED_YOU_TO_JOIN_A_GROUP", thisGroup.Uri, "invite");

                            SetRedirectUri(thisGroup.Uri);
                            core.Display.ShowMessage("Invited Friend", "You have invited a friend to the group.");
                        }
                        else
                        {
                            SetError("You can only invite people who are friends with you to join a group.");
                            return;
                        }
                    }
                    else
                    {
                        SetError("The person you are trying to invite is already a member of the group. An invitation has not been sent.");
                        return;
                    }
                }
                catch (InvalidUserException)
                {
                    SetError("The user you have entered does not exist.");
                    return;
                }
            }
            catch (InvalidGroupException)
            {
                DisplayGenericError();
                return;
            }
        }
    }
}
