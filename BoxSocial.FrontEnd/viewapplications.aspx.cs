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

            DataTable applicationsTable = db.Query(string.Format(@"SELECT {0} FROM applications ap ORDER BY application_title ASC LIMIT {1}, 10",
                ApplicationEntry.APPLICATION_FIELDS, (p - 1) * 10));

            foreach (DataRow dr in applicationsTable.Rows)
            {
                ApplicationEntry ae = new ApplicationEntry(db, dr);

                VariableCollection applicationVariableCollection = template.CreateChild("application_list");

                applicationVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode(ae.Title));
                applicationVariableCollection.ParseVariables("URI", HttpUtility.HtmlEncode(ae.Uri));
            }

            Display.GeneratePagination("/applications/", p, (int)Math.Ceiling((double)applicationsTable.Rows.Count / 10));

            EndResponse();
        }
    }
}
