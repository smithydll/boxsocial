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
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Groups
{
    /// <summary>
    /// 
    /// </summary>
    [AccountSubModule(AppPrimitives.Group, "groups", "edit", true)]
    public class AccountGroupsEdit : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Group Preferences";
            }
        }

        public override int Order
        {
            get
            {
                return 1;
            }
        }

        public AccountGroupsEdit(Core core)
            : base(core)
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

            if (Owner.GetType() != typeof(UserGroup))
            {
                DisplayGenericError();
                return;
            }

            UserGroup thisGroup = (UserGroup)Owner;

            if (!thisGroup.IsGroupOperator(LoggedInMember))
            {
                core.Display.ShowMessage("Cannot Edit Group", "You must be an operator of the group to edit it.");
                return;
            }

            short category = thisGroup.RawCategory;

            SelectBox categoriesSelectBox = new SelectBox("category");
            DataTable categoriesTable = db.Query("SELECT category_id, category_title FROM global_categories ORDER BY category_title ASC;");
            foreach (DataRow categoryRow in categoriesTable.Rows)
            {
                categoriesSelectBox.Add(new SelectBoxItem(((long)categoryRow["category_id"]).ToString(), (string)categoryRow["category_title"]));
            }

            categoriesSelectBox.SelectedKey = category.ToString();
            template.Parse("S_CATEGORIES", categoriesSelectBox);
            template.Parse("S_GROUP_ID", thisGroup.GroupId.ToString());
            template.Parse("GROUP_DISPLAY_NAME", thisGroup.DisplayName);
            template.Parse("GROUP_DESCRIPTION", thisGroup.Description);

            string selected = "checked=\"checked\" ";
            switch (thisGroup.GroupType)
            {
                case "OPEN":
                    template.Parse("S_OPEN_CHECKED", selected);
                    break;
                case "REQUEST":
                    template.Parse("S_REQUEST_CHECKED", selected);
                    break;
                case "CLOSED":
                    template.Parse("S_CLOSED_CHECKED", selected);
                    break;
                case "PRIVATE":
                    template.Parse("S_PRIVATE_CHECKED", selected);
                    break;
            }

            DataTable pagesTable = db.Query(string.Format("SELECT page_id, page_slug, page_parent_path FROM user_pages WHERE page_item_id = {0} AND page_item_type_id = {1} ORDER BY page_order ASC;",
                thisGroup.Id, thisGroup.TypeId));

            SelectBox pagesSelectBox = new SelectBox("homepage");
            Dictionary<string, string> pages = new Dictionary<string, string>();
            List<string> disabledItems = new List<string>();
            pages.Add("/profile", "Group Profile");

            foreach (DataRow pageRow in pagesTable.Rows)
            {
                if (string.IsNullOrEmpty((string)pageRow["page_parent_path"]))
                {
                    pagesSelectBox.Add(new SelectBoxItem("/" + (string)pageRow["page_slug"], "/" + (string)pageRow["page_slug"] + "/"));
                }
                else
                {
                    pagesSelectBox.Add(new SelectBoxItem("/" + (string)pageRow["page_parent_path"] + "/" + (string)pageRow["page_slug"], "/" + (string)pageRow["page_parent_path"] + "/" + (string)pageRow["page_slug"] + "/"));
                }
            }

            pagesSelectBox.SelectedKey = thisGroup.Info.GroupHomepage.ToString();
            template.Parse("S_HOMEPAGE", pagesSelectBox);

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
            string homepage = "/profile";

            try
            {
                groupId = long.Parse(core.Http.Form["id"]);
                category = short.Parse(core.Http.Form["category"]);
                title = core.Http.Form["title"];
                description = core.Http.Form["description"];
                type = core.Http.Form["type"];
                homepage = core.Http.Form["homepage"];
            }
            catch
            {
                core.Display.ShowMessage("Error", "An error has occured, go back.");
                return;
            }

            switch (type)
            {
                case "open":
                    type = "OPEN";
                    break;
                case "request":
                    type = "REQUEST";
                    break;
                case "closed":
                    type = "CLOSED";
                    break;
                case "private":
                    type = "PRIVATE";
                    break;
                default:
                    core.Display.ShowMessage("Error", "An error has occured, go back.");
                    return;
            }

            UserGroup thisGroup = new UserGroup(core, groupId);

            if (!thisGroup.IsGroupOperator(LoggedInMember))
            {
                core.Display.ShowMessage("Cannot Edit Group", "You must be an operator of the group to edit it.");
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

                if (homepage != "/profile" && homepage != "/blog")
                {
                    try
                    {
                        Page thisPage = new Page(core, thisGroup, homepage.TrimStart(new char[] { '/' }));
                    }
                    catch (PageNotFoundException)
                    {
                        homepage = "/profile";
                    }
                }

                // save the edits to the group
                db.UpdateQuery(string.Format("UPDATE group_info SET group_name_display = '{1}', group_category = {2}, group_abstract = '{3}', group_type = '{4}', group_home_page = '{5}' WHERE group_id = {0}",
                    thisGroup.GroupId, Mysql.Escape(title), category, Mysql.Escape(description), Mysql.Escape(type), Mysql.Escape(homepage)));

                SetRedirectUri(thisGroup.Uri);
                core.Display.ShowMessage("Group Saved", "You have successfully edited the group.");
                return;
            }
        }
    }
}
