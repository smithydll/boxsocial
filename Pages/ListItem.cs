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
using Lachlan.Web;
using BoxSocial.Internals;

namespace BoxSocial.Applications.Pages
{
    public class ListItem
    {
        public const string LIST_ITEM_FIELDS = "li.list_item_id, li.list_id, li.list_item_text_id, lit.list_item_text, lit.list_item_text_normalised";

        private Mysql db;

        private long listItemId;
        private long listId;
        private string text;
        private string textNormalised;

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

        public ListItem(Mysql db, DataRow listItemRow)
        {
            this.db = db;

            loadListItemInfo(listItemRow);
        }

        private void loadListItemInfo(DataRow listItemRow)
        {
            listItemId = (long)listItemRow["list_item_id"];
            listId = (long)listItemRow["list_id"];
            text = (string)listItemRow["list_item_text"];
            textNormalised = (string)listItemRow["list_item_text_normalised"];
        }
    }
}
