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

namespace BoxSocial.Groups
{
    /// <summary>
    /// 
    /// </summary>
    [AccountSubModule("groups", "memberships")]
    public class AccountGroupsMembershipsManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Manage Memberships";
            }
        }

        public override int Order
        {
            get
            {
                return 2;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountGroupsMembershipsManage class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountGroupsMembershipsManage(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountGroupsMembershipsManage_Load);
            this.Show += new EventHandler(AccountGroupsMembershipsManage_Show);
        }

        void AccountGroupsMembershipsManage_Load(object sender, EventArgs e)
        {
            AddModeHandler("join", new ModuleModeHandler(AccountGroupsMembershipsManage_Join));
            AddSaveHandler("join", new EventHandler(AccountGroupsMembershipsManage_Join_Save));
            AddModeHandler("leave", new ModuleModeHandler(AccountGroupsMembershipsManage_Leave));
            AddModeHandler("make-officer", new ModuleModeHandler(AccountGroupsMembershipsManage_MakeOfficer));
            AddSaveHandler("make-officer", new EventHandler(AccountGroupsMembershipsManage_MakeOfficer_Save));
            AddModeHandler("remove-officer", new ModuleModeHandler(AccountGroupsMembershipsManage_RemoveOfficer));
            AddModeHandler("make-operator", new ModuleModeHandler(AccountGroupsMembershipsManage_MakeOperator));
            AddModeHandler("resign-operator", new ModuleModeHandler(AccountGroupsMembershipsManage_ResignOperator));
            AddSaveHandler("resign-operator", new EventHandler(AccountGroupsMembershipsManage_ResignOperator_Save));
            AddModeHandler("approve", new ModuleModeHandler(AccountGroupsMembershipsManage_ApproveMember));
            AddModeHandler("ban-member", new ModuleModeHandler(AccountGroupsMembershipsManage_BanMember));
            AddSaveHandler("ban-member", new EventHandler(AccountGroupsMembershipsManage_BanMember_Save));
        }

        void AccountGroupsMembershipsManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_group_membership");

            long groupTypeId = ItemKey.GetTypeId(typeof(UserGroup));

            SelectQuery query = GroupMember.GetSelectQueryStub(core, UserLoadOptions.Common);
            query.AddCondition("user_keys.user_id", LoggedInMember.Id);

            DataTable membershipGroupsTable = db.Query(query);

            List<long> groupIds = new List<long>();
            for (int i = 0; i < membershipGroupsTable.Rows.Count; i++)
            {
                long groupId = (long)membershipGroupsTable.Rows[i]["group_id"];
                core.PrimitiveCache.LoadPrimitiveProfile(groupId, groupTypeId);
                groupIds.Add(groupId);
            }

            int pending = 0;
            int approved = 0;
            for (int i = 0; i < membershipGroupsTable.Rows.Count; i++)
            {
                VariableCollection groupVariableCollection = null;
                UserGroup thisGroup = null;

                try
                {
                    thisGroup = (UserGroup)core.PrimitiveCache[(long)membershipGroupsTable.Rows[i]["group_id"], groupTypeId];
                }
                catch
                {
                    continue;
                }

                if ((byte)membershipGroupsTable.Rows[i]["group_member_approved"] == 0)
                {
                    groupVariableCollection = template.CreateChild("pending_list");
                    pending++;

                    groupVariableCollection.Parse("U_LEAVE", thisGroup.LeaveUri);
                }
                else if ((byte)membershipGroupsTable.Rows[i]["group_member_approved"] == 1)
                {
                    groupVariableCollection = template.CreateChild("group_list");
                    approved++;

                    GroupMember gm = new GroupMember(core, membershipGroupsTable.Rows[i], UserLoadOptions.Common);

                    if (!gm.IsOperator)
                    {
                        groupVariableCollection.Parse("U_LEAVE", thisGroup.LeaveUri);
                    }

                    groupVariableCollection.Parse("U_INVITE", thisGroup.InviteUri);
                }

                groupVariableCollection.Parse("GROUP_DISPLAY_NAME", thisGroup.DisplayName);
                groupVariableCollection.Parse("MEMBERS", thisGroup.Members.ToString());

                groupVariableCollection.Parse("U_VIEW", thisGroup.Uri);
                groupVariableCollection.Parse("U_MEMBERLIST", thisGroup.MemberlistUri);

                switch (thisGroup.GroupType)
                {
                    case "OPEN":
                        groupVariableCollection.Parse("GROUP_TYPE", "Open");
                        break;
                    case "REQUEST":
                        groupVariableCollection.Parse("GROUP_TYPE", "Request");
                        break;
                    case "CLOSED":
                        groupVariableCollection.Parse("GROUP_TYPE", "Closed");
                        break;
                    case "PRIVATE":
                        groupVariableCollection.Parse("GROUP_TYPE", "Private");
                        break;
                }
            }

            if (pending > 0)
            {
                template.Parse("PENDING_MEMBERSHIPS", "TRUE");
            }

            if (approved > 0)
            {
                template.Parse("GROUP_MEMBERSHIPS", "TRUE");
            }
        }

        void AccountGroupsMembershipsManage_Join(object sender, ModuleModeEventArgs e)
        {
            long groupId = core.Functions.RequestLong("id", 0);
            UserGroup thisGroup;

            if (AuthorisedRequest())
            {
                AccountGroupsMembershipsManage_Join_Save(sender, new EventArgs());
            }
            else
            {

                try
                {
                    thisGroup = new UserGroup(core, groupId);
                }
                catch (InvalidGroupException)
                {
                    DisplayGenericError();
                    return;
                }

                Dictionary<string, string> hiddenFieldList = new Dictionary<string, string>();
                hiddenFieldList.Add("module", "groups");
                hiddenFieldList.Add("sub", "memberships");
                hiddenFieldList.Add("mode", "join");
                hiddenFieldList.Add("id", groupId.ToString());

                core.Display.ShowConfirmBox(HttpUtility.HtmlEncode(core.Hyperlink.AppendSid(Owner.AccountUriStub, true)),
                    "Confirm join group",
                    "Do you want to join the group `" + thisGroup.DisplayName + "`?",
                    hiddenFieldList);
            }
        }

        void AccountGroupsMembershipsManage_Join_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long groupId = core.Functions.FormLong("id", core.Functions.RequestLong("id", 0));

            if (groupId == 0)
            {
                core.Display.ShowMessage("Error", "Unable to complete action, missing data. Go back and try again.");
                return;
            }

            if (core.Display.GetConfirmBoxResult() == ConfirmBoxResult.Yes || core.Functions.RequestLong("id", 0) == groupId)
            {
                try
                {
                    UserGroup thisGroup = new UserGroup(core, groupId);
                    int activated = 0;

                    DataTable membershipTable = db.Query(string.Format("SELECT user_id FROM group_members WHERE group_id = {0} AND user_id = {1};",
                        thisGroup.GroupId, LoggedInMember.Id));

                    if (membershipTable.Rows.Count > 0)
                    {
                        SetRedirectUri(thisGroup.Uri);
                        core.Display.ShowMessage("Already a Member", "You are already a member of this group.");
                        return;
                    }

                    switch (thisGroup.GroupType)
                    {
                        case "OPEN":
                        case "PRIVATE": // assume as you've been invited that it is enough for activation
                            activated = 1;
                            break;
                        case "REQUEST":
                        case "CLOSED":
                            activated = 0;
                            break;
                    }

                    bool isInvited = thisGroup.IsGroupInvitee(LoggedInMember);

                    // do not need an invite unless the group is private
                    // private groups you must be invited to
                    if (thisGroup.GroupType != "PRIVATE" || (thisGroup.GroupType == "PRIVATE" && isInvited))
                    {
                        db.BeginTransaction();
                        db.UpdateQuery(string.Format("INSERT INTO group_members (group_id, user_id, group_member_approved, group_member_ip, group_member_date_ut) VALUES ({0}, {1}, {2}, '{3}', UNIX_TIMESTAMP());",
                            thisGroup.GroupId, LoggedInMember.Id, activated, Mysql.Escape(session.IPAddress.ToString()), true));

                        if (activated == 1)
                        {
                            db.UpdateQuery(string.Format("UPDATE group_info SET group_members = group_members + 1 WHERE group_id = {0}",
                                thisGroup.GroupId));
                        }

                        // just do it anyway, can be invited to any type of group
                        db.UpdateQuery(string.Format("DELETE FROM group_invites WHERE group_id = {0} AND user_id = {1}",
                            thisGroup.GroupId, LoggedInMember.Id));

                        SetRedirectUri(thisGroup.Uri);
                        if (thisGroup.GroupType == "OPEN" || thisGroup.GroupType == "PRIVATE")
                        {
                            core.Display.ShowMessage("Joined Group", "You have joined this group.");
                        }
                        else if (thisGroup.GroupType == "CLOSED")
                        {
                            core.Display.ShowMessage("Joined Group", "You applied to join this group. A group operator must approve your membership before you will be admitted into the group.");
                        }
                        return;
                    }
                    else
                    {
                        core.Display.ShowMessage("Cannot join group", "This group is private, you must be invited to be able to join it.");
                        return;
                    }
                }
                catch
                {
                    core.Display.ShowMessage("Group does not Exist", "The group you are trying to join does not exist.");
                    return;
                }
            }
            else
            {
                SetRedirectUri(BuildUri());
                core.Display.ShowMessage("Cancelled", "You cancelled joining the group.");
            }
        }

        void AccountGroupsMembershipsManage_Leave(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            long groupId = 0;

            try
            {
                groupId = long.Parse(core.Http.Query["id"]);
            }
            catch
            {
                core.Display.ShowMessage("Error", "Unable to complete action, missing data. Go back and try again.");
                return;
            }

            try
            {
                UserGroup thisGroup = new UserGroup(core, groupId);

                bool isGroupMemberPending = thisGroup.IsGroupMemberPending(LoggedInMember.ItemKey);
                bool isGroupMember = thisGroup.IsGroupMember(LoggedInMember.ItemKey);

                DataTable operatorsTable = db.Query(string.Format("SELECT user_id FROM group_operators WHERE group_id = {0} AND user_id = {1};",
                    thisGroup.GroupId, LoggedInMember.Id));

                if (operatorsTable.Rows.Count > 0)
                {
                    SetRedirectUri(thisGroup.Uri);
                    core.Display.ShowMessage("Cannot Leave Group", "You cannot leave this group while you are an operator of the group.");
                    return;
                }
                else
                {
                    if (isGroupMember)
                    {
                        db.BeginTransaction();
                        db.UpdateQuery(string.Format("DELETE FROM group_members WHERE group_id = {0} AND user_id = {1};",
                            thisGroup.GroupId, LoggedInMember.Id));

                        long officerRowsChanged = db.UpdateQuery(string.Format("DELETE FROM group_officers WHERE group_id = {0} AND user_id = {1};",
                            thisGroup.GroupId, LoggedInMember.Id));

                        db.UpdateQuery(string.Format("UPDATE group_info SET group_members = group_members - 1, group_officers = group_officers - {1} WHERE group_id = {0}",
                            thisGroup.GroupId, officerRowsChanged));

                        SetRedirectUri(thisGroup.Uri);
                        core.Display.ShowMessage("Left Group", "You have left the group.");
                        return;
                    }
                    else if (isGroupMemberPending)
                    {
                        db.UpdateQuery(string.Format("DELETE FROM group_members WHERE group_id = {0} AND user_id = {1};",
                            thisGroup.GroupId, LoggedInMember.UserId));

                        SetRedirectUri(thisGroup.Uri);
                        core.Display.ShowMessage("Left Group", "You are no longer pending membership of the group.");
                        return;
                    }
                    else
                    {
                        SetRedirectUri(thisGroup.Uri);
                        core.Display.ShowMessage("Not a Member", "You cannot leave a group you are not a member of.");
                        return;
                    }
                }
            }
            catch (InvalidGroupException)
            {
                core.Display.ShowMessage("Group does not Exist", "The group you are trying to leave does not exist.");
                return;
            }
        }

        void AccountGroupsMembershipsManage_MakeOfficer(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_group_appoint_officer");

            long groupId;
            long userId;

            try
            {
                string[] idString = core.Http.Query["id"].Split(new char[] { ',' });
                groupId = long.Parse(idString[0]);
                userId = long.Parse(idString[1]);
            }
            catch
            {
                DisplayGenericError();
                return;
            }

            try
            {
                UserGroup thisGroup = new UserGroup(core, groupId);

                if (thisGroup.IsGroupOperator(LoggedInMember.ItemKey))
                {
                    try
                    {
                        User member = new User(core, userId);

                        if (thisGroup.IsGroupMember(member.ItemKey))
                        {
                            // all ok, don't really need to do much, so let's do it
                            template.Parse("S_ID", string.Format("{0},{1}", groupId, userId));
                            template.Parse("S_USERNAME", member.UserName);
                        }
                        else
                        {
                            core.Functions.ThrowError();
                            return;
                        }
                    }
                    catch
                    {
                        core.Functions.ThrowError();
                        return;
                    }
                }
                else
                {
                    core.Display.ShowMessage("Unauthorised", "You must be the group operator to appoint an operator.");
                    return;
                }
            }
            catch
            {
                core.Functions.ThrowError();
                return;
            }
        }

        void AccountGroupsMembershipsManage_MakeOfficer_Save(object sender, EventArgs e)
        {
            long groupId = 0;
            long userId = 0;
            string title;

            try
            {
                string[] idString = core.Http.Form["id"].Split(new char[] { ',' });
                groupId = long.Parse(idString[0]);
                userId = long.Parse(idString[1]);
                title = core.Http.Form["title"];
            }
            catch
            {
                core.Functions.ThrowError();
                return;
            }

            if (string.IsNullOrEmpty(title))
            {
                core.Display.ShowMessage("Officer Title Empty", "The officer title must not be empty, go back and enter an officer title.");
                return;
            }
            else
            {
                if (title.Length < 4)
                {
                    core.Display.ShowMessage("Officer Title Too Short", "The officer title must be at least four characters, go back and enter an officer title.");
                    return;
                }
                else if (title.Length > 24)
                {
                    core.Display.ShowMessage("Officer Title Too Long", "The officer title must be at most twenty four characters, go back and enter an officer title.");
                    return;
                }
            }

            try
            {
                UserGroup thisGroup = new UserGroup(core, groupId);

                if (thisGroup.IsGroupOperator(LoggedInMember.ItemKey))
                {
                    try
                    {
                        User member = new User(core, userId);

                        if (thisGroup.IsGroupMember(member.ItemKey))
                        {
                            // allow to be an officer to many things
                            db.BeginTransaction();
                            long status = db.UpdateQuery(string.Format("INSERT INTO group_officers (group_id, user_id, officer_title) VALUES ({0}, {1}, '{2}');",
                                thisGroup.GroupId, member.UserId, Mysql.Escape(title)));

                            if (status >= 0)
                            {
                                db.UpdateQuery(string.Format("UPDATE group_info SET group_officers = group_officers + 1 WHERE group_id = {0}",
                                    thisGroup.GroupId));

                                SetRedirectUri(thisGroup.Uri);
                                core.Display.ShowMessage("Officer Appointed to Group", "You have successfully appointed an officer to the group.");
                            }
                            else
                            {
                                core.Display.ShowMessage("Already Officer", "This member is already appointed as this officer.");
                                return;
                            }
                        }
                        else
                        {
                            core.Functions.ThrowError();
                            return;
                        }
                    }
                    catch
                    {
                        core.Functions.ThrowError();
                        return;
                    }
                }
                else
                {
                    core.Display.ShowMessage("Unauthorised", "You must be the group operator to appoint an officer.");
                    return;
                }
            }
            catch
            {
                core.Functions.ThrowError();
                return;
            }
        }

        void AccountGroupsMembershipsManage_RemoveOfficer(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            long groupId;
            long userId;
            string title;

            try
            {
                string[] idString = core.Http.Query["id"].Split(new char[] { ',' });
                groupId = long.Parse(idString[0]);
                userId = long.Parse(idString[1]);
                title = UTF8Encoding.UTF8.GetString(Convert.FromBase64String(idString[2]));
            }
            catch
            {
                DisplayGenericError();
                return;
            }

            try
            {
                UserGroup thisGroup = new UserGroup(core, groupId);

                if (thisGroup.IsGroupOperator(LoggedInMember.ItemKey))
                {
                    db.BeginTransaction();
                    long deletedRows = db.UpdateQuery(string.Format("DELETE FROM group_officers WHERE group_id = {0} AND user_id = {1} AND officer_title = '{2}'",
                        groupId, userId, Mysql.Escape(title)));

                    if (deletedRows >= 0)
                    {
                        db.UpdateQuery(string.Format("UPDATE group_info SET group_officers = group_officers - {1} WHERE group_id = {0}",
                            thisGroup.GroupId, deletedRows));

                        SetRedirectUri(thisGroup.Uri);
                        core.Display.ShowMessage("Officer Removed from Group", "You have successfully removed an officer from the group.");
                    }
                    else
                    {
                        core.Display.ShowMessage("Error", "Could not delete officer, they may have already been delted.");
                        return;
                    }
                }
            }
            catch (InvalidGroupException)
            {
                DisplayGenericError();
                return;
            }
        }

        void AccountGroupsMembershipsManage_MakeOperator(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            long groupId;
            long userId;

            try
            {
                string[] idString = core.Http.Query["id"].Split(new char[] { ',' });
                groupId = long.Parse(idString[0]);
                userId = long.Parse(idString[1]);
            }
            catch
            {
                core.Display.ShowMessage("Error", "An error has occured, go back.");
                return;
            }

            try
            {
                UserGroup thisGroup = new UserGroup(core, groupId);

                if (thisGroup.IsGroupOperator(LoggedInMember.ItemKey))
                {
                    try
                    {
                        User member = new User(core, userId);
                        if (!thisGroup.IsGroupOperator(member.ItemKey))
                        {
                            db.BeginTransaction();
                            db.UpdateQuery(string.Format("INSERT INTO group_operators (group_id, user_id) VALUES ({0}, {1});",
                                thisGroup.GroupId, userId));

                            db.UpdateQuery(string.Format("UPDATE group_info SET group_operators = group_operators + 1 WHERE group_id = {0}",
                                thisGroup.GroupId));

                            SetRedirectUri(thisGroup.Uri);
                            core.Display.ShowMessage("Operator Appointed to Group", "You have successfully appointed an operator to the group.");
                        }
                        else
                        {
                            SetRedirectUri(thisGroup.Uri);
                            core.Display.ShowMessage("Already an Officer", "This member is already an officer.");
                            return;
                        }
                    }
                    catch
                    {
                        DisplayGenericError();
                        return;
                    }
                }
                else
                {
                    SetRedirectUri(thisGroup.Uri);
                    core.Display.ShowMessage("Unauthorised", "You must be the group operator to appoint an operator.");
                    return;
                }
            }
            catch
            {
                DisplayGenericError();
                return;
            }
        }

        void AccountGroupsMembershipsManage_ResignOperator(object sender, ModuleModeEventArgs e)
        {
            long groupId = core.Functions.RequestLong("id", 0);

            if (groupId == 0)
            {
                DisplayGenericError();
                return;
            }

            Dictionary<string, string> hiddenFieldList = new Dictionary<string, string>();
            hiddenFieldList.Add("module", "groups");
            hiddenFieldList.Add("sub", "memberships");
            hiddenFieldList.Add("mode", "resign-operator");
            hiddenFieldList.Add("id", groupId.ToString());

            core.Display.ShowConfirmBox(HttpUtility.HtmlEncode(core.Hyperlink.AppendSid(Owner.AccountUriStub, true)),
                "Are you sure you want to resign as operator from this group?",
                "When you resign as operator from this group, you can only become operator again if appointed by another operator. Once you confirm resignation it is final.",
                hiddenFieldList);
        }

        void AccountGroupsMembershipsManage_ResignOperator_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long groupId = core.Functions.RequestLong("id", 0);

            if (groupId == 0)
            {
                DisplayGenericError();
                return;
            }

            UserGroup thisGroup = new UserGroup(core, groupId);

            if (core.Display.GetConfirmBoxResult() == ConfirmBoxResult.Yes)
            {
                if (thisGroup.IsGroupOperator(LoggedInMember.ItemKey))
                {
                    if (thisGroup.Operators > 1)
                    {
                        db.BeginTransaction();
                        long deletedRows = db.UpdateQuery(string.Format("DELETE FROM group_operators WHERE group_id = {0} AND user_id = {1}",
                            thisGroup.GroupId, LoggedInMember.UserId));

                        db.UpdateQuery(string.Format("UPDATE group_info SET group_operators = group_operators - {1} WHERE group_id = {0}",
                            thisGroup.GroupId, deletedRows));

                        SetRedirectUri(thisGroup.Uri);
                        core.Display.ShowMessage("Success", "You successfully resigned as a group operator. You are still a member of the group. You will be redirected in a second.");
                    }
                    else
                    {
                        core.Display.ShowMessage("Cannot resign as operator", "Groups must have at least one operator, you cannot resign from this group at this moment.");
                        return;
                    }
                }
                else
                {
                    core.Display.ShowMessage("Error", "An error has occured. You are not an operator of this group, go back.");
                    return;
                }
            }
            else
            {
                SetRedirectUri(thisGroup.Uri);
                core.Display.ShowMessage("Cancelled", "You cancelled resignation from being a group operator.");
            }
        }

        void AccountGroupsMembershipsManage_ApproveMember(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            long groupId;
            long userId;

            try
            {
                string[] idString = core.Http.Query["id"].Split(new char[] { ',' });
                groupId = long.Parse(idString[0]);
                userId = long.Parse(idString[1]);
            }
            catch
            {
                DisplayGenericError();
                return;
            }

            try
            {
                UserGroup thisGroup = new UserGroup(core, groupId);

                if (thisGroup.IsGroupOperator(LoggedInMember.ItemKey))
                {
                    try
                    {
                        User member = new User(core, userId);

                        if (thisGroup.IsGroupMemberPending(member.ItemKey))
                        {
                            // we can approve the pending membership
                            db.BeginTransaction();
                            long rowsChanged = db.UpdateQuery(string.Format("UPDATE group_members SET group_member_approved = 1, group_member_date_ut = UNIX_TIMESTAMP() WHERE group_id = {0} AND user_id = {1} AND group_member_approved = 0;",
                                thisGroup.GroupId, member.UserId));

                            if (rowsChanged > 0) // committ the change
                            {
                                db.UpdateQuery(string.Format("UPDATE group_info SET group_members = group_members + 1 WHERE group_id = {0}",
                                    thisGroup.GroupId));

                                SetRedirectUri(thisGroup.MemberlistUri);
                                core.Display.ShowMessage("Membership Approved", "You have approved the membership for the user.");
                                return;
                            }
                            else
                            {
                                core.Display.ShowMessage("Not Pending", "This member is not pending membership. They may have cancelled their request, or been approved by another operator.");
                                return;
                            }
                        }
                        else
                        {
                            core.Display.ShowMessage("Not Pending", "This member is not pending membership. They may have cancelled their request, or been approved by another operator.");
                            return;
                        }
                    }
                    catch
                    {
                        core.Display.ShowMessage("Error", "An error has occured, group member does not exist, go back.");
                        return;
                    }
                }
                else
                {
                    core.Display.ShowMessage("Not Group Operator", "You must be an operator of the group to approve new memberships.");
                    return;
                }
            }
            catch
            {
                core.Display.ShowMessage("Error", "An error has occured, group does not exist, go back.");
                return;
            }
        }

        void AccountGroupsMembershipsManage_BanMember(object sender, ModuleModeEventArgs e)
        {
            long groupId;
            long userId;

            try
            {
                string[] idString = core.Http.Query["id"].Split(new char[] { ',' });
                groupId = long.Parse(idString[0]);
                userId = long.Parse(idString[1]);
            }
            catch
            {
                core.Functions.ThrowError();
                return;
            }

            Dictionary<string, string> hiddenFieldList = new Dictionary<string, string>();
            hiddenFieldList.Add("module", "groups");
            hiddenFieldList.Add("sub", "memberships");
            hiddenFieldList.Add("mode", "ban-member");
            hiddenFieldList.Add("id", string.Format("{0},{1}", groupId, userId));

            core.Display.ShowConfirmBox(HttpUtility.HtmlEncode(core.Hyperlink.AppendSid("/account/", true)),
                "Are you sure you want to ban this member?",
                "Banning a member from the group prevents them from seeing, or participating in the group.",
                hiddenFieldList);
        }

        void AccountGroupsMembershipsManage_BanMember_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long groupId;
            long userId;

            try
            {
                string[] idString = core.Http.Form["id"].Split(new char[] { ',' });
                groupId = long.Parse(idString[0]);
                userId = long.Parse(idString[1]);
            }
            catch
            {
                core.Functions.ThrowError();
                return;
            }

            if (core.Display.GetConfirmBoxResult() == ConfirmBoxResult.Yes)
            {
                try
                {
                    UserGroup group = new UserGroup(core, groupId);

                    if (group.IsGroupOperator(LoggedInMember.ItemKey))
                    {
                        try
                        {
                            GroupMember member = new GroupMember(core, group, userId);

                            member.Ban();

                            core.Display.ShowMessage("Member Banned", "The member has been banned from the group.");
                            return;
                        }
                        catch (InvalidUserException)
                        {
                            DisplayGenericError();
                            return;
                        }
                    }
                    else
                    {
                        core.Display.ShowMessage("Cannot ban member", "Only group operators can ban members from groups.");
                        return;
                    }
                }
                catch (InvalidGroupException)
                {
                    DisplayGenericError();
                    return;
                }
            }
            else
            {
                core.Display.ShowMessage("Cancelled", "You cancelled the banning of this member.");
                return;
            }
        }
    }
}
