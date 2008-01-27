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
    public class List
    {
        public const string LIST_FIELDS = "ul.list_id, ul.user_id, ul.list_type, ul.list_title, ul.list_items, ul.list_abstract, ul.list_path, ul.list_access";

        private Mysql db;

        private long listId;
        private int ownerId;
        private Member owner;
        private short type;
        private string title;
        private uint items;
        private string listAbstract;
        private string path;
        private ushort permissions;
        private Access listAccess;

        public long ListId
        {
            get
            {
                return listId;
            }
        }

        public short Type
        {
            get
            {
                return type;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
        }

        public uint Items
        {
            get
            {
                return items;
            }
        }

        public string Abstract
        {
            get
            {
                return listAbstract;
            }
        }

        public string Path
        {
            get
            {
                return path;
            }
        }

        public ushort Permissions
        {
            get
            {
                return permissions;
            }
        }

        public Access ListAccess
        {
            get
            {
                return listAccess;
            }
        }

        public List(Mysql db, Member owner, string listName)
        {
            this.db = db;
            this.owner = owner;

            DataTable listTable = db.SelectQuery(string.Format("SELECT {0} FROM user_lists ul WHERE ul.user_id = {1} AND ul.list_path = '{2}';",
                List.LIST_FIELDS, owner.UserId, Mysql.Escape(listName)));

            if (listTable.Rows.Count == 1)
            {
                loadListInfo(listTable.Rows[0]);
            }
            else
            {
                throw new Exception("Could not load list exception");
            }
        }

        private List(Mysql db, Member owner, DataRow listRow)
        {
            this.db = db;
            this.owner = owner;

            loadListInfo(listRow);
        }

        private void loadListInfo(DataRow listRow)
        {
            listId = (long)listRow["list_id"];
            ownerId = (int)listRow["user_id"];
            type = (short)listRow["list_type"];
            title = (string)listRow["list_title"];
            items = (uint)listRow["list_items"];
            if (!(listRow["list_abstract"] is DBNull))
            {
                listAbstract = (string)listRow["list_abstract"];
            }
            path = (string)listRow["list_path"];
            permissions = (ushort)listRow["list_access"];

            listAccess = new Access(db, permissions, owner);
        }

        public List<ListItem> GetListItems()
        {
            List<ListItem> listItems = new List<ListItem>();

            DataTable listItemsTable = db.SelectQuery(string.Format("SElECT {0} FROM list_items li INNER JOIN list_items_text lit ON li.list_item_text_id = lit.list_item_text_id WHERE li.list_id = {1} ORDER BY lit.list_item_text_normalised ASC;",
                ListItem.LIST_ITEM_FIELDS, listId));

            foreach (DataRow dr in listItemsTable.Rows)
            {
                listItems.Add(new ListItem(db, dr));
            }

            return listItems;
        }

        /// <summary>
        /// This may seem confusing, but List<> is a type that the compiler can distinguish from List
        /// </summary>
        /// <param name="db"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public static List<List> GetLists(Core core, Member owner)
        {
            List<List> lists = new List<List>();

            long loggedIdUid = Member.GetMemberId(core.session.LoggedInMember);
            ushort readAccessLevel = owner.GetAccessLevel(core.session.LoggedInMember);

            DataTable listsTable = core.db.SelectQuery(string.Format("SELECT {0} FROM user_keys uk INNER JOIN user_lists ul ON ul.user_id = uk.user_id WHERE uk.user_id = {1} AND (list_access & {3:0} OR ul.user_id = {2})",
                List.LIST_FIELDS, owner.UserId, loggedIdUid, readAccessLevel));

            foreach (DataRow dr in listsTable.Rows)
            {
                lists.Add(new List(core.db, owner, dr));
            }

            return lists;
        }

        public static string BuildListsUri(Member member)
        {
            return Linker.AppendSid(string.Format("/{0}/lists",
                member.UserName.ToLower()));
        }

        public static string BuildListUri(List list)
        {
            return Linker.AppendSid(string.Format("/{0}/lists/{1}",
                list.owner.UserName.ToLower(), list.path));
        }

        public static void ShowLists(Core core, PPage page)
        {
            page.template.SetTemplate("viewlist.html");

            page.template.ParseVariables("LIST_TITLE", HttpUtility.HtmlEncode(string.Format("{0} Lists", page.ProfileOwner.DisplayNameOwnership)));
            page.template.ParseVariables("LIST_ABSTRACT", "FALSE");

            List<List> lists = List.GetLists(core, page.ProfileOwner);

            if (lists.Count > 0)
            {
                page.template.ParseVariables("NOT_EMPTY", "TRUE");
            }

            foreach (List list in lists)
            {
                VariableCollection listVariableCollection = page.template.CreateChild("list_list");

                listVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode(list.Title));
                listVariableCollection.ParseVariables("URI", HttpUtility.HtmlEncode(List.BuildListUri(list)));
            }
        }

        public static void Show(Core core, PPage page, string listName)
        {
            page.template.SetTemplate("viewlist.html");

            try
            {
                List list = new List(core.db, page.ProfileOwner, listName);

                list.ListAccess.SetSessionViewer(core.session);

                if (!list.ListAccess.CanRead)
                {
                    Functions.Generate403(core);
                    return;
                }

                page.template.ParseVariables("LIST_TITLE", HttpUtility.HtmlEncode(list.title));
                page.template.ParseVariables("LIST_ID", HttpUtility.HtmlEncode(list.ListId.ToString()));
                page.template.ParseVariables("LIST_LIST", "TRUE");

                if (!string.IsNullOrEmpty(list.Abstract))
                {
                    page.template.ParseVariables("LIST_ABSTRACT", Bbcode.Parse(HttpUtility.HtmlEncode(list.Abstract)));
                }
                else
                {
                    page.template.ParseVariables("LIST_ABSTRACT", "FALSE");
                }

                List<ListItem> listItems = list.GetListItems();

                if (listItems.Count > 0)
                {
                    page.template.ParseVariables("NOT_EMPTY", "TRUE");
                }

                foreach (ListItem listItem in listItems)
                {
                    VariableCollection listVariableCollection = page.template.CreateChild("list_list");

                    listVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode(listItem.Text));
                    listVariableCollection.ParseVariables("URI", "FALSE");

                    listVariableCollection.ParseVariables("U_DELETE", HttpUtility.HtmlEncode(Linker.BuildRemoveFromListUri(listItem.ListItemId)));
                }
            }
            catch
            {
                Functions.Generate404(core);
            }
        }
    }
}
