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
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Groups
{
    [AccountSubModule(AppPrimitives.Group, "groups", "subgroups", true)]
    public class AccountSubGroupsManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Manage Groups";
            }
        }

        public override int Order
        {
            get
            {
                return 2;
            }
        }

        public AccountSubGroupsManage()
        {
            this.Load += new EventHandler(AccountSubGroupsManage_Load);
            this.Show += new EventHandler(AccountSubGroupsManage_Show);
        }

        void AccountSubGroupsManage_Load(object sender, EventArgs e)
        {
            AddModeHandler("create", new ModuleModeHandler(AccountSubGroupsManage_Create));
            //AddSaveHandler("create", new EventHandler(AccountSubGroupsManage_Create_Save));
        }

        void AccountSubGroupsManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_group_subgroup_manage.html");

            if (Owner is UserGroup)
            {
                UserGroup group = (UserGroup)Owner;

                List<SubUserGroup> subGroups = group.GetSubGroups();

                foreach (SubUserGroup subGroup in subGroups)
                {
                    VariableCollection subGroupVariableCollection = template.CreateChild("group_list");

                    subGroupVariableCollection.Parse("DISPLAY_NAME", subGroup.DisplayName);
                    subGroupVariableCollection.Parse("ID", subGroup.Id);

                    //subGroupVariableCollection.Parse("U_EDIT", subGroup.EditUri);
                }

                template.Parse("U_CREATE_USER_GROUP", BuildUri("subgroups", "create"));
            }
            else
            {
                core.Functions.Generate404();
                return;
            }
        }

        void AccountSubGroupsManage_Create(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_group_subgroup_create.html");

            Save(AccountSubGroupsManage_Create_Save);

            TextBox titleTextBox = new TextBox("title");
            titleTextBox.MaxLength = 64;
            titleTextBox.Script.OnKeyDown = "";

            TextBox slugTextBox = new TextBox("slug");
            slugTextBox.MaxLength = 64;

            TextBox descriptionTextBox = new TextBox("description");
            descriptionTextBox.IsFormatted = true;
            descriptionTextBox.Lines = 4;

            RadioList groupTypeRadioList = new RadioList("group-type");

            //groupTypeRadioList.Add(new RadioListItem(
        }

        void AccountSubGroupsManage_Create_Save(object sender, EventArgs e)
        {
            try
            {
                SubUserGroup.Create(core, (UserGroup)Owner, core.Http.Form["title"], core.Http.Form["slug"], core.Http.Form["description"], core.Http.Form["group-type"]);
            }
            catch (FieldTooLongException ex)
            {
                switch (ex.FieldName)
                {
                    case "sub_group_name":
                    case "sub_group_name_display":
                        DisplayError("The group name is too long, please choose another");
                        break;
                    case "sub_group_abstract":
                        DisplayError("The group description is too long, please use a shorter description");
                        break;
                    default:
                        // Generic error occured
                        core.Functions.ThrowError();
                        break;
                }
            }
            catch
            {
                DisplayError("A group with the same name already exists");
            }
        }
    }
}
