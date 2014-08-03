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
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Pages
{
    public class Pages
    {
        private Primitive owner;
        private Core core;
        private Mysql db;

        public Pages(Core core, Primitive owner)
        {
            this.owner = owner;
            this.core = core;
            this.db = core.Db;
        }

        public List<Page> GetPages(bool draft)
        {
            return GetPages(draft, false);
        }

        public List<Page> GetPages(bool draft, bool all)
        {
            List<Page> pages = new List<Page>();

            SelectQuery query = Page.GetSelectQueryStub(core, typeof(Page));
            query.AddCondition("page_item_id", owner.Id);
            query.AddCondition("page_item_type_id", owner.TypeId);
            if (!all)
            {
                query.AddCondition("page_list_only", false);
            }
            if (draft)
            {
                query.AddCondition("page_status", "DRAFT");
            }
            else
            {
                query.AddCondition("page_status", "PUBLISH");
            }
            query.AddSort(SortOrder.Ascending, "page_order");

            System.Data.Common.DbDataReader pagesReader = db.ReaderQuery(query);

            while(pagesReader.Read())
            {
                pages.Add(new Page(core, owner, pagesReader));
            }

            pagesReader.Close();
            pagesReader.Dispose();

            return pages;
        }
    }
}
