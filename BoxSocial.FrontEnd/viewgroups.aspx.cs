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
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;

namespace BoxSocial.FrontEnd
{
    public partial class viewgroups : TPage
    {
        public viewgroups()
            : base("viewgroups.html")
        {
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Request.QueryString["category"]))
            {
                template.Parse("U_CREATE_GROUP", Linker.AppendSid("/groups/create"));
                DataTable categoriesTable = db.Query("SELECT category_title, category_path, category_groups FROM global_categories");

                template.Parse("CATEGORIES", categoriesTable.Rows.Count.ToString());

                for (int i = 0; i < categoriesTable.Rows.Count; i++)
                {
                    VariableCollection categoriesVariableCollection = template.CreateChild("category_list");

                    categoriesVariableCollection.Parse("TITLE", (string)categoriesTable.Rows[i]["category_title"]);
                    categoriesVariableCollection.Parse("GROUPS", ((long)categoriesTable.Rows[i]["category_groups"]).ToString());
                    categoriesVariableCollection.Parse("U_GROUP_CATEGORY", Linker.AppendSid("/groups/" + (string)categoriesTable.Rows[i]["category_path"]));
                }

            }
            else
            {

                DataTable categoryTable = db.Query(string.Format("SELECT category_id, category_title FROM global_categories WHERE category_path = '{0}'",
                    Mysql.Escape((string)Request.QueryString["category"])));

                if (categoryTable.Rows.Count > 0)
                {
                    template.Parse("CATEGORY_TITLE", (string)categoryTable.Rows[0]["category_title"]);
                    template.Parse("U_CREATE_GROUP_C", Linker.AppendSid("/groups/create?category=" + ((long)categoryTable.Rows[0]["category_id"]).ToString()));
                    template.Parse("U_CREATE_GROUP", Linker.AppendSid("/groups/create?category=" + ((long)categoryTable.Rows[0]["category_id"]).ToString()));

                    DataTable groupsTable = db.Query(string.Format("SELECT {1} FROM group_info gi WHERE gi.group_category = {0} AND gi.group_type <> 'PRIVATE'",
                        (long)categoryTable.Rows[0]["category_id"], UserGroup.GROUP_INFO_FIELDS));

                    template.Parse("GROUPS", groupsTable.Rows.Count.ToString());

                    for (int i = 0; i < groupsTable.Rows.Count; i++)
                    {
                        UserGroup groupRow = new UserGroup(core, groupsTable.Rows[i], UserGroupLoadOptions.Common);

                        VariableCollection groupsVariableCollection = template.CreateChild("groups_list");

                        groupsVariableCollection.Parse("TITLE", groupRow.DisplayName);
                        groupsVariableCollection.Parse("U_GROUP", groupRow.Uri);
                    }
                }
            }

            EndResponse();
        }
    }
}
