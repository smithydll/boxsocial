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
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Pages
{
    [AccountSubModule("pages", "lists")]
    public class AccountListsManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Manage Lists";
            }
        }

        public override int Order
        {
            get
            {
                return 4;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountListsManage class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountListsManage(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountListsManage_Load);
            this.Show += new EventHandler(AccountListsManage_Show);
        }

        void AccountListsManage_Load(object sender, EventArgs e)
        {
            AddModeHandler("remove", new ModuleModeHandler(AccountListsManage_Remove));
            AddModeHandler("delete", new ModuleModeHandler(AccountListsManage_Delete));
            AddModeHandler("edit", new ModuleModeHandler(AccountListsManage_Edit));
            AddModeHandler("append", new ModuleModeHandler(AccountListsManage_Append));
        }

        void AccountListsManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_lists");

            ushort listPermissions = 0x1111;

			SelectQuery query = List.GetSelectQueryStub(typeof(List));
			query.AddCondition("user_id", Owner.Id); 
            DataTable listsTable = db.Query(query);

            for (int i = 0; i < listsTable.Rows.Count; i++)
            {
				List l = new List(core, (User)Owner, listsTable.Rows[i]);
                VariableCollection listVariableCollection = template.CreateChild("list_list");

                listVariableCollection.Parse("TITLE", l.Title);
                listVariableCollection.Parse("TYPE", l.Type.ToString());
                listVariableCollection.Parse("ITEMS", core.Functions.LargeIntegerToString(l.Items));

                listVariableCollection.Parse("U_VIEW", core.Uri.BuildListUri(LoggedInMember, l.Path));
                listVariableCollection.Parse("U_DELETE", core.Uri.BuildDeleteListUri(l.Id));
                listVariableCollection.Parse("U_PERMISSIONS", l.Access.AclUri);
                listVariableCollection.Parse("U_EDIT", core.Uri.BuildEditListUri(l.Id));
            }

            DataTable listTypesTable = db.Query("SELECT list_type_id, list_type_title FROM list_types ORDER BY list_type_title ASC");

            SelectBox listTypesSelectBox = new SelectBox("type");

            for (int i = 0; i < listTypesTable.Rows.Count; i++)
            {
                listTypesSelectBox.Add(new SelectBoxItem(((long)listTypesTable.Rows[i]["list_type_id"]).ToString(),
                    (string)listTypesTable.Rows[i]["list_type_title"]));
            }

            listTypesSelectBox.SelectedKey = "1";

            List<string> permissions = new List<string>();
            permissions.Add("Can Read");

            template.Parse("S_LIST_TYPES", listTypesSelectBox);
            //core.Display.ParsePermissionsBox(template, "S_LIST_PERMS", listPermissions, permissions);

            Save(new EventHandler(AccountListsManage_Save));
        }

        void AccountListsManage_Save(object sender, EventArgs e)
        {
            string title = core.Http.Form["title"];
            string slug = core.Http.Form["title"];
            string listAbstract = core.Http.Form["abstract"];
            short type = core.Functions.FormShort("type", 1);
            long listId = core.Functions.FormLong("id", 0);

            // new
            if (listId == 0)
            {
                try
                {
                    List newList = List.Create(core, title, ref slug, listAbstract, type);

                    SetRedirectUri(BuildUri("lists"));
                    core.Display.ShowMessage("List Created", "You have created a new list");
                    return;
                }
                catch (ListTypeNotValidException)
                {
                    core.Display.ShowMessage("List Error", "You submitted invalid information. Go back and try again.");
                    return;
                }
                catch (ListSlugNotUniqueException)
                {
                    core.Display.ShowMessage("List Error", "You have already created a list with the same name, go back and give another name.");
                    return;
                }
            }

            // edit
            if (listId > 0)
            {
                try
                {
                    List list = new List(core, session.LoggedInMember, listId);

                    string oldSlug = list.Path;

                    list.Title = title;
                    list.Abstract = listAbstract;
                    list.Type = type;

                    try
                    {
                        list.Update();

                        // Update page
                        try
                        {
                            Page listPage = new Page(core, core.Session.LoggedInMember, oldSlug, "lists");

                            listPage.Title = list.Title;
                            listPage.Slug = list.Path;

                            listPage.Update();
                        }
                        catch (PageNotFoundException)
                        {
                            Page listPage;
                            try
                            {
                                listPage = new Page(core, core.Session.LoggedInMember, "lists");
                            }
                            catch (PageNotFoundException)
                            {
                                string listSlug = "lists";
                                try
                                {
                                    listPage = Page.Create(core, core.Session.LoggedInMember, "Lists", ref listSlug, 0, "", PageStatus.PageList, 0, Classifications.None);
                                }
                                catch (PageSlugNotUniqueException)
                                {
                                    throw new Exception("Cannot create lists slug.");
                                }
                            }
                            slug = list.Path;
                            Page page = Page.Create(core, core.Session.LoggedInMember, title, ref slug, listPage.Id, "", PageStatus.PageList, 0, Classifications.None);
                        }

                        SetRedirectUri(core.Uri.BuildAccountSubModuleUri(ModuleKey, "lists"));
                        core.Display.ShowMessage("List Saved", "You have saved the list");
                        return;
                    }
                    catch (UnauthorisedToUpdateItemException)
                    {
                        DisplayGenericError();
                        return;
                    }
                    catch (RecordNotUniqueException)
                    {
                        core.Display.ShowMessage("List Error", "You have already created a list with the same name, go back and give another name.");
                        return;
                    }
                }
                catch (InvalidListException)
                {
                    DisplayGenericError();
                    return;
                }
            }
        }

        /// <summary>
        /// Remove an item from a list
        /// </summary>
        void AccountListsManage_Remove(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long itemId = core.Functions.RequestLong("id", 0);
            try
            {
                ListItem item = new ListItem(core, itemId);
                List list = new List(core, LoggedInMember, item.ListId);

                List.Remove(core, item);

                SetRedirectUri(list.Uri);
                core.Display.ShowMessage("List Updated", "You have successfully removed an item from your list.");
            }
            catch (InvalidListItemException)
            {
                DisplayGenericError();
                return;
            }
            catch (UnauthorisedToDeleteItemException)
            {
                DisplayGenericError();
                return;
            }
        }

        /// <summary>
        /// Delete the list itself
        /// </summary>
        void AccountListsManage_Delete(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long listId = core.Functions.RequestLong("id", 0);

            try
            {
                List list = new List(core, core.Session.LoggedInMember, listId);

                try
                {
                    list.Delete();
                }
                catch (UnauthorisedToDeleteItemException)
                {
                    core.Display.ShowMessage("Cannot Delete", "You are unauthorised to delete this list");
                    return;
                }

                try
                {
                    Page listPage = new Page(core, core.Session.LoggedInMember, list.Path, "lists");

                    listPage.Delete();
                }
                catch (PageNotFoundException)
                {
                    // Can ignore
                }

                SetRedirectUri(core.Uri.BuildAccountSubModuleUri(ModuleKey, "lists"));
                core.Display.ShowMessage("List Deleted", "You have deleted a list.");
                return;
            }
            catch (InvalidListException)
            {
                core.Display.ShowMessage("List Error", "You submitted invalid information. Go back and try again. List may have already been deleted.");
                return;
            }
        }

        /// <summary>
        /// Edit a list
        /// </summary>
        void AccountListsManage_Edit(object sender, EventArgs e)
        {
            long listId = core.Functions.RequestLong("id", 0);

            SetTemplate("account_list_edit");

            try
            {
                List list = new List(core, session.LoggedInMember, listId);

                if (!list.Access.Can("EDIT"))
                {
                    DisplayGenericError();
                    return;
                }

                DataTable listTypesTable = db.Query("SELECT list_type_id, list_type_title FROM list_types ORDER BY list_type_title ASC");

                SelectBox listTypesSelectBox = new SelectBox("type");

                for (int i = 0; i < listTypesTable.Rows.Count; i++)
                {
                    listTypesSelectBox.Add(new SelectBoxItem(((long)listTypesTable.Rows[i]["list_type_id"]).ToString(),
                        (string)listTypesTable.Rows[i]["list_type_title"]));
                }

                listTypesSelectBox.SelectedKey = list.Type.ToString();

                template.Parse("S_LIST_TYPES", listTypesSelectBox);
                //core.Display.ParsePermissionsBox(template, "S_LIST_PERMS", list.Permissions, list.PermissibleActions);

                template.Parse("S_LIST_TITLE", list.Title);
                template.Parse("S_LIST_SLUG", list.Path);
                template.Parse("S_LIST_ABSTRACT", list.Abstract);

                template.Parse("S_LIST_ID", list.Id.ToString());
            }
            catch (InvalidListException)
            {
                core.Display.ShowMessage("List Error", "You submitted invalid information. Go back and try again. List may have already been deleted.");
                return;
            }
        }

        /// <summary>
        /// Add an item onto a list
        /// </summary>
        void AccountListsManage_Append(object sender, EventArgs e)
        {
            string text = core.Http.Form["text"];
            string slug = text; // normalised representation
            long listId = core.Functions.FormLong("id", 0);
            bool ajax = false;

            try
            {
                ajax = bool.Parse(core.Http["ajax"]);
            }
            catch { }

            try
            {
                List list = new List(core, LoggedInMember, listId);

                try
                {
                    ListItem item = list.AddNew(text, ref slug);

                    ApplicationEntry ae = new ApplicationEntry(core);

                    // TODO: different list types
                    core.CallingApplication.PublishToFeed(LoggedInMember, list.ItemKey, string.Format("added {0} to list [iurl={2}]{1}[/iurl]", item.Text, list.Title, list.Uri));

                    if (ajax)
                    {
                        core.Ajax.SendRawText("posted", text);

                        if (db != null)
                        {
                            db.CloseConnection();
                        }
                        core.Http.End();
                        return;
                    }
                    else
                    {
                        SetRedirectUri(core.Uri.BuildListUri(LoggedInMember, list.Path));
                        core.Display.ShowMessage("List Updated", "You have successfully appended an item to your list.");
                    }
                }
                catch (UnauthorisedToCreateItemException)
                {
                    core.Ajax.ShowMessage(ajax, "unauthorised", "Unauthorised", "You are unauthorised to append to this list.");
                    return;
                }

            }
            catch (InvalidListException)
            {
                core.Ajax.ShowMessage(ajax, "error", "List Error", "You submitted invalid information. Go back and try again.");
                return;
            }
        }
    }
}
