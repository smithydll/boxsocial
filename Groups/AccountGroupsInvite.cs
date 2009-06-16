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

            try
            {
                UserGroup thisGroup = new UserGroup(core, groupId);

                if (!thisGroup.IsGroupMember(LoggedInMember))
                {
                    core.Display.ShowMessage("Error", "You must be a member of a group to invite someone to it.");
                    return;
                }

                switch (thisGroup.GroupType)
                {
                    case "OPEN":
                    case "CLOSED":
                    case "PRIVATE":
                        break;
                }

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

            long groupId = Functions.FormLong("id", 0);
            string username = Request.Form["username"];

            try
            {
                UserGroup thisGroup = new UserGroup(core, groupId);

                try
                {
                    User inviteMember = core.UserProfiles[core.LoadUserProfile(username)];

                    if (!thisGroup.IsGroupMember(LoggedInMember))
                    {
                        core.Display.ShowMessage("Error", "You must be a member of a group to invite someone to it.");
                        return;
                    }

                    if (!thisGroup.IsGroupMember(inviteMember))
                    {
                        // use their relation, otherwise you could just create a billion pending friends and still SPAM them with group invites
                        DataTable friendsTable = db.Query(string.Format("SELECT relation_time_ut FROM user_relations WHERE relation_me = {0} AND relation_you = {1} AND relation_type = 'FRIEND';",
                            inviteMember.UserId, LoggedInMember.Id));

                        if (friendsTable.Rows.Count > 0)
                        {
                            RawTemplate emailTemplate = new RawTemplate(Server.MapPath("./templates/emails/"), "group_invitation.eml");

                            emailTemplate.Parse("TO_NAME", LoggedInMember.DisplayName);
                            emailTemplate.Parse("FROM_NAME", LoggedInMember.DisplayName);
                            emailTemplate.Parse("FROM_USERNAME", LoggedInMember.UserName);
                            emailTemplate.Parse("GROUP_NAME", LoggedInMember.DisplayName);
                            emailTemplate.Parse("U_GROUP", core.Uri.StripSid(thisGroup.UriStubAbsolute));
                            emailTemplate.Parse("U_JOIN", core.Uri.StripSid(core.Uri.AppendAbsoluteSid(thisGroup.JoinUri)));

                            ApplicationEntry ae = Application.GetExecutingApplication(core, LoggedInMember);
                            ae.SendNotification(inviteMember, string.Format("[user]{0}[/user] invited you to join a group.", core.LoggedInMemberId), string.Format("[url={0}]Join {1}[/url]",
                                core.Uri.StripSid(core.Uri.AppendAbsoluteSid(thisGroup.JoinUri)), thisGroup.TitleName), emailTemplate);

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
                    SetError("The username you have entered does not exist.");
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
