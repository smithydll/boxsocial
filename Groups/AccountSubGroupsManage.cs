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
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Groups
{
    [AccountSubModule(AppPrimitives.Group, "groups", "subgroups")]
    public class AccountSubGroupsManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Manage Sub Groups";
            }
        }

        public override int Order
        {
            get
            {
                return 5;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountSubGroupsManage class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountSubGroupsManage(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountSubGroupsManage_Load);
            this.Show += new EventHandler(AccountSubGroupsManage_Show);
        }

        void AccountSubGroupsManage_Load(object sender, EventArgs e)
        {
            AddModeHandler("create", new ModuleModeHandler(AccountSubGroupsManage_Create));
            AddModeHandler("edit", new ModuleModeHandler(AccountSubGroupsManage_Create));
            AddModeHandler("members", new ModuleModeHandler(AccountSubGroupsManage_Members));
        }

        void AccountSubGroupsManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_group_subgroup_manage");

            if (Owner is UserGroup)
            {
                UserGroup group = (UserGroup)Owner;

                List<SubUserGroup> subGroups = group.GetSubGroups();

                foreach (SubUserGroup subGroup in subGroups)
                {
                    VariableCollection subGroupVariableCollection = template.CreateChild("group_list");

                    subGroupVariableCollection.Parse("DISPLAY_NAME", subGroup.DisplayName);
                    subGroupVariableCollection.Parse("ID", subGroup.Id);
                    subGroupVariableCollection.Parse("MEMBERS", subGroup.MemberCount);

                    subGroupVariableCollection.Parse("U_SUBGROUP", subGroup.Uri);
                    subGroupVariableCollection.Parse("U_MEMBERS", subGroup.EditMembersUri);
                    subGroupVariableCollection.Parse("U_EDIT", subGroup.EditUri);
                    subGroupVariableCollection.Parse("U_DELETE", subGroup.DeleteUri);
                }

                template.Parse("U_CREATE_USER_GROUP", BuildUri("subgroups", "create"));
            }
            else
            {
                core.Functions.Generate404();
                return;
            }
        }

        void AccountSubGroupsManage_Members(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_group_subgroup_members");

            long id = core.Functions.RequestLong("id", 0);

            if (id <= 0)
            {
                return;
            }

            try
            {
                subUserGroup = new SubUserGroup(core, id);
            }
            catch (InvalidSubGroupException)
            {
                return;
            }

            if (subUserGroup.CanEditItem())
            {
                List<SubGroupMember> leaders = subUserGroup.GetLeaders();
                List<SubGroupMember> awaiting = subUserGroup.GetMembersWaitingApproval();
                List<SubGroupMember> members = subUserGroup.GetMembers(core.TopLevelPageNumber, 25);

                UserSelectBox newUserSelectBox = new UserSelectBox(core, "usernames");
                newUserSelectBox.SelectMultiple = false;
                //Form.AddFormField(newUserSelectBox);

                YesNoList makeLeaderYesNoList = new YesNoList(core, "make-leader");
                makeLeaderYesNoList.SelectedKey = "no";
                //Form.AddFormField(makeLeaderYesNoList);

                YesNoList makeDefaultYesNoList = new YesNoList(core, "make-default");
                makeDefaultYesNoList.SelectedKey = "no";
                //Form.AddFormField(makeDefaultYesNoList);

                template.Parse("SUBGROUP_DISPLAY_NAME", subUserGroup.DisplayName);
                template.Parse("S_USERNAMES", newUserSelectBox);
                template.Parse("S_MAKE_LEADER", makeLeaderYesNoList);
                template.Parse("S_MAKE_DEFAULT_GROUP", makeDefaultYesNoList);
                template.Parse("S_GROUP_ID", subUserGroup.Id.ToString());

                foreach (SubGroupMember member in leaders)
                {
                    CheckBox memberCheckBox = new CheckBox("check[" + member.Id.ToString() + "]");

                    VariableCollection memberVariableCollection = template.CreateChild("leader_list");
                    memberVariableCollection.Parse("DISPLAY_NAME", member.DisplayName);
                    memberVariableCollection.Parse("JOINED_DATE", core.Tz.DateTimeToDateString(member.GetJoinedDate(core.Tz)));
                    memberVariableCollection.Parse("S_MARK", memberCheckBox);

                    if (member.IsDefaultGroup)
                    {
                        memberVariableCollection.Parse("DEFAULT_GROUP", core.Prose.GetString("YES"));
                    }
                    else
                    {
                        memberVariableCollection.Parse("DEFAULT_GROUP", core.Prose.GetString("NO"));
                    }
                }

                foreach (SubGroupMember member in awaiting)
                {
                    CheckBox memberCheckBox = new CheckBox("check[" + member.Id.ToString() + "]");

                    VariableCollection memberVariableCollection = template.CreateChild("awaiting_list");
                    memberVariableCollection.Parse("DISPLAY_NAME", member.DisplayName);
                    memberVariableCollection.Parse("JOINED_DATE", core.Tz.DateTimeToDateString(member.GetJoinedDate(core.Tz)));
                    memberVariableCollection.Parse("S_MARK", memberCheckBox);

                    if (member.IsDefaultGroup)
                    {
                        memberVariableCollection.Parse("DEFAULT_GROUP", core.Prose.GetString("YES"));
                    }
                    else
                    {
                        memberVariableCollection.Parse("DEFAULT_GROUP", core.Prose.GetString("NO"));
                    }
                }

                foreach (SubGroupMember member in members)
                {
                    CheckBox memberCheckBox = new CheckBox("check[" + member.Id.ToString() + "]");

                    VariableCollection memberVariableCollection = template.CreateChild("member_list");
                    memberVariableCollection.Parse("DISPLAY_NAME", member.DisplayName);
                    memberVariableCollection.Parse("JOINED_DATE", core.Tz.DateTimeToDateString(member.GetJoinedDate(core.Tz)));
                    memberVariableCollection.Parse("S_MARK", memberCheckBox);

                    if (member.IsDefaultGroup)
                    {
                        memberVariableCollection.Parse("DEFAULT_GROUP", core.Prose.GetString("YES"));
                    }
                    else
                    {
                        memberVariableCollection.Parse("DEFAULT_GROUP", core.Prose.GetString("NO"));
                    }
                }
                
            }
            else
            {
                return;
            }

            Save(AccountSubGroupsManage_Members_AddNew);
        }

        void AccountSubGroupsManage_Members_AddNew(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            List<long> userIds = UserSelectBox.FormUsers(core, "usernames");
            core.LoadUserProfiles(userIds);

            foreach (long id in userIds)
            {
                if (subUserGroup.AddMember(core.PrimitiveCache[id], true, YesNoList.FormBool(core, "make-leader"), YesNoList.FormBool(core, "make-default")))
                {

                    if (YesNoList.FormBool(core, "make-leader"))
                    {
                        VariableCollection memberVariableCollection = template.CreateChild("leader_list");
                        memberVariableCollection.Parse("DISPLAY_NAME", core.PrimitiveCache[id].DisplayName);
                    }
                    else
                    {
                        VariableCollection memberVariableCollection = template.CreateChild("member_list");
                        memberVariableCollection.Parse("DISPLAY_NAME", core.PrimitiveCache[id].DisplayName);
                    }
                }
            }
        }

        SubUserGroup subUserGroup = null;
        void AccountSubGroupsManage_Create(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_group_subgroup_create");

            TextBox titleTextBox = new TextBox("title");
            titleTextBox.MaxLength = 64;
            titleTextBox.Script.OnChange = "UpdateSlug()";
            Form.AddFormField(titleTextBox);

            TextBox slugTextBox = new TextBox("slug");
            slugTextBox.MaxLength = 64;
            Form.AddFormField(slugTextBox);

            TextBox descriptionTextBox = new TextBox("description");
            descriptionTextBox.IsFormatted = true;
            descriptionTextBox.Lines = 4;
            Form.AddFormField(descriptionTextBox);

            RadioList groupTypeRadioList = new RadioList("group-type");

            groupTypeRadioList.Add(new RadioListItem(groupTypeRadioList.Name, "open", "Open Group"));
            groupTypeRadioList.Add(new RadioListItem(groupTypeRadioList.Name, "request", "Request Group"));
            groupTypeRadioList.Add(new RadioListItem(groupTypeRadioList.Name, "closed", "Closed Group"));
            groupTypeRadioList.Add(new RadioListItem(groupTypeRadioList.Name, "private", "Private Group"));
            Form.AddFormField(groupTypeRadioList);

            switch (e.Mode)
            {
                case "create":
                    break;
                case "edit":
                    long id = core.Functions.FormLong("id", core.Functions.RequestLong("id", 0));

                    if (id > 0)
                    {
                        try
                        {
                            subUserGroup = new SubUserGroup(core, id);
                        }
                        catch (InvalidSubGroupException)
                        {
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }

                    if (subUserGroup.Parent.Owner.Id != Owner.Id)
                    {
                        core.Functions.Generate403();
                        return;
                    }

                    if (!Form.IsFormSubmission)
                    {
                        titleTextBox.Script.OnChange = string.Empty;
                        titleTextBox.Value = subUserGroup.DisplayName;
                        descriptionTextBox.Value = subUserGroup.Description;

                        switch (subUserGroup.SubGroupType)
                        {
                            case "OPEN":
                                groupTypeRadioList.SelectedKey = "open";
                                break;
                            case "REQUEST":
                                groupTypeRadioList.SelectedKey = "request";
                                break;
                            case "CLOSED":
                                groupTypeRadioList.SelectedKey = "closed";
                                break;
                            case "PRIVATE":
                                groupTypeRadioList.SelectedKey = "private";
                                break;
                        }
                    }

                    slugTextBox.Value = subUserGroup.Key;
                    slugTextBox.IsDisabled = true;

                    template.Parse("S_GROUP_ID", subUserGroup.Id.ToString());
                    template.Parse("EDIT", "TRUE");
                    break;
            }

            template.Parse("S_TITLE", titleTextBox);
            template.Parse("S_SLUG", slugTextBox);
            template.Parse("S_DESCRIPTION", descriptionTextBox);

            template.Parse("S_TYPE_OPEN", groupTypeRadioList["open"]);
            template.Parse("S_TYPE_REQUEST", groupTypeRadioList["request"]);
            template.Parse("S_TYPE_CLOSED", groupTypeRadioList["closed"]);
            template.Parse("S_TYPE_PRIVATE", groupTypeRadioList["private"]);

            SaveMode(new ModuleModeHandler(AccountSubGroupsManage_Save));
        }

        void AccountSubGroupsManage_Save(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            switch (e.Mode)
            {
                case "create":
                    try
                    {
                        subUserGroup = SubUserGroup.Create(core, (UserGroup)Owner, core.Http.Form["title"], core.Http.Form["slug"], core.Http.Form["description"], core.Http.Form["group-type"]);

                        SetRedirectUri(BuildUri());
                        core.Display.ShowMessage("Group Created", "The group has been created");
                    }
                    catch (FieldTooLongException ex)
                    {
                        switch (ex.FieldName)
                        {
                            case "sub_group_name":
                            case "sub_group_name_display":
                                SetError("The group name is too long, please choose another");
                                break;
                            case "sub_group_abstract":
                                SetError("The group description is too long, please use a shorter description");
                                break;
                            default:
                                // Generic error occured
                                core.Functions.ThrowError();
                                break;
                        }
                    }
                    catch (GroupNameNotUniqueException)
                    {
                        SetError("A group with the same name already exists");
                    }
                    catch (Exception ex)
                    {
                        DisplayError(ex.ToString());
                    }
                    break;
                case "edit":

                    string groupType = core.Http.Form["group-type"];
                    switch (groupType.ToLower())
                    {
                        case "open":
                            groupType = "OPEN";
                            break;
                        case "request":
                            groupType = "REQUEST";
                            break;
                        case "closed":
                            groupType = "CLOSED";
                            break;
                        case "private":
                            groupType = "PRIVATE";
                            break;
                        default:
                            return;
                    }

                    subUserGroup.Title = core.Http.Form["title"];
                    subUserGroup.Description = core.Http.Form["description"];
                    subUserGroup.SubGroupType = groupType;

                    subUserGroup.Update();

                    break;
            }
        }
    }
}
