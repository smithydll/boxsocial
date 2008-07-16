﻿/*
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

namespace BoxSocial.Applications.Pages
{
    public class Pages
    {
        private User owner;
        private Core core;
        private Mysql db;

        public Pages(Core core, User owner)
        {
            this.owner = owner;
            this.core = core;
            this.db = core.db;
        }

        public List<Page> GetPages(bool draft)
        {
            List<Page> pages = new List<Page>();

            SelectQuery query = new SelectQuery("user_pages");
            query.AddFields(Page.GetFieldsPrefixed(typeof(Page)));
            query.AddCondition("user_id", owner.Id);
            query.AddCondition("page_list_only", false);
            if (draft)
            {
                query.AddCondition("page_status", "DRAFT");
            }
            else
            {
                query.AddCondition("page_status", "PUBLISH");
            }
            query.AddSort(SortOrder.Ascending, "page_order");

            DataTable pagesTable = db.Query(query);

            foreach (DataRow dr in pagesTable.Rows)
            {
                pages.Add(new Page(core, owner, dr));
            }

            return pages;
        }
    }
}