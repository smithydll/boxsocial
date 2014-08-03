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
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.FrontEnd
{
    public partial class viewapplications : TPage
    {
        public viewapplications()
            : base("viewapplications.html")
        {
            this.Load += new EventHandler(Page_Load);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            long typeId = core.Functions.RequestLong("type", 0);
            long id = core.Functions.RequestLong("id", 0);

            AppPrimitives viewingPrimitive = AppPrimitives.None;

            if (typeId == 0 || typeId == ItemKey.GetTypeId(typeof(User)))
			{
				typeId = ItemKey.GetTypeId(typeof(User));
                viewingPrimitive = AppPrimitives.Member;
			}
			else if (typeId == ItemKey.GetTypeId(typeof(UserGroup)))
			{					
                viewingPrimitive = AppPrimitives.Group;
			}
			else if (typeId == ItemKey.GetTypeId(typeof(Network)))
			{
                viewingPrimitive = AppPrimitives.Network;
			}
			else if (typeId == ItemKey.GetTypeId(typeof(ApplicationEntry)))
			{
                viewingPrimitive = AppPrimitives.Application;
			}
			else if (typeId == ItemKey.GetTypeId(typeof(Musician.Musician)))
			{
                viewingPrimitive = AppPrimitives.Musician;
            }

            SelectQuery query = ApplicationEntry.GetSelectQueryStub(core, typeof(ApplicationEntry));
            query.AddCondition("application_primitives & " + ((byte)viewingPrimitive).ToString(), (byte)viewingPrimitive);
            query.AddCondition("application_locked", false);
            query.AddSort(SortOrder.Ascending, "application_title");
            query.LimitStart = (TopLevelPageNumber - 1) * 10;
            query.LimitCount = 10;

            DataTable applicationsTable = db.Query(query);

            foreach (DataRow dr in applicationsTable.Rows)
            {
                ApplicationEntry ae = new ApplicationEntry(core, dr);

                VariableCollection applicationVariableCollection = template.CreateChild("application_list");

                applicationVariableCollection.Parse("TITLE", ae.Title);
                applicationVariableCollection.Parse("URI", ae.GetUri(typeId, id));
                applicationVariableCollection.Parse("I_TILE", ae.Tile);
            }

            core.Display.ParsePagination("/applications/", 10, applicationsTable.Rows.Count);

            EndResponse();
        }
    }
}
