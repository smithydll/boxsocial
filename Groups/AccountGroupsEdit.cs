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
    [AccountSubModule("groups", "edit")]
    public class AccountGroupsEdit : AccountSubModule
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

        public AccountGroupsEdit()
        {
            this.Load += new EventHandler(AccountGroupsEdit_Load);
            this.Show += new EventHandler(AccountGroupsEdit_Show);
        }

        void AccountGroupsEdit_Load(object sender, EventArgs e)
        {
        }

        void AccountGroupsEdit_Show(object sender, EventArgs e)
        {
            SetTemplate("account_group_edit");

            long groupId = Functions.RequestLong("id", 0);

            if (groupId == 0)
            {
                DisplayGenericError();
                return;
            }

            UserGroup thisGroup = new UserGroup(core, groupId);

            if (!thisGroup.IsGroupOperator(LoggedInMember))
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

            template.Parse("S_EDIT_GROUP", Linker.AppendSid("/account/", true));
            Display.ParseSelectBox(template, "S_CATEGORIES", "category", categories, category.ToString());
            template.Parse("S_GROUP_ID", thisGroup.GroupId.ToString());
            template.Parse("GROUP_DISPLAY_NAME", thisGroup.DisplayName);
            template.Parse("GROUP_DESCRIPTION", thisGroup.Description);

            string selected = "checked=\"checked\" ";
            switch (thisGroup.GroupType)
            {
                case "OPEN":
                    template.Parse("S_OPEN_CHECKED", selected);
                    break;
                case "CLOSED":
                    template.Parse("S_CLOSED_CHECKED", selected);
                    break;
                case "PRIVATE":
                    template.Parse("S_PRIVATE_CHECKED", selected);
                    break;
            }

            Save(new EventHandler(AccountGroupsEdit_Save));
        }

        void AccountGroupsEdit_Save(object sender, EventArgs e)
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

            if (!thisGroup.IsGroupOperator(LoggedInMember))
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
    }
}
