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
    public class ListItem : NumberedItem
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
                lit = new ListItemText(core, listItemTable.Rows[0]);
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
            SelectQuery query = NumberedItem.GetSelectQueryStub(typeof(ListItem), false);

            query.AddFields(ListItemText.GetFieldsPrefixed(typeof(ListItemText)));
            query.AddJoin(JoinTypes.Inner, ListItemText.GetTable(typeof(ListItemText)), "list_item_text_id", "list_item_text_id");

            query.AddSort(SortOrder.Ascending, "list_item_text_normalised");

            return query;
        }

        public static ListItem Create(Core core, List list, string text, ref string normalisedText)
        {
            if (string.IsNullOrEmpty(normalisedText))
            {
                normalisedText = text;
            }

            if (!list.Access.Can("APPEND"))
            {
                throw new UnauthorisedToCreateItemException();
            }

            NormaliseListItem(text, ref normalisedText);

            core.db.BeginTransaction();

            ListItemText lit;

            try
            {
                lit = new ListItemText(core, normalisedText);
            }
            catch (InvalidListItemTextException)
            {
                lit = ListItemText.Create(core, text, ref normalisedText);
            }

            InsertQuery iQuery = new InsertQuery(GetTable(typeof(ListItem)));
            iQuery.AddField("list_id", list.Id);
            iQuery.AddField("list_item_text_id", lit.Id);

            long listItemId = core.db.Query(iQuery);

            return new ListItem(core, listItemId);
        }

        public static void NormaliseListItem(string text, ref string normalisedText)
        {
            if (string.IsNullOrEmpty(normalisedText))
            {
                normalisedText = text;
            }

            // normalise slug if it has been fiddeled with
            normalisedText = normalisedText.ToLower().Normalize(NormalizationForm.FormD);
            string normalisedSlug = "";

            for (int i = 0; i < normalisedText.Length; i++)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(normalisedText[i]) != UnicodeCategory.NonSpacingMark)
                {
                    normalisedSlug += normalisedText[i];
                }
            }
            // we want to be a little less stringent with list items to allow for some punctuation of being of importance
            normalisedText = Regex.Replace(normalisedSlug, @"([^\w\+\&\*\(\)\=\:\?\-\#\@\!\$]+)", "-");
        }

        public override long Id
        {
            get
            {
                return listItemId;
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
    public class ListItemText : NumberedItem
    {
        [DataField("list_item_text_id", DataFieldKeys.Primary)]
        private long listItemTextId;
        [DataField("list_item_text", 63)]
        private string text;
        [DataField("list_item_text_normalised", DataFieldKeys.Unique, 63)]
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

        internal ListItemText(Core core, long listItemTextId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ListItemText_ItemLoad);

            try
            {
                LoadItem(listItemTextId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidListItemTextException();
            }
        }

        internal ListItemText(Core core, string textNormalised)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ListItemText_ItemLoad);

            try
            {
                LoadItem("list_item_text_normalised", textNormalised);
            }
            catch (InvalidItemException)
            {
                throw new InvalidListItemTextException();
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

        internal static ListItemText Create(Core core, string text, ref string normalisedText)
        {
            ListItem.NormaliseListItem(text, ref normalisedText);

            InsertQuery iQuery = new InsertQuery(GetTable(typeof(ListItemText)));
            iQuery.AddField("list_item_text", text);
            iQuery.AddField("list_item_text_normalised", normalisedText);

            long listItemTextId = core.db.Query(iQuery);

            return new ListItemText(core, listItemTextId);
        }

        public override long Id
        {
            get
            {
                return listItemTextId;
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

    public class InvalidListItemTextException : Exception
    {
    }
}
