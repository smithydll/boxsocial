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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("global_categories")]
    public class Category : NumberedItem
    {
        [DataField("category_id", DataFieldKeys.Primary)]
        private long categoryId;
        [DataField("category_title", 31)]
        private string title;
        [DataField("category_path", DataFieldKeys.Unique, 31)]
        private string path;
        [DataField("category_groups")]
        private long groupCount;

        public long CategoryId
        {
            get
            {
                return categoryId;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
        }

        public string Path
        {
            get
            {
                return path;
            }
        }

        public long Groups
        {
            get
            {
                return groupCount;
            }
        }

        public Category(Core core, string path)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Category_ItemLoad);

            this.path = path;

            /* We cache this as it's quite static */
            switch (path)
            {
                default:
                    try
                    {
                        LoadItem("category_path", path);
                    }
                    catch (InvalidItemException)
                    {
                        throw new InvalidCategoryException();
                    }
                    break;
            }
        }

        public Category(Core core, DataRow categoryRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Category_ItemLoad);

            loadItemInfo(categoryRow);
        }

        protected override void loadItemInfo(DataRow categoryRow)
        {
            loadValue(categoryRow, "category_id", out categoryId);
            loadValue(categoryRow, "category_title", out title);
            loadValue(categoryRow, "category_path", out path);
            loadValue(categoryRow, "category_groups", out groupCount);

            itemLoaded(categoryRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        void Category_ItemLoad()
        {
        }

        public static List<Category> GetAll(Core core)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            List<Category> categories = new List<Category>();

            SelectQuery query = GetSelectQueryStub(typeof(Category));

            DataTable categoriesDataTable = core.Db.Query(query);

            foreach (DataRow dr in categoriesDataTable.Rows)
            {
                categories.Add(new Category(core, dr));
            }

            return categories;
        }

        public override long Id
        {
            get
            {
                return categoryId;
            }
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }

    public class InvalidCategoryException : Exception
    {
    }
}
