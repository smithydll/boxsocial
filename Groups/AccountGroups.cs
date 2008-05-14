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
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Groups
{
    public class AccountGroups : AccountModule
    {
        public AccountGroups(Account account)
            : base(account)
        {
            RegisterSubModule += new RegisterSubModuleHandler(ManageGroups);
            RegisterSubModule += new RegisterSubModuleHandler(ManageGroupMemberships);
            RegisterSubModule += new RegisterSubModuleHandler(DeleteGroup);
            RegisterSubModule += new RegisterSubModuleHandler(EditGroup);
            RegisterSubModule += new RegisterSubModuleHandler(JoinGroup);
            RegisterSubModule += new RegisterSubModuleHandler(LeaveGroup);
            RegisterSubModule += new RegisterSubModuleHandler(InviteGroup);
            RegisterSubModule += new RegisterSubModuleHandler(GroupMakeOfficer);
            RegisterSubModule += new RegisterSubModuleHandler(GroupMakeOperator);
            RegisterSubModule += new RegisterSubModuleHandler(GroupRemoveOfficer);
            RegisterSubModule += new RegisterSubModuleHandler(GroupResignOperator);
            RegisterSubModule += new RegisterSubModuleHandler(GroupApproveMember);
            RegisterSubModule += new RegisterSubModuleHandler(GroupBanMember);
        }

        protected override void RegisterModule(Core core, EventArgs e)
        {
        }

        public override string Name
        {
            get
            {
                return "Groups";
            }
        }

        public override string Key
        {
            get
            {
                return "groups";
            }
        }

        public override int Order
        {
            get
            {
                return 7;
            }
        }

        private void ManageGroups(string submodule)
        {
            subModules.Add("groups", "Manage Groups");
            if (submodule != "groups" && !string.IsNullOrEmpty(submodule)) return;

            template.SetTemplate("Groups", "account_group_manage");

            template.ParseVariables("U_CREATE_GROUP", HttpUtility.HtmlEncode(Linker.AppendSid("/groups/create")));

            DataTable groupsTable = db.Query(string.Format("SELECT {1} FROM group_operators go INNER JOIN group_keys gk ON go.group_id = gk.group_id INNER JOIN group_info gi ON gk.group_id = gi.group_id WHERE go.user_id = {0}",
                loggedInMember.UserId, UserGroup.GROUP_INFO_FIELDS));

            for (int i = 0; i < groupsTable.Rows.Count; i++)
            {
                VariableCollection groupVariableCollection = template.CreateChild("group_list");

                UserGroup thisGroup = new UserGroup(core, groupsTable.Rows[i]);

                groupVariableCollection.ParseVariables("GROUP_DISPLAY_NAME", HttpUtility.HtmlEncode(thisGroup.DisplayName));
                groupVariableCollection.ParseVariables("MEMBERS", HttpUtility.HtmlEncode(thisGroup.Members.ToString()));

                groupVariableCollection.ParseVariables("U_VIEW", HttpUtility.HtmlEncode(thisGroup.Uri));
                groupVariableCollection.ParseVariables("U_MEMBERLIST", HttpUtility.HtmlEncode(thisGroup.MemberlistUri));
                groupVariableCollection.ParseVariables("U_EDIT", HttpUtility.HtmlEncode(thisGroup.EditUri));
                groupVariableCollection.ParseVariables("U_DELETE", HttpUtility.HtmlEncode(thisGroup.DeleteUri));

                switch (thisGroup.GroupType)
                {
                    case "OPEN":
                        groupVariableCollection.ParseVariables("GROUP_TYPE", HttpUtility.HtmlEncode("Open"));
                        break;
                    case "CLOSED":
                        groupVariableCollection.ParseVariables("GROUP_TYPE", HttpUtility.HtmlEncode("Closed"));
                        break;
                    case "PRIVATE":
                        groupVariableCollection.ParseVariables("GROUP_TYPE", HttpUtility.HtmlEncode("Private"));
                        break;
                }
            }
        }

        private void DeleteGroup(string submodule)
        {
            subModules.Add("delete", null);
            if (submodule != "delete") return;

            if (Display.GetConfirmBoxResult() != ConfirmBoxResult.None)
            {
                DeleteGroupSave();
                return;
            }

            long groupId = Functions.RequestLong("id", -1);

            if (groupId >= 0)
            {
                Dictionary<string, string> hiddenFieldList = new Dictionary<string, string>();
                hiddenFieldList.Add("module", "groups");
                hiddenFieldList.Add("sub", "delete");
                hiddenFieldList.Add("id", groupId.ToString());

                Display.ShowConfirmBox(HttpUtility.HtmlEncode(Linker.AppendSid("/account/", true)),
                    "Are you sure you want to delete this group?",
                    "When you delete this group, all information is also deleted and cannot be undone. Deleting a group is final.",
                    hiddenFieldList);
            }
            else
            {
                Functions.ThrowError();
                return;
            }
        }

        public void DeleteGroupSave()
        {
            AuthoriseRequestSid();

            long groupId = Functions.RequestLong("id", -1);

            if (Request.Form["1"] != null)
            {
                try
                {
                    UserGroup group = new UserGroup(core, groupId);

                    SetRedirectUri(BuildModuleUri("groups"));
                    Display.ShowMessage("Cancelled", "This feature is currently not supported.");
                    return;
                }
                catch (InvalidGroupException)
                {
                    Display.ShowMessage("Error", "An error has occured, go back.");
                    return;
                }
            }
            else if (Request.Form["0"] != null)
            {
                Display.ShowMessage("Cancelled", "You cancelled the deletion of the group.");
                return;
            }
        }

        private void EditGroup(string submodule)
        {
            subModules.Add("edit", null);
            if (submodule != "edit") return;

            if (Request.Form["save"] != null)
            {
                EditGroupSave();
                return;
            }

            long groupId;

            template.SetTemplate("Groups", "account_group_edit");

            try
            {
                groupId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Error", "An error has occured, go back.");
                return;
            }

            UserGroup thisGroup = new UserGroup(core, groupId);

            if (!thisGroup.IsGroupOperator(loggedInMember))
            {
                Display.ShowMessage("Cannot Edit Group", "You must be an operator of the group to edit it.");
                return;
            }

            short category = thisGroup.RawCategory;

            Dictionary<string, string> categories = new Dictionary<string, string>();
            DataTable categoriesTable = db.Query("SELECT category_id, category_title FROM global_categories ORDER BY category_title ASC;");
            foreach (DataRow categoryRow in categoriesTable.Rows)
            {
                categories.Add(((short)categoryRow["category_id"]).ToString(), (string)categoryRow["category_title"]);
            }

            template.ParseVariables("S_EDIT_GROUP", HttpUtility.HtmlEncode(Linker.AppendSid("/account/", true)));
            template.ParseVariables("S_CATEGORIES", Functions.BuildSelectBox("category", categories, category.ToString()));
            template.ParseVariables("S_GROUP_ID", HttpUtility.HtmlEncode(thisGroup.GroupId.ToString()));
            template.ParseVariables("GROUP_DISPLAY_NAME", HttpUtility.HtmlEncode(thisGroup.DisplayName));
            template.ParseVariables("GROUP_DESCRIPTION", HttpUtility.HtmlEncode(thisGroup.Description));

            string selected = "checked=\"checked\" ";
            switch (thisGroup.GroupType)
            {
                case "OPEN":
                    template.ParseVariables("S_OPEN_CHECKED", selected);
                    break;
                case "CLOSED":
                    template.ParseVariables("S_CLOSED_CHECKED", selected);
                    break;
                case "PRIVATE":
                    template.ParseVariables("S_PRIVATE_CHECKED", selected);
                    break;
            }
        }

        private void EditGroupSave()
        {
            AuthoriseRequestSid();

            long groupId;
            short category;
            string title;
            string description;
            string type;

            try
            {
                groupId = long.Parse(Request.Form["id"]);
                category = short.Parse(Request.Form["category"]);
                title = Request.Form["title"];
                description = Request.Form["description"];
                type = Request.Form["type"];
            }
            catch
            {
                Display.ShowMessage("Error", "An error has occured, go back.");
                return;
            }

            switch (type)
            {
                case "open":
                    type = "OPEN";
                    break;
                case "closed":
                    type = "CLOSED";
                    break;
                case "private":
                    type = "PRIVATE";
                    break;
                default:
                    Display.ShowMessage("Error", "An error has occured, go back.");
                    return;
            }

            UserGroup thisGroup = new UserGroup(core, groupId);

            if (!thisGroup.IsGroupOperator(loggedInMember))
            {
                Display.ShowMessage("Cannot Edit Group", "You must be an operator of the group to edit it.");
                return;
            }
            else
            {

                // update the public viewcount is necessary
                if (type != "PRIVATE" && thisGroup.GroupType == "PRIVATE")
                {
                    db.BeginTransaction();
                    db.UpdateQuery(string.Format("UPDATE global_categories SET category_groups = category_groups + 1 WHERE category_id = {0}",
                        category));
                }
                else if (type == "PRIVATE" && thisGroup.GroupType != "PRIVATE")
                {
                    db.UpdateQuery(string.Format("UPDATE global_categories SET category_groups = category_groups - 1 WHERE category_id = {0}",
                        category));
                }

                // save the edits to the group
                db.UpdateQuery(string.Format("UPDATE group_info SET group_name_display = '{1}', group_category = {2}, group_abstract = '{3}', group_type = '{4}' WHERE group_id = {0}",
                    thisGroup.GroupId, Mysql.Escape(title), category, Mysql.Escape(description), Mysql.Escape(type)));

                SetRedirectUri(thisGroup.Uri);
                Display.ShowMessage("Group Saved", "You have successfully edited the group.");
                return;
            }
        }

        private void ManageGroupMemberships(string submodule)
        {
            subModules.Add("memberships", "Manage Memberships");
            if (submodule != "memberships") return;

            template.SetTemplate("Groups", "account_group_membership");

            SelectQuery query = new SelectQuery("group_members gm");
            query.AddFields(UserGroup.GROUP_INFO_FIELDS);
            query.AddJoin(JoinTypes.Inner, "group_keys gk", "gm.group_id", "gk.group_id");
            query.AddJoin(JoinTypes.Inner, "group_info gi", "gm.group_id", "gi.group_id");
            query.AddCondition("gm.user_id", loggedInMember.UserId);
            query.AddCondition("gm.group_member_approved", 0);

            DataTable pendingGroupsTable = db.Query(query);

            if (pendingGroupsTable.Rows.Count > 0)
            {
                template.ParseVariables("PENDING_MEMBERSHIPS", "TRUE");
            }

            for (int i = 0; i < pendingGroupsTable.Rows.Count; i++)
            {
                VariableCollection groupVariableCollection = template.CreateChild("pending_list");

                UserGroup thisGroup = new UserGroup(core, pendingGroupsTable.Rows[i]);

                groupVariableCollection.ParseVariables("GROUP_DISPLAY_NAME", HttpUtility.HtmlEncode(thisGroup.DisplayName));
                groupVariableCollection.ParseVariables("MEMBERS", HttpUtility.HtmlEncode(thisGroup.Members.ToString()));

                groupVariableCollection.ParseVariables("U_VIEW", HttpUtility.HtmlEncode(thisGroup.Uri));
                groupVariableCollection.ParseVariables("U_MEMBERLIST", HttpUtility.HtmlEncode(thisGroup.MemberlistUri));
                groupVariableCollection.ParseVariables("U_LEAVE", HttpUtility.HtmlEncode(thisGroup.LeaveUri));

                switch (thisGroup.GroupType)
                {
                    case "OPEN":
                        groupVariableCollection.ParseVariables("GROUP_TYPE", HttpUtility.HtmlEncode("Open"));
                        break;
                    case "CLOSED":
                        groupVariableCollection.ParseVariables("GROUP_TYPE", HttpUtility.HtmlEncode("Closed"));
                        break;
                    case "PRIVATE":
                        groupVariableCollection.ParseVariables("GROUP_TYPE", HttpUtility.HtmlEncode("Private"));
                        break;
                }
            }

            DataTable groupsTable = db.Query(string.Format("SELECT {1}, go.user_id as user_id_go FROM group_members gm INNER JOIN group_keys gk ON gm.group_id = gk.group_id INNER JOIN group_info gi ON gk.group_id = gi.group_id LEFT JOIN group_operators go ON gm.user_id = go.user_id AND gm.group_id = go.group_id WHERE gm.user_id = {0} AND gm.group_member_approved = 1",
                loggedInMember.UserId, UserGroup.GROUP_INFO_FIELDS));

            if (groupsTable.Rows.Count > 0)
            {
                template.ParseVariables("GROUP_MEMBERSHIPS", "TRUE");
            }

            for (int i = 0; i < groupsTable.Rows.Count; i++)
            {
                VariableCollection groupVariableCollection = template.CreateChild("group_list");

                UserGroup thisGroup = new UserGroup(core, groupsTable.Rows[i]);

                groupVariableCollection.ParseVariables("GROUP_DISPLAY_NAME", HttpUtility.HtmlEncode(thisGroup.DisplayName));
                groupVariableCollection.ParseVariables("MEMBERS", HttpUtility.HtmlEncode(thisGroup.Members.ToString()));

                groupVariableCollection.ParseVariables("U_VIEW", HttpUtility.HtmlEncode(thisGroup.Uri));
                groupVariableCollection.ParseVariables("U_MEMBERLIST", HttpUtility.HtmlEncode(thisGroup.MemberlistUri));
                if (!(groupsTable.Rows[i]["user_id_go"] is DBNull))
                {
                    if ((int)groupsTable.Rows[i]["user_id_go"] != loggedInMember.UserId)
                    {
                        groupVariableCollection.ParseVariables("U_LEAVE", HttpUtility.HtmlEncode(thisGroup.LeaveUri));
                    }
                }
                else
                {
                    groupVariableCollection.ParseVariables("U_LEAVE", HttpUtility.HtmlEncode(thisGroup.LeaveUri));
                }
                groupVariableCollection.ParseVariables("U_INVITE", HttpUtility.HtmlEncode(thisGroup.InviteUri));

                switch (thisGroup.GroupType)
                {
                    case "OPEN":
                        groupVariableCollection.ParseVariables("GROUP_TYPE", HttpUtility.HtmlEncode("Open"));
                        break;
                    case "CLOSED":
                        groupVariableCollection.ParseVariables("GROUP_TYPE", HttpUtility.HtmlEncode("Closed"));
                        break;
                    case "PRIVATE":
                        groupVariableCollection.ParseVariables("GROUP_TYPE", HttpUtility.HtmlEncode("Private"));
                        break;
                }
            }
        }

        private void JoinGroup(string submodule)
        {
            subModules.Add("join", null);
            if (submodule != "join") return;

            AuthoriseRequestSid();

            long groupId = 0;

            try
            {
                groupId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Error", "Unable to complete action, missing data. Go back and try again.");
                return;
            }

            try
            {
                UserGroup thisGroup = new UserGroup(core, groupId);
                int activated = 0;

                DataTable membershipTable = db.Query(string.Format("SELECT user_id FROM group_members WHERE group_id = {0} AND user_id = {1};",
                    thisGroup.GroupId, loggedInMember.UserId));

                if (membershipTable.Rows.Count > 0)
                {
                    SetRedirectUri(thisGroup.Uri);
                    Display.ShowMessage("Already a Member", "You are already a member of this group.");
                    return;
                }

                switch (thisGroup.GroupType)
                {
                    case "OPEN":
                    case "PRIVATE": // assume as you've been invited that it is enough for activation
                        activated = 1;
                        break;
                    case "CLOSED":
                        activated = 0;
                        break;
                }

                bool isInvited = thisGroup.IsGroupInvitee(loggedInMember);

                // do not need an invite unless the group is private
                // private groups you must be invited to
                if (thisGroup.GroupType != "PRIVATE" || (thisGroup.GroupType == "PRIVATE" && isInvited))
                {
                    db.BeginTransaction();
                    db.UpdateQuery(string.Format("INSERT INTO group_members (group_id, user_id, group_member_approved, group_member_ip, group_member_date_ut) VALUES ({0}, {1}, {2}, '{3}', UNIX_TIMESTAMP());",
                        thisGroup.GroupId, loggedInMember.UserId, activated, Mysql.Escape(session.IPAddress.ToString()), true));

                    if (activated == 1)
                    {
                        db.UpdateQuery(string.Format("UPDATE group_info SET group_members = group_members + 1 WHERE group_id = {0}",
                            thisGroup.GroupId));
                    }

                    // just do it anyway, can be invited to any type of group
                    db.UpdateQuery(string.Format("DELETE FROM group_invites WHERE group_id = {0} AND user_id = {1}",
                        thisGroup.GroupId, loggedInMember.UserId));

                    SetRedirectUri(thisGroup.Uri);
                    if (thisGroup.GroupType == "OPEN" || thisGroup.GroupType == "PRIVATE")
                    {
                        Display.ShowMessage("Joined Group", "You have joined this group.");
                    }
                    else if (thisGroup.GroupType == "CLOSED")
                    {
                        Display.ShowMessage("Joined Group", "You applied to join this group. A group operator must approve your membership before you will be admitted into the group.");
                    }
                    return;
                }
                else
                {
                    Display.ShowMessage("Cannot join group", "This group is private, you must be invited to be able to join it.");
                    return;
                }
            }
            catch
            {
                Display.ShowMessage("Group does not Exist", "The group you are trying to join does not exist.");
                return;
            }
        }

        private void LeaveGroup(string submodule)
        {
            subModules.Add("leave", null);
            if (submodule != "leave") return;

            AuthoriseRequestSid();

            long groupId = 0;

            try
            {
                groupId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Error", "Unable to complete action, missing data. Go back and try again.");
                return;
            }

            try
            {
                UserGroup thisGroup = new UserGroup(core, groupId);

                bool isGroupMemberPending = thisGroup.IsGroupMemberPending(loggedInMember);
                bool isGroupMember = thisGroup.IsGroupMember(loggedInMember);

                DataTable operatorsTable = db.Query(string.Format("SELECT user_id FROM group_operators WHERE group_id = {0} AND user_id = {1};",
                    thisGroup.GroupId, loggedInMember.UserId));

                if (operatorsTable.Rows.Count > 0)
                {
                    SetRedirectUri(thisGroup.Uri);
                    Display.ShowMessage("Cannot Leave Group", "You cannot leave this group while you are an operator of the group.");
                    return;
                }
                else
                {
                    if (isGroupMember)
                    {
                        db.BeginTransaction();
                        db.UpdateQuery(string.Format("DELETE FROM group_members WHERE group_id = {0} AND user_id = {1};",
                            thisGroup.GroupId, loggedInMember.UserId));

                        long officerRowsChanged = db.UpdateQuery(string.Format("DELETE FROM group_officers WHERE group_id = {0} AND user_id = {1};",
                            thisGroup.GroupId, loggedInMember.UserId));

                        db.UpdateQuery(string.Format("UPDATE group_info SET group_members = group_members - 1, group_officers = group_officers - {1} WHERE group_id = {0}",
                            thisGroup.GroupId, officerRowsChanged));

                        SetRedirectUri(thisGroup.Uri);
                        Display.ShowMessage("Left Group", "You have left the group.");
                        return;
                    }
                    else if (isGroupMemberPending)
                    {
                        db.UpdateQuery(string.Format("DELETE FROM group_members WHERE group_id = {0} AND user_id = {1};",
                            thisGroup.GroupId, loggedInMember.UserId));

                        SetRedirectUri(thisGroup.Uri);
                        Display.ShowMessage("Left Group", "You are no longer pending membership of the group.");
                        return;
                    }
                    else
                    {
                        SetRedirectUri(thisGroup.Uri);
                        Display.ShowMessage("Not a Member", "You cannot leave a group you are not a member of.");
                        return;
                    }
                }
             }
            catch (InvalidGroupException)
            {
                Display.ShowMessage("Group does not Exist", "The group you are trying to leave does not exist.");
                return;
            }
        }

        private void InviteGroup(string submodule)
        {
            subModules.Add("invite", null);
            if (submodule != "invite") return;

            if (Request.Form["send"] != null)
            {
                InviteGroupSend();
                return;
            }

            long groupId;

            template.SetTemplate("Groups", "account_group_invite");

            try
            {
                groupId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Error", "An error has occured, go back.");
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

            template.ParseVariables("S_FORM_ACTION", HttpUtility.HtmlEncode(Linker.AppendSid("/account/", true)));
            template.ParseVariables("S_ID", HttpUtility.HtmlEncode(groupId.ToString()));
        }

        private void InviteGroupSend()
        {
            AuthoriseRequestSid();

            long groupId;
            string username;

            try
            {
                groupId = long.Parse(Request.Form["id"]);
                username = Request.Form["username"];
            }
            catch
            {
                Display.ShowMessage("Error", "An error has occured, go back.");
                return;
            }

            try
            {
                UserGroup thisGroup = new UserGroup(core, groupId);

                try
                {
                    Member inviteMember = new Member(core, username);

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

                            emailTemplate.ParseVariables("TO_NAME", inviteMember.DisplayName);
                            emailTemplate.ParseVariables("FROM_NAME", loggedInMember.DisplayName);
                            emailTemplate.ParseVariables("FROM_USERNAME", loggedInMember.UserName);
                            emailTemplate.ParseVariables("GROUP_NAME", thisGroup.DisplayName);
                            emailTemplate.ParseVariables("U_GROUP", "http://zinzam.com" + "/group/" + thisGroup.Slug);
                            emailTemplate.ParseVariables("U_JOIN", "http://zinzam.com" + Linker.StripSid(thisGroup.JoinUri));

                            ApplicationEntry ae = Application.GetExecutingApplication(core, loggedInMember);
                            ae.SendNotification(inviteMember, string.Format("[user]{0}[/user] invited you to join a group.", core.LoggedInMemberId), "{TODO}" ,emailTemplate);

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

        public void GroupMakeOperator(string submodule)
        {
            subModules.Add("make-operator", null);
            if (submodule != "make-operator") return;

            AuthoriseRequestSid();

            long groupId;
            long userId;

            try
            {
                string[] idString = Request.QueryString["id"].Split(new char[] { ',' });
                groupId = long.Parse(idString[0]);
                userId = long.Parse(idString[1]);
            }
            catch
            {
                Display.ShowMessage("Error", "An error has occured, go back.");
                return;
            }

            try
            {
                UserGroup thisGroup = new UserGroup(core, groupId);

                if (thisGroup.IsGroupOperator(loggedInMember))
                {
                    try
                    {
                        Member member = new Member(core, userId);
                        if (!thisGroup.IsGroupOperator(member))
                        {
                            db.BeginTransaction();
                            db.UpdateQuery(string.Format("INSERT INTO group_operators (group_id, user_id) VALUES ({0}, {1});",
                                thisGroup.GroupId, userId));

                            db.UpdateQuery(string.Format("UPDATE group_info SET group_operators = group_operators + 1 WHERE group_id = {0}",
                                thisGroup.GroupId));

                            SetRedirectUri(thisGroup.Uri);
                            Display.ShowMessage("Operator Appointed to Group", "You have successfully appointed an operator to the group.");
                        }
                        else
                        {
                            Display.ShowMessage("Already an Officer", "This member is already an officer.");
                            return;
                        }
                    }
                    catch
                    {
                        Functions.ThrowError();
                        return;
                    }
                }
                else
                {
                    Display.ShowMessage("Unauthorised", "You must be the group operator to appoint an operator.");
                    return;
                }
            }
            catch
            {
                Functions.ThrowError();
                return;
            }
        }

        public void GroupMakeOfficer(string submodule)
        {
            subModules.Add("make-officer", null);
            if (submodule != "make-officer") return;

            if (Request.Form["save"] != null)
            {
                GroupMakeOfficerSave();
                return;
            }

            template.SetTemplate("Groups", "account_group_appoint_officer");

            AuthoriseRequestSid();

            long groupId;
            long userId;

            try
            {
                string[] idString = Request.QueryString["id"].Split(new char[] { ',' });
                groupId = long.Parse(idString[0]);
                userId = long.Parse(idString[1]);
            }
            catch
            {
                Display.ShowMessage("Error", "An error has occured, go back.");
                return;
            }

            try
            {
                UserGroup thisGroup = new UserGroup(core, groupId);

                if (thisGroup.IsGroupOperator(loggedInMember))
                {
                    try
                    {
                        Member member = new Member(core, userId);

                        if (thisGroup.IsGroupMember(member))
                        {
                            // all ok, don't really need to do much, so let's do it
                            template.ParseVariables("S_ID", HttpUtility.HtmlEncode(string.Format("{0},{1}", groupId, userId)));
                            template.ParseVariables("S_FORM_ACTION", HttpUtility.HtmlEncode(Linker.AppendSid("/account/", true)));
                            template.ParseVariables("S_USERNAME", HttpUtility.HtmlEncode(member.UserName));
                        }
                        else
                        {
                            Functions.ThrowError();
                            return;
                        }
                    }
                    catch
                    {
                        Functions.ThrowError();
                        return;
                    }
                }
                else
                {
                    Display.ShowMessage("Unauthorised", "You must be the group operator to appoint an operator.");
                    return;
                }
            }
            catch
            {
                Functions.ThrowError();
                return;
            }
        }

        public void GroupMakeOfficerSave()
        {
            AuthoriseRequestSid();

            long groupId = 0;
            long userId = 0;
            string title;

            try
            {
                string[] idString = Request.Form["id"].Split(new char[] { ',' });
                groupId = long.Parse(idString[0]);
                userId = long.Parse(idString[1]);
                title = Request.Form["title"];
            }
            catch
            {
                Functions.ThrowError();
                return;
            }

            if (string.IsNullOrEmpty(title))
            {
                Display.ShowMessage("Officer Title Empty", "The officer title must not be empty, go back and enter an officer title.");
                return;
            }
            else
            {
                if (title.Length < 4)
                {
                    Display.ShowMessage("Officer Title Too Short", "The officer title must be at least four characters, go back and enter an officer title.");
                    return;
                }
                else if (title.Length > 24)
                {
                    Display.ShowMessage("Officer Title Too Long", "The officer title must be at most twenty four characters, go back and enter an officer title.");
                    return;
                }
            }

            try
            {
                UserGroup thisGroup = new UserGroup(core, groupId);

                if (thisGroup.IsGroupOperator(loggedInMember))
                {
                    try
                    {
                        Member member = new Member(core, userId);

                        if (thisGroup.IsGroupMember(member))
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
                                Display.ShowMessage("Officer Appointed to Group", "You have successfully appointed an officer to the group.");
                            }
                            else
                            {
                                Display.ShowMessage("Already Officer", "This member is already appointed as this officer.");
                                return;
                            }
                        }
                        else
                        {
                            Functions.ThrowError();
                            return;
                        }
                    }
                    catch
                    {
                        Functions.ThrowError();
                        return;
                    }
                }
                else
                {
                    Display.ShowMessage("Unauthorised", "You must be the group operator to appoint an officer.");
                    return;
                }
            }
            catch
            {
                Functions.ThrowError();
                return;
            }
        }

        public void GroupRemoveOfficer(string submodule)
        {
            subModules.Add("remove-officer", null);
            if (submodule != "remove-officer") return;

            AuthoriseRequestSid();

            long groupId;
            long userId;
            string title;

            try
            {
                string[] idString = Request.QueryString["id"].Split(new char[] { ',' });
                groupId = long.Parse(idString[0]);
                userId = long.Parse(idString[1]);
                title = UTF8Encoding.UTF8.GetString(Convert.FromBase64String(idString[2]));
            }
            catch
            {
                Functions.ThrowError();
                return;
            }

            try
            {
                UserGroup thisGroup = new UserGroup(core, groupId);

                if (thisGroup.IsGroupOperator(loggedInMember))
                {
                    db.BeginTransaction();
                    long deletedRows = db.UpdateQuery(string.Format("DELETE FROM group_officers WHERE group_id = {0} AND user_id = {1} AND officer_title = '{2}'",
                        groupId, userId, Mysql.Escape(title)));

                    if (deletedRows >= 0)
                    {
                        db.UpdateQuery(string.Format("UPDATE group_info SET group_officers = group_officers - {1} WHERE group_id = {0}",
                            thisGroup.GroupId, deletedRows));

                        SetRedirectUri(thisGroup.Uri);
                        Display.ShowMessage("Officer Removed from Group", "You have successfully removed an officer from the group.");
                    }
                    else
                    {
                        Display.ShowMessage("Error", "Could not delete officer, they may have already been delted.");
                        return;
                    }
                }
            }
            catch
            {
                Functions.ThrowError();
                return;
            }
        }

        public void GroupResignOperator(string submodule)
        {
            subModules.Add("resign-operator", null);
            if (submodule != "resign-operator") return;

            if (Display.GetConfirmBoxResult() != ConfirmBoxResult.None)
            {
                GroupResignOperatorSave();
                return;
            }

            AuthoriseRequestSid();

            long groupId;

            try
            {
                groupId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Functions.ThrowError();
                return;
            }

            Dictionary<string, string> hiddenFieldList = new Dictionary<string, string>();
            hiddenFieldList.Add("module", "groups");
            hiddenFieldList.Add("sub", "resign-operator");
            hiddenFieldList.Add("id", groupId.ToString());

            Display.ShowConfirmBox(HttpUtility.HtmlEncode(Linker.AppendSid("/account/", true)),
                "Are you sure you want to resign as operator from this group?",
                "When you resign as operator from this group, you can only become operator again if appointed by another operator. Once you confirm resignation it is final.",
                hiddenFieldList);
        }

        public void GroupResignOperatorSave()
        {
            long groupId;

            try
            {
                groupId = long.Parse(Request.Form["id"]);
            }
            catch
            {
                Functions.ThrowError();
                return;
            }

            AuthoriseRequestSid();

            UserGroup thisGroup = new UserGroup(core, groupId);

            if (Request.Form["1"] != null)
            {
                if (thisGroup.IsGroupOperator(loggedInMember))
                {
                    if (thisGroup.Operators > 1)
                    {
                        db.BeginTransaction();
                        long deletedRows = db.UpdateQuery(string.Format("DELETE FROM group_operators WHERE group_id = {0} AND user_id = {1}",
                            thisGroup.GroupId, loggedInMember.UserId));

                        db.UpdateQuery(string.Format("UPDATE group_info SET group_operators = group_operators - {1} WHERE group_id = {0}",
                            thisGroup.GroupId, deletedRows));

                        SetRedirectUri(thisGroup.Uri);
                        Display.ShowMessage("Success", "You successfully resigned as a group operator. You are still a member of the group. You will be redirected in a second.");
                    }
                    else
                    {
                        Display.ShowMessage("Cannot resign as operator", "Groups must have at least one operator, you cannot resign from this group at this moment.");
                        return;
                    }
                }
                else
                {
                    Display.ShowMessage("Error", "An error has occured. You are not an operator of this group, go back.");
                    return;
                }
            }
            else if (Request.Form["0"] != null)
            {
                SetRedirectUri(thisGroup.Uri);
                Display.ShowMessage("Cancelled", "You cancelled resignation from being a group operator.");
            }
        }

        public void GroupApproveMember(string submodule)
        {
            subModules.Add("approve", null);
            if (submodule != "approve") return;

            AuthoriseRequestSid();

            long groupId;
            long userId;

            try
            {
                string[] idString = Request.QueryString["id"].Split(new char[] { ',' });
                groupId = long.Parse(idString[0]);
                userId = long.Parse(idString[1]);
            }
            catch
            {
                Functions.ThrowError();
                return;
            }

            try
            {
                UserGroup thisGroup = new UserGroup(core, groupId);

                if (thisGroup.IsGroupOperator(loggedInMember))
                {
                    try
                    {
                        Member member = new Member(core, userId);

                        if (thisGroup.IsGroupMemberPending(member))
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
                                Display.ShowMessage("Membership Approved", "You have approved the membership for the user.");
                                return;
                            }
                            else
                            {
                                Display.ShowMessage("Not Pending", "This member is not pending membership. They may have cancelled their request, or been approved by another operator.");
                                return;
                            }
                        }
                        else
                        {
                            Display.ShowMessage("Not Pending", "This member is not pending membership. They may have cancelled their request, or been approved by another operator.");
                            return;
                        }
                    }
                    catch
                    {
                        Display.ShowMessage("Error", "An error has occured, group member does not exist, go back.");
                        return;
                    }
                }
                else
                {
                    Display.ShowMessage("Not Group Operator", "You must be an operator of the group to approve new memberships.");
                    return;
                }
            }
            catch
            {
                Display.ShowMessage("Error", "An error has occured, group does not exist, go back.");
                return;
            }
        }

        public void GroupBanMember(string submodule)
        {
            subModules.Add("ban-member", null);
            if (submodule != "ban-member") return;

            AuthoriseRequestSid();

            if (Display.GetConfirmBoxResult() != ConfirmBoxResult.None)
            {
                GroupBanMemberSave();
                return;
            }

            long groupId;
            long userId;

            try
            {
                string[] idString = Request.QueryString["id"].Split(new char[] { ',' });
                groupId = long.Parse(idString[0]);
                userId = long.Parse(idString[1]);
            }
            catch
            {
                Functions.ThrowError();
                return;
            }

            Dictionary<string, string> hiddenFieldList = new Dictionary<string, string>();
            hiddenFieldList.Add("module", "groups");
            hiddenFieldList.Add("sub", "ban-member");
            hiddenFieldList.Add("id", string.Format("{0},{1}", groupId, userId));

            Display.ShowConfirmBox(HttpUtility.HtmlEncode(Linker.AppendSid("/account/", true)),
                "Are you sure you want to ban this member?",
                "Banning a member from the group prevents them from seeing, or participating in the group.",
                hiddenFieldList);
        }

        public void GroupBanMemberSave()
        {
            long groupId;
            long userId;

            try
            {
                string[] idString = Request.Form["id"].Split(new char[] { ',' });
                groupId = long.Parse(idString[0]);
                userId = long.Parse(idString[1]);
            }
            catch
            {
                Functions.ThrowError();
                return;
            }

            if (Request.Form["1"] != null)
            {
                try
                {
                    UserGroup group = new UserGroup(core, groupId);

                    if (group.IsGroupOperator(loggedInMember))
                    {
                        try
                        {
                            GroupMember member = new GroupMember(core, group, userId);

                            member.Ban();

                            Display.ShowMessage("Member Banned", "The member has been banned from the group.");
                            return;
                        }
                        catch (InvalidUserException)
                        {
                            Functions.ThrowError();
                            return;
                        }
                    }
                    else
                    {
                        Display.ShowMessage("Cannot ban member", "Only group operators can ban members from groups.");
                        return;
                    }
                }
                catch (InvalidGroupException)
                {
                    Functions.ThrowError();
                    return;
                }
            }
            else if (Request.Form["0"] != null)
            {
                Display.ShowMessage("Cancelled", "You cancelled the banning of this member.");
                return;
            }
        }
    }
}
