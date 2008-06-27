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
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Groups
{
    [AccountModule("groups")]
    public class AccountGroups : AccountModule
    {
        public AccountGroups(Account account)
            : base(account)
        {
            //RegisterSubModule += new RegisterSubModuleHandler(ManageGroups);
            //RegisterSubModule += new RegisterSubModuleHandler(ManageGroupMemberships);
            //RegisterSubModule += new RegisterSubModuleHandler(DeleteGroup);
            //RegisterSubModule += new RegisterSubModuleHandler(EditGroup);
            //RegisterSubModule += new RegisterSubModuleHandler(JoinGroup);
            //RegisterSubModule += new RegisterSubModuleHandler(LeaveGroup);
            //RegisterSubModule += new RegisterSubModuleHandler(InviteGroup);
            //RegisterSubModule += new RegisterSubModuleHandler(GroupMakeOfficer);
            //RegisterSubModule += new RegisterSubModuleHandler(GroupMakeOperator);
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

        public override int Order
        {
            get
            {
                return 7;
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
                        User member = new User(core, userId);

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
