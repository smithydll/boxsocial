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
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Pages
{
    [DataTable("user_lists")]
    [Permission("VIEW", "Can view the list", PermissionTypes.View)]
    [Permission("EDIT", "Can edit the list", PermissionTypes.CreateAndEdit)]
    [Permission("APPEND", "Can add items to the list", PermissionTypes.CreateAndEdit)]
    [Permission("DELETE_ITEMS", "Can delete items from the list", PermissionTypes.Delete)]
    public class List : NumberedItem, IPermissibleItem
    {
        [DataField("list_id", DataFieldKeys.Primary)]
        private long listId;
        [DataField("user_id", DataFieldKeys.Unique, "u_user_path")]
        private long ownerId;
        [DataField("list_type", typeof(ListType))]
        private long type;
        [DataField("list_title", 31)]
        private string title;
        [DataField("list_items")]
        private long items;
        [DataField("list_abstract", MYSQL_TEXT)]
        private string listAbstract;
        [DataField("list_path", DataFieldKeys.Unique, "u_user_path", 31)]
        private string path;
        [DataField("list_simple_permissions")]
        private bool simplePermissions;

        private User owner;
        private Access listAccess;

        public long ListId
        {
            get
            {
                return listId;
            }
        }

        public long Type
        {
            get
            {
                return type;
            }
            set
            {
                SetProperty("type", value);
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                string slug = value;
                Navigation.GenerateSlug(title, ref slug);

                SetProperty("title", value);

                Path = slug;
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
            set
            {
                SetProperty("listAbstract", value);
            }
        }

        public string Path
        {
            get
            {
                return path;
            }
            set
            {
                SetProperty("path", value);
            }
        }

        public Access ListAccess
        {
            get
            {
                if (listAccess == null)
                {
                    listAccess = new Access(core, this);
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
                    owner = core.PrimitiveCache[ownerId];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

        public List(Core core, long listId)
            : base(core)
        {
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

        internal List(Core core, User owner, DataRow listRow)
            : base(core)
        {
            this.owner = owner;
            ItemLoad += new ItemLoadHandler(List_ItemLoad);

            loadItemInfo(listRow);
        }

        private void List_ItemLoad()
        {
        }

        /* EXAMPLE getSubItems */
        public List<ListItem> GetListItems()
        {
            return getSubItems(typeof(ListItem)).ConvertAll<ListItem>(new Converter<Item, ListItem>(convertToListItem));
        }

        public ListItem convertToListItem(Item input)
        {
            return (ListItem)input;
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

            SelectQuery query = Item.GetSelectQueryStub(typeof(List));
            query.AddCondition("user_id", owner.Id);

            DataTable listsTable = core.Db.Query(query);

            foreach (DataRow dr in listsTable.Rows)
            {
                lists.Add(new List(core, owner, dr));
            }

            return lists;
        }

        public static string BuildListsUri(Core core, User member)
        {
            return core.Uri.AppendSid(string.Format("/{0}/lists",
                member.UserName.ToLower()));
        }

        public static string BuildListUri(Core core, List list)
        {
            return core.Uri.AppendSid(string.Format("/{0}/lists/{1}",
                list.owner.UserName.ToLower(), list.path));
        }

        public static bool IsValidListType(Core core, short listType)
        {
            // verify that the type exists
            SelectQuery query = new SelectQuery("list_types");
            query.AddFields("list_type_id");
            query.AddCondition("list_type_id", listType);

            DataTable listTypeTable = core.Db.Query(query);

            if (listTypeTable.Rows.Count == 1)
            {
                return true;
            }

            return false;
        }

        public ListItem AddNew(string text, ref string normalisedText)
        {
            ListItem item = ListItem.Create(core, this, text, ref normalisedText);

            UpdateQuery uQuery = new UpdateQuery(GetTable(typeof(List)));
            uQuery.AddField("list_items", new QueryOperation("list_items", QueryOperations.Addition, 1));
            uQuery.AddCondition("list_id", listId);

            core.Db.Query(uQuery);

            return item;
        }

        public void Remove(long listItemId)
        {
            ListItem item = new ListItem(core, listItemId);
            
            db.BeginTransaction();

            item.Delete();

            UpdateQuery uQuery = new UpdateQuery(GetTable(typeof(List)));
            uQuery.AddField("list_items", new QueryOperation("list_items", QueryOperations.Subtraction, 1));
            uQuery.AddCondition("list_id", listId);

            db.Query(uQuery);
        }

        public static void Remove(Core core, ListItem item)
        {
            core.Db.BeginTransaction();

            item.Delete();

            UpdateQuery uQuery = new UpdateQuery(GetTable(typeof(List)));
            uQuery.AddField("list_items", new QueryOperation("list_items", QueryOperations.Subtraction, 1));
            uQuery.AddCondition("list_id", item.ListId);

            core.Db.Query(uQuery);
        }

        public static List Create(Core core, string title, ref string slug, string listAbstract, short listType)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            Navigation.GenerateSlug(title, ref slug);

            if (!IsValidListType(core, listType))
            {
                throw new ListTypeNotValidException();
            }

            try
            {
                Page listPage;
                try
                {
                    listPage = new Page(core, core.Session.LoggedInMember, "lists");
                }
                catch (PageNotFoundException)
                {
                    string listsSlug = "lists";
                    try
                    {
                        listPage = Page.Create(core, core.Session.LoggedInMember, "Lists", ref listsSlug, 0, "", PageStatus.PageList, 0, Classifications.None);
                    }
                    catch (PageSlugNotUniqueException)
                    {
                        throw new Exception("Cannot create lists slug.");
                    }
                }
                Page page = Page.Create(core, core.Session.LoggedInMember, title, ref slug, listPage.Id, "", PageStatus.PageList, 0, Classifications.None);

                // Create list

                InsertQuery iQuery = new InsertQuery(GetTable(typeof(List)));
                iQuery.AddField("user_id", core.LoggedInMemberId);
                iQuery.AddField("list_title", title);
                iQuery.AddField("list_path", slug);
                iQuery.AddField("list_type", listType);
                iQuery.AddField("list_abstract", listAbstract);

                long listId = core.Db.Query(iQuery);

                List list = new List(core, core.Session.LoggedInMember, listId);

                /* LOAD THE DEFAULT ITEM PERMISSIONS */
                list.Access.CreateAllGrantsForOwner();
                list.Access.CreateGrantForPrimitive(User.EveryoneGroupKey, "VIEW");
                //Access.CreateAllGrantsForOwner(core, list);
                //Access.CreateGrantForPrimitive(core, list, new ItemKey(-2, ItemType.GetTypeId(typeof(User))), "VIEW");

                return list;
            }
            catch (PageSlugNotUniqueException)
            {
                throw new ListSlugNotUniqueException();
            }
        }

        //public static void ShowLists(Core core, UPage page)
        public static void ShowLists(object sender, ShowPPageEventArgs e)
        {
            e.Template.SetTemplate("Pages", "viewlist");

            Page page = new Page(e.Core, e.Page.Owner, "lists");

            e.Core.Display.ParsePageList(e.Page.Owner, true, page);

            e.Template.Parse("LIST_TITLE", string.Format("{0} Lists", e.Page.Owner.DisplayNameOwnership));
            e.Template.Parse("LIST_ABSTRACT", "FALSE");

            List<List> lists = List.GetLists(e.Core, (User)e.Page.Owner);

            if (lists.Count > 0)
            {
                e.Template.Parse("NOT_EMPTY", "TRUE");
            }

            foreach (List list in lists)
            {
                VariableCollection listVariableCollection = e.Template.CreateChild("list_list");

                listVariableCollection.Parse("TITLE", list.Title);
                listVariableCollection.Parse("URI", List.BuildListUri(e.Core, list));
            }

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "lists", "Lists" });

            page.Owner.ParseBreadCrumbs(breadCrumbParts);
        }

        public static void Show(object sender, ShowPPageEventArgs e)
        {
            e.Template.SetTemplate("Pages", "viewlist");

            try
            {
                Page page = new Page(e.Core, e.Page.Owner, "lists/" + e.Slug);
                List list = new List(e.Core, (User)e.Page.Owner, e.Slug);

                if (!list.Access.Can("VIEW"))
                {
                    e.Core.Functions.Generate403();
                    return;
                }

                e.Core.Display.ParsePageList(e.Page.Owner, true, page);

                e.Template.Parse("LIST_TITLE", list.title);
                e.Template.Parse("LIST_ID", list.ListId.ToString());
                e.Template.Parse("LIST_LIST", "TRUE");

                if (!string.IsNullOrEmpty(list.Abstract))
                {
                    e.Core.Display.ParseBbcode("LIST_ABSTRACT", list.Abstract);
                }
                else
                {
                    e.Template.Parse("LIST_ABSTRACT", "FALSE");
                }

                List<ListItem> listItems = list.GetListItems();

                if (listItems.Count > 0)
                {
                    e.Template.Parse("NOT_EMPTY", "TRUE");
                }

                foreach (ListItem listItem in listItems)
                {
                    VariableCollection listVariableCollection = e.Template.CreateChild("list_list");

                    listVariableCollection.Parse("TITLE", listItem.Text);
                    listVariableCollection.Parse("URI", "FALSE");

                    if (list.Owner.Id == e.Core.LoggedInMemberId)
                    {
                        listVariableCollection.Parse("U_DELETE", e.Core.Uri.BuildRemoveFromListUri(listItem.ListItemId));
                    }
                }

                List<string[]> breadCrumbParts = new List<string[]>();
                breadCrumbParts.Add(new string[] { "lists", "Lists" });
                breadCrumbParts.Add(new string[] { list.Path, list.Title });

                page.Owner.ParseBreadCrumbs(breadCrumbParts);
            }
            catch (InvalidListException)
            {
                e.Core.Functions.Generate404();
            }
        }

        public override long Id
        {
            get
            {
                return listId;
            }
        }

        public override string Uri
        {
            get
            {
                return core.Uri.BuildListUri(Owner, path);
            }
        }

        public Access Access
        {
            get
            {
                return ListAccess;
            }
        }

        public bool IsSimplePermissions
        {
            get
            {
                return simplePermissions;
            }
            set
            {
                SetPropertyByRef(new { simplePermissions }, value);
            }
        }

        Primitive IPermissibleItem.Owner
        {
            get
            {
                return this.Owner;
            }
        }

        public List<AccessControlPermission> AclPermissions
        {
            get
            {
                return AccessControlLists.GetPermissions(core, this);
            }
        }

        public bool IsItemGroupMember(User viewer, ItemKey key)
        {
            return false;
        }

        public IPermissibleItem PermissiveParent
        {
            get
            {
                return Owner;
            }
        }

        public ItemKey PermissiveParentKey
        {
            get
            {
                return new ItemKey(ownerId, typeof(User));
            }
        }

        public bool GetDefaultCan(string permission)
        {
            return false;
        }

        public string DisplayTitle
        {
            get
            {
                return "List: " + Title;
            }
        }

        public string ParentPermissionKey(Type parentType, string permission)
        {
            return permission;
        }
    }

    public class InvalidListException : Exception
    {
    }

    public class ListSlugNotUniqueException : Exception
    {
    }

    public class ListTypeNotValidException : Exception
    {
    }
}
