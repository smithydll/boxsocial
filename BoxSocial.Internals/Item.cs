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
using System.Text;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public abstract class Item
    {
        protected Mysql db;
        protected Core core;

        protected Item(Core core)
        {
            this.core = core;
            this.db = core.db;
        }

        protected DataTable Query(SelectQuery query)
        {
            return db.Query(query);
        }

        protected long Query(InsertQuery query)
        {
            return db.Query(query);
        }

        protected long Query(UpdateQuery query)
        {
            return db.Query(query);
        }

        protected long Query(DeleteQuery query)
        {
            return db.Query(query);
        }

        public abstract long Id
        {
            get;
        }

        public abstract string Namespace
        {
            get;
        }

        public abstract string Uri
        {
            get;
        }

        protected List<ItemTag> getTags()
        {
            List<ItemTag> tags = new List<ItemTag>();

            return tags;
        }
    }
}
