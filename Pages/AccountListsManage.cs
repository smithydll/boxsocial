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

        public AccountListsManage()
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

            DataTable listsTable = db.Query(string.Format("SELECT list_id, list_title, list_items, list_type_title, list_path FROM user_lists INNER JOIN list_types ON list_type_id = list_type WHERE user_id = {0}",
                loggedInMember.UserId));

            for (int i = 0; i < listsTable.Rows.Count; i++)
            {
                VariableCollection listVariableCollection = template.CreateChild("list_list");

                listVariableCollection.Parse("TITLE", (string)listsTable.Rows[i]["list_title"]);
                listVariableCollection.Parse("TYPE", (string)listsTable.Rows[i]["list_type_title"]);
                listVariableCollection.Parse("ITEMS", ((uint)listsTable.Rows[i]["list_items"]).ToString());

                listVariableCollection.Parse("U_VIEW", Linker.BuildListUri(loggedInMember, (string)listsTable.Rows[i]["list_path"]));
                listVariableCollection.Parse("U_DELETE", Linker.BuildDeleteListUri((long)listsTable.Rows[i]["list_id"]));
                listVariableCollection.Parse("U_EDIT", Linker.BuildEditListUri((long)listsTable.Rows[i]["list_id"]));
            }

            DataTable listTypesTable = db.Query("SELECT list_type_id, list_type_title FROM list_types ORDER BY list_type_title ASC");

            Dictionary<string, string> listTypes = new Dictionary<string, string>();

            for (int i = 0; i < listTypesTable.Rows.Count; i++)
            {
                listTypes.Add(((short)listTypesTable.Rows[i]["list_type_id"]).ToString(),
                    (string)listTypesTable.Rows[i]["list_type_title"]);
            }

            List<string> permissions = new List<string>();
            permissions.Add("Can Read");

            Display.ParseSelectBox(template, "S_LIST_TYPES", "type", listTypes, "1");
            Display.ParsePermissionsBox(template, "S_LIST_PERMS", listPermissions, permissions);

            Save(new EventHandler(AccountListsManage_Save));
        }

        void AccountListsManage_Save(object sender, EventArgs e)
        {
            string title = Request.Form["title"];
            string slug = Request.Form["title"];
            string listAbstract = Request.Form["abstract"];
            short type = Functions.FormShort("type", 1);
            long listId = Functions.FormLong("id", 0);

            // new
            if (listId == 0)
            {
                try
                {
                    List newList = List.Create(core, title, ref slug, listAbstract, type, Functions.GetPermission());

                    SetRedirectUri(AccountModule.BuildModuleUri("pages", "lists"));
                    Display.ShowMessage("List Created", "You have created a new list");
                    return;
                }
                catch (ListTypeNotValidException)
                {
                    Display.ShowMessage("List Error", "You submitted invalid information. Go back and try again.");
                    return;
                }
                catch (ListSlugNotUniqueException)
                {
                    Display.ShowMessage("List Error", "You have already created a list with the same name, go back and give another name.");
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
                    list.Permissions = Functions.GetPermission();
                    list.Abstract = listAbstract;
                    list.Type = type;

                    try
                    {
                        list.Update();

                        // Update page
                        try
                        {
                            Page listPage = new Page(core, core.session.LoggedInMember, oldSlug, "lists");

                            listPage.Title = list.Title;
                            listPage.Slug = list.Path;

                            listPage.Update();
                        }
                        catch (PageNotFoundException)
                        {
                            Page listPage;
                            try
                            {
                                listPage = new Page(core, core.session.LoggedInMember, "lists");
                            }
                            catch (PageNotFoundException)
                            {
                                string listSlug = "lists";
                                try
                                {
                                    listPage = Page.Create(core, core.session.LoggedInMember, "Lists", ref listSlug, 0, "", PageStatus.PageList, 0x1111, 0, Classifications.None);
                                }
                                catch (PageSlugNotUniqueException)
                                {
                                    throw new Exception("Cannot create lists slug.");
                                }
                            }
                            slug = list.Path;
                            Page page = Page.Create(core, core.session.LoggedInMember, title, ref slug, listPage.Id, "", PageStatus.PageList, 0x1111, 0, Classifications.None);
                        }

                        SetRedirectUri(AccountModule.BuildModuleUri("pages", "lists"));
                        Display.ShowMessage("List Saved", "You have saved the list");
                        return;
                    }
                    catch (UnauthorisedToUpdateItemException)
                    {
                        DisplayGenericError();
                        return;
                    }
                    catch (RecordNotUniqueException)
                    {
                        Display.ShowMessage("List Error", "You have already created a list with the same name, go back and give another name.");
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

            long itemId = 0;
            long listId = 0;

            try
            {
                itemId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("List Error", "You submitted invalid information. Go back and try again.");
                return;
            }

            DataTable listItemTable = db.Query(string.Format("SELECT li.list_id, ul.list_path FROM list_items li INNER JOIN user_lists ul ON ul.list_id = li.list_id WHERE li.list_item_id = {0};",
                itemId));

            if (listItemTable.Rows.Count == 1)
            {
                listId = (long)listItemTable.Rows[0]["list_id"];

                db.BeginTransaction();
                db.UpdateQuery(string.Format("DELETE FROM list_items WHERE list_item_id = {0} AND list_id = {1}",
                    itemId, listId));

                db.UpdateQuery(string.Format("UPDATE user_lists SET list_items = list_items - 1 WHERE user_id = {0} AND list_id = {1}",
                        loggedInMember.UserId, listId));

                SetRedirectUri(Linker.BuildListUri(loggedInMember, (string)listItemTable.Rows[0]["list_path"]));
                Display.ShowMessage("List Updated", "You have successfully removed an item from your list.");
            }
            else
            {
                Display.ShowMessage("List Error", "You submitted invalid information. Go back and try again.");
                return;
            }
        }

        /// <summary>
        /// Delete the list itself
        /// </summary>
        void AccountListsManage_Delete(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long listId = Functions.RequestLong("id", 0);

            try
            {
                List list = new List(core, core.session.LoggedInMember, listId);

                try
                {
                    list.Delete();
                }
                catch (UnauthorisedToDeleteItemException)
                {
                    Display.ShowMessage("Cannot Delete", "You are unauthorised to delete this list");
                    return;
                }

                try
                {
                    Page listPage = new Page(core, core.session.LoggedInMember, list.Path, "lists");

                    listPage.Delete();
                }
                catch (PageNotFoundException)
                {
                    // Can ignore
                }

                SetRedirectUri(AccountModule.BuildModuleUri("pages", "lists"));
                Display.ShowMessage("List Deleted", "You have deleted a list.");
                return;
            }
            catch (InvalidListException)
            {
                Display.ShowMessage("List Error", "You submitted invalid information. Go back and try again. List may have already been deleted.");
                return;
            }
        }

        /// <summary>
        /// Edit a list
        /// </summary>
        void AccountListsManage_Edit(object sender, EventArgs e)
        {
            long listId = Functions.RequestLong("id", 0);

            SetTemplate("account_list_edit");

            try
            {
                List list = new List(core, session.LoggedInMember, listId);

                if (!list.Access.CanEdit)
                {
                    DisplayGenericError();
                    return;
                }

                DataTable listTypesTable = db.Query("SELECT list_type_id, list_type_title FROM list_types ORDER BY list_type_title ASC");

                Dictionary<string, string> listTypes = new Dictionary<string, string>();

                for (int i = 0; i < listTypesTable.Rows.Count; i++)
                {
                    listTypes.Add(((short)listTypesTable.Rows[i]["list_type_id"]).ToString(),
                        (string)listTypesTable.Rows[i]["list_type_title"]);
                }

                Display.ParseSelectBox(template, "S_LIST_TYPES", "type", listTypes, list.Type.ToString());
                Display.ParsePermissionsBox(template, "S_LIST_PERMS", list.Permissions, list.PermissibleActions);

                template.Parse("S_LIST_TITLE", list.Title);
                template.Parse("S_LIST_SLUG", list.Path);
                template.Parse("S_LIST_ABSTRACT", list.Abstract);

                template.Parse("S_LIST_ID", list.Id.ToString());
            }
            catch (InvalidListException)
            {
                Display.ShowMessage("List Error", "You submitted invalid information. Go back and try again. List may have already been deleted.");
                return;
            }
        }

        /// <summary>
        /// Add an item onto a list
        /// </summary>
        void AccountListsManage_Append(object sender, EventArgs e)
        {
            string text = Request.Form["text"];
            string slug = text; // normalised representation
            long listId = 0;
            bool ajax = false;

            try
            {
                ajax = bool.Parse(Request["ajax"]);
            }
            catch { }

            try
            {
                listId = long.Parse(Request.Form["id"]);
            }
            catch
            {
                Ajax.ShowMessage(ajax, "error", "List Error", "You submitted invalid information. Go back and try again.");
                return;
            }

            // normalise slug if it has been fiddeled with
            slug = slug.ToLower().Normalize(NormalizationForm.FormD);
            string normalisedSlug = "";

            for (int i = 0; i < slug.Length; i++)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(slug[i]) != UnicodeCategory.NonSpacingMark)
                {
                    normalisedSlug += slug[i];
                }
            }
            // we want to be a little less stringent with list items to allow for some punctuation of being of importance
            slug = Regex.Replace(normalisedSlug, @"([^\w\+\&\*\(\)\=\:\?\-\#\@\!\$]+)", "-");

            // check that the list exists for the given user
            try
            {
                List list = new List(core, loggedInMember, listId);

                DataTable listItemTextTable = db.Query(string.Format("SELECT list_item_text_id FROM list_items_text WHERE list_item_text_normalised = '{0}';",
                    Mysql.Escape(slug)));

                if (listItemTextTable.Rows.Count == 1)
                {
                    // already exists
                    db.BeginTransaction();
                    db.UpdateQuery(string.Format("INSERT INTO list_items (list_id, list_item_text_id) VALUES ({0}, {1})",
                        listId, (long)listItemTextTable.Rows[0]["list_item_text_id"]));
                }
                else // insert new
                {
                    db.BeginTransaction();
                    long listItemTextId = db.UpdateQuery(string.Format("INSERT INTO list_items_text (list_item_text, list_item_text_normalised) VALUES ('{0}', '{1}');",
                        Mysql.Escape(text), Mysql.Escape(slug)));

                    db.UpdateQuery(string.Format("INSERT INTO list_items (list_id, list_item_text_id) VALUES ({0}, {1})",
                        listId, listItemTextId));
                }

                db.UpdateQuery(string.Format("UPDATE user_lists SET list_items = list_items + 1 WHERE user_id = {0} AND list_id = {1}",
                    loggedInMember.UserId, listId));

                ApplicationEntry ae = new ApplicationEntry(core);

                // TODO: different list types
                AppInfo.Entry.PublishToFeed(loggedInMember, string.Format("added {0} to list {1}", text, list.Title));

                if (ajax)
                {
                    Ajax.SendRawText("posted", text);

                    if (db != null)
                    {
                        db.CloseConnection();
                    }
                    Response.End();
                    return;
                }
                else
                {
                    SetRedirectUri(Linker.BuildListUri(loggedInMember, list.Path));
                    Display.ShowMessage("List Updated", "You have successfully appended an item to your list.");
                }

            }
            catch (InvalidListException)
            {
                Ajax.ShowMessage(ajax, "error", "List Error", "You submitted invalid information. Go back and try again.");
                return;
            }
        }
    }
}
