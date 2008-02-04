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
using System.Web.Security;
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
                template.ParseVariables("U_CREATE_GROUP", HttpUtility.HtmlEncode(Linker.AppendSid("/groups/create")));
                DataTable categoriesTable = db.SelectQuery("SELECT category_title, category_path, category_groups FROM global_categories");

                template.ParseVariables("CATEGORIES", HttpUtility.HtmlEncode(categoriesTable.Rows.Count.ToString()));

                for (int i = 0; i < categoriesTable.Rows.Count; i++)
                {
                    VariableCollection categoriesVariableCollection = template.CreateChild("category_list");

                    categoriesVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode((string)categoriesTable.Rows[i]["category_title"]));
                    categoriesVariableCollection.ParseVariables("GROUPS", HttpUtility.HtmlEncode(((long)categoriesTable.Rows[i]["category_groups"]).ToString()));
                    categoriesVariableCollection.ParseVariables("U_GROUP_CATEGORY", HttpUtility.HtmlEncode(Linker.AppendSid("/groups/" + (string)categoriesTable.Rows[i]["category_path"])));
                }

            }
            else
            {

                DataTable categoryTable = db.SelectQuery(string.Format("SELECT category_id, category_title FROM global_categories WHERE category_path = '{0}'",
                    Mysql.Escape((string)Request.QueryString["category"])));

                if (categoryTable.Rows.Count > 0)
                {
                    template.ParseVariables("CATEGORY_TITLE", HttpUtility.HtmlEncode((string)categoryTable.Rows[0]["category_title"]));
                    template.ParseVariables("U_CREATE_GROUP_C", HttpUtility.HtmlEncode(Linker.AppendSid("/groups/create?category=" + ((short)categoryTable.Rows[0]["category_id"]).ToString())));
                    template.ParseVariables("U_CREATE_GROUP", HttpUtility.HtmlEncode(Linker.AppendSid("/groups/create?category=" + ((short)categoryTable.Rows[0]["category_id"]).ToString())));

                    DataTable groupsTable = db.SelectQuery(string.Format("SELECT {1} FROM group_info gi WHERE gi.group_category = {0} AND gi.group_type <> 'PRIVATE'",
                        (short)categoryTable.Rows[0]["category_id"], UserGroup.GROUP_INFO_FIELDS));

                    template.ParseVariables("GROUPS", HttpUtility.HtmlEncode(groupsTable.Rows.Count.ToString()));

                    for (int i = 0; i < groupsTable.Rows.Count; i++)
                    {
                        UserGroup groupRow = new UserGroup(db, groupsTable.Rows[i]);

                        VariableCollection groupsVariableCollection = template.CreateChild("groups_list");

                        groupsVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode(groupRow.DisplayName));
                        groupsVariableCollection.ParseVariables("U_GROUP", HttpUtility.HtmlEncode(groupRow.Uri));
                    }
                }
            }

            EndResponse();
        }
    }
}
