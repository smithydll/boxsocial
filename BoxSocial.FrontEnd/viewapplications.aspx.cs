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

namespace BoxSocial.FrontEnd
{
    public partial class viewapplications : TPage
    {
        public viewapplications()
            : base("viewapplications.html")
        {
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            int p = Functions.RequestInt("p", 1);
            string type = Request.QueryString["type"];
            long id = Functions.RequestLong("id", 0);

            AppPrimitives viewingPrimitive = AppPrimitives.Member;

            if (!string.IsNullOrEmpty(type))
            {
                switch (type)
                {
                    case "USER":
                        viewingPrimitive = AppPrimitives.Member;
                        break;
                    case "GROUP":
                        viewingPrimitive = AppPrimitives.Group;
                        break;
                    case "NETWORK":
                        viewingPrimitive = AppPrimitives.Network;
                        break;
                    case "APPLICATION":
                        viewingPrimitive = AppPrimitives.Application;
                        break;
                    case "MUSIC":
                        viewingPrimitive = AppPrimitives.Musician;
                        break;
                }
            }

            SelectQuery query = ApplicationEntry.GetSelectQueryStub(typeof(ApplicationEntry));
            query.AddCondition("application_primitives & " + (byte)viewingPrimitive, (byte)viewingPrimitive);
            query.AddSort(SortOrder.Ascending, "application_title");
            query.LimitStart = (p - 1) * 10;
            query.LimitCount = 10;

            DataTable applicationsTable = db.Query(query);

            foreach (DataRow dr in applicationsTable.Rows)
            {
                ApplicationEntry ae = new ApplicationEntry(core, dr);

                VariableCollection applicationVariableCollection = template.CreateChild("application_list");

                applicationVariableCollection.Parse("TITLE", ae.Title);
                applicationVariableCollection.Parse("URI", ae.GetUri(type, id));
            }

            Display.ParsePagination("/applications/", p, (int)Math.Ceiling((double)applicationsTable.Rows.Count / 10));

            EndResponse();
        }
    }
}
