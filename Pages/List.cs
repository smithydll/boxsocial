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
    [DataTable("user_lists")]
    public class List : Item
    {
        public const string LIST_FIELDS = "ul.list_id, ul.user_id, ul.list_type, ul.list_title, ul.list_items, ul.list_abstract, ul.list_path, ul.list_access";

        [DataField("list_id", DataFieldKeys.Primary)]
        private long listId;
        [DataField("user_id")]
        private long ownerId;
        [DataField("list_type")]
        private short type;
        [DataField("list_title", 31)]
        private string title;
        [DataField("list_items")]
        private long items;
        [DataField("list_abstract", MYSQL_TEXT)]
        private string listAbstract;
        [DataField("list_path", 31)]
        private string path;
        [DataField("list_access")]
        private ushort permissions;

        private User owner;
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

        public long Items
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
                if (listAccess == null)
                {
                    listAccess = new Access(db, permissions, Owner);
                }
                return listAccess;
            }
        }

        public User Owner
        {
            get
            {
                if (owner == null || ownerId != owner.Id)
                {
                    core.LoadUserProfile(ownerId);
                    owner = core.UserProfiles[ownerId];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

        public List(Core core, User owner, long listId)
            : base(core)
        {
            this.owner = owner;
            ItemLoad += new ItemLoadHandler(List_ItemLoad);

            try
            {
                LoadItem(listId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidListException();
            }
        }

        public List(Core core, User owner, string listName)
            : base(core)
        {
            this.owner = owner;
            ItemLoad += new ItemLoadHandler(List_ItemLoad);

            SelectQuery query = new SelectQuery(List.GetTable(typeof(List)));
            query.AddFields(List.GetFieldsPrefixed(typeof(List)));
            query.AddCondition("list_path", listName);
            query.AddCondition("user_id", owner.Id);

            DataTable listTable = db.Query(query);

            if (listTable.Rows.Count == 1)
            {
                loadItemInfo(listTable.Rows[0]);
            }
            else
            {
                throw new InvalidListException();
            }
        }

        private List(Core core, User owner, DataRow listRow)
            : base(core)
        {
            this.owner = owner;
            ItemLoad += new ItemLoadHandler(List_ItemLoad);

            loadItemInfo(listRow);
        }

        private void List_ItemLoad()
        {
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

            SelectQuery query = new SelectQuery(ListItem.GetTable(typeof(ListItem)));
            query.AddFields(ListItem.GetFieldsPrefixed(typeof(ListItem)));
            query.AddFields(ListItemText.GetFieldsPrefixed(typeof(ListItemText)));
            query.AddJoin(JoinTypes.Inner, ListItemText.GetTable(typeof(ListItemText)), "list_item_text_id", "list_item_text_id");
            query.AddCondition("list_id", listId);
            query.AddSort(SortOrder.Ascending, "list_item_text_normalised");

            DataTable listItemsTable = db.Query(query);

            foreach (DataRow dr in listItemsTable.Rows)
            {
                listItems.Add(new ListItem(core, dr));
            }

            return listItems;
        }

        /// <summary>
        /// This may seem confusing, but List<> is a type that the compiler can distinguish from List
        /// </summary>
        /// <param name="db"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public static List<List> GetLists(Core core, User owner)
        {
            List<List> lists = new List<List>();

            long loggedIdUid = User.GetMemberId(core.session.LoggedInMember);
            ushort readAccessLevel = owner.GetAccessLevel(core.session.LoggedInMember);

            DataTable listsTable = core.db.Query(string.Format("SELECT {0} FROM user_keys uk INNER JOIN user_lists ul ON ul.user_id = uk.user_id WHERE uk.user_id = {1} AND (list_access & {3:0} OR ul.user_id = {2})",
                List.LIST_FIELDS, owner.UserId, loggedIdUid, readAccessLevel));

            foreach (DataRow dr in listsTable.Rows)
            {
                lists.Add(new List(core, owner, dr));
            }

            return lists;
        }

        public static string BuildListsUri(User member)
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

            page.template.Parse("LIST_TITLE", string.Format("{0} Lists", page.ProfileOwner.DisplayNameOwnership));
            page.template.Parse("LIST_ABSTRACT", "FALSE");

            List<List> lists = List.GetLists(core, page.ProfileOwner);

            if (lists.Count > 0)
            {
                page.template.Parse("NOT_EMPTY", "TRUE");
            }

            foreach (List list in lists)
            {
                VariableCollection listVariableCollection = page.template.CreateChild("list_list");

                listVariableCollection.Parse("TITLE", list.Title);
                listVariableCollection.Parse("URI", List.BuildListUri(list));
            }
        }

        public static void Show(Core core, PPage page, string listName)
        {
            page.template.SetTemplate("viewlist.html");

            try
            {
                List list = new List(core, page.ProfileOwner, listName);

                list.ListAccess.SetSessionViewer(core.session);

                if (!list.ListAccess.CanRead)
                {
                    Functions.Generate403();
                    return;
                }

                page.template.Parse("LIST_TITLE", list.title);
                page.template.Parse("LIST_ID", list.ListId.ToString());
                page.template.Parse("LIST_LIST", "TRUE");

                if (!string.IsNullOrEmpty(list.Abstract))
                {
                    Display.ParseBbcode("LIST_ABSTRACT", list.Abstract);
                }
                else
                {
                    page.template.Parse("LIST_ABSTRACT", "FALSE");
                }

                List<ListItem> listItems = list.GetListItems();

                if (listItems.Count > 0)
                {
                    page.template.Parse("NOT_EMPTY", "TRUE");
                }

                foreach (ListItem listItem in listItems)
                {
                    VariableCollection listVariableCollection = page.template.CreateChild("list_list");

                    listVariableCollection.Parse("TITLE", listItem.Text);
                    listVariableCollection.Parse("URI", "FALSE");

                    listVariableCollection.Parse("U_DELETE", Linker.BuildRemoveFromListUri(listItem.ListItemId));
                }
            }
            catch (InvalidListException)
            {
                Functions.Generate404();
            }
        }

        public override long Id
        {
            get
            {
                return listId;
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
                return Linker.BuildListUri(Owner, path);
            }
        }
    }

    public class InvalidListException : Exception
    {
    }
}
