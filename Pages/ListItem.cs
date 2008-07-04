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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Pages
{
    [DataTable("list_items")]
    public class ListItem : Item
    {
        public const string LIST_ITEM_FIELDS = "li.list_item_id, li.list_id, li.list_item_text_id, lit.list_item_text, lit.list_item_text_normalised";

        [DataField("list_item_id", DataFieldKeys.Primary)]
        private long listItemId;
        [DataField("list_id", typeof(List))]
        private long listId;
        [DataField("list_item_text_id")]
        private long listItemTextId;

        private ListItemText lit;

        public long ListItemId
        {
            get
            {
                return listItemId;
            }
        }

        public long ListId
        {
            get
            {
                return listId;
            }
        }

        public string Text
        {
            get
            {
                return lit.Text;
            }
        }

        public string TextNormalised
        {
            get
            {
                return lit.TextNormalised;
            }
        }

        public ListItemText ItemText
        {
            get
            {
                return lit;
            }
        }

        internal ListItem(Core core, long listItemId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ListItem_ItemLoad);

            
            SelectQuery query = ListItem_GetSelectQueryStub();
            query.AddCondition("list_item_id", listItemId);

            DataTable listItemTable = db.Query(query);

            if (listItemTable.Rows.Count == 1)
            {
                loadItemInfo(listItemTable.Rows[0]);
            }
            else
            {
                throw new InvalidListException();
            }
        }

        public ListItem(Core core, DataRow listItemRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ListItem_ItemLoad);

            loadItemInfo(listItemRow);
            lit = new ListItemText(core, listItemRow);
        }

        private void ListItem_ItemLoad()
        {
        }

        public static SelectQuery ListItem_GetSelectQueryStub()
        {
            SelectQuery query = Item.GetSelectQueryStub(typeof(ListItem));

            query.AddFields(ListItemText.GetFieldsPrefixed(typeof(ListItemText)));
            query.AddJoin(JoinTypes.Inner, ListItemText.GetTable(typeof(ListItemText)), "list_item_text_id", "list_item_text_id");

            query.AddSort(SortOrder.Ascending, "list_item_text_normalised");

            return query;
        }

        public override long Id
        {
            get
            {
                return listItemId;
            }
        }

        public override string Namespace
        {
            get
            {
                return this.GetType().FullName;
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

    [DataTable("list_items_text")]
    public class ListItemText : Item
    {
        [DataField("list_item_text_id", DataFieldKeys.Primary)]
        private long listItemTextId;
        [DataField("list_item_text", 63)]
        private string text;
        [DataField("list_item_text_normalised", 63)]
        private string textNormalised;

        public long ListItemTextId
        {
            get
            {
                return listItemTextId;
            }
        }

        public string Text
        {
            get
            {
                return text;
            }
        }

        public string TextNormalised
        {
            get
            {
                return textNormalised;
            }
        }

        internal ListItemText(Core core, DataRow listTextRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ListItemText_ItemLoad);

            loadItemInfo(listTextRow);
        }

        private void ListItemText_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return listItemTextId;
            }
        }

        public override string Namespace
        {
            get
            {
                return this.GetType().FullName;
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

    public class InvalidListItemException : Exception
    {
    }
}
