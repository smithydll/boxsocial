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

        public AccountGroupsInvite()
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

            long groupId = Functions.RequestLong("id", 0);

            if (groupId == 0)
            {
                DisplayGenericError();
                return;
            }

            UserGroup thisGroup = new UserGroup(core, groupId);

            if (!thisGroup.IsGroupMember(loggedInMember))
            {
                Display.ShowMessage("Error", "You must be a member of a group to invite someone to it.");
                return;
            }

            switch (thisGroup.GroupType)
            {
                case "OPEN":
                case "CLOSED":
                case "PRIVATE":
                    break;
            }

            template.Parse("S_FORM_ACTION", Linker.AppendSid("/account/", true));
            template.Parse("S_ID", groupId.ToString());

            Save(new EventHandler(AccountGroupsInvite_Save));
        }

        void AccountGroupsInvite_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long groupId = Functions.RequestLong("id", 0);
            string username = Request.Form["username"];

            if (groupId == 0)
            {
                DisplayGenericError();
                return;
            }

            try
            {
                UserGroup thisGroup = new UserGroup(core, groupId);

                try
                {
                    User inviteMember = new User(core, username);

                    if (!thisGroup.IsGroupMember(loggedInMember))
                    {
                        Display.ShowMessage("Error", "You must be a member of a group to invite someone to it.");
                        return;
                    }

                    if (!thisGroup.IsGroupMember(inviteMember))
                    {
                        // use their relation, otherwise you could just create a billion pending friends and still SPAM them with group invites
                        DataTable friendsTable = db.Query(string.Format("SELECT relation_time_ut FROM user_relations WHERE relation_me = {0} AND relation_you = {1} AND relation_type = 'FRIEND';",
                            inviteMember.UserId, loggedInMember.UserId));

                        if (friendsTable.Rows.Count > 0)
                        {
                            Template emailTemplate = new Template(Server.MapPath("./templates/emails/"), "group_invitation.eml");

                            emailTemplate.Parse("TO_NAME", inviteMember.DisplayName);
                            emailTemplate.Parse("FROM_NAME", loggedInMember.DisplayName);
                            emailTemplate.Parse("FROM_USERNAME", loggedInMember.UserName);
                            emailTemplate.Parse("GROUP_NAME", thisGroup.DisplayName);
                            emailTemplate.Parse("U_GROUP", "http://zinzam.com" + "/group/" + thisGroup.Slug);
                            emailTemplate.Parse("U_JOIN", "http://zinzam.com" + Linker.StripSid(thisGroup.JoinUri));

                            ApplicationEntry ae = Application.GetExecutingApplication(core, loggedInMember);
                            ae.SendNotification(inviteMember, string.Format("[user]{0}[/user] invited you to join a group.", core.LoggedInMemberId), "{TODO}", emailTemplate);

                            SetRedirectUri(thisGroup.Uri);
                            Display.ShowMessage("Invited Friend", "You have invited a friend to the group.");
                        }
                        else
                        {
                            Display.ShowMessage("Cannot Invite User", "You can only invite people who are friends with you to join a group.");
                            return;
                        }
                    }
                    else
                    {
                        Display.ShowMessage("Already in Group", "The person you are trying to invite is already a member of the group. An invitation has not been sent.");
                        return;
                    }
                }
                catch
                {
                    Display.ShowMessage("Username does not exist", "The username you have entered does not exist, go back.");
                    return;
                }
            }
            catch
            {
                Display.ShowMessage("Error", "An error has occured, go back.");
                return;
            }
        }
    }
}
