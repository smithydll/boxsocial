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
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Pages
{
    [AccountModule("pages")]
    public class AccountPages : AccountModule
    {
        public AccountPages(Account account)
            : base(account)
        {
            RegisterSubModule += new RegisterSubModuleHandler(ManagePages);
            RegisterSubModule += new RegisterSubModuleHandler(WritePage);
            RegisterSubModule += new RegisterSubModuleHandler(ManageDrafts);
            RegisterSubModule += new RegisterSubModuleHandler(ManageLists);
        }

        protected override void RegisterModule(Core core, EventArgs e)
        {       
        }

        public override string Name
        {
            get
            {
                return "Pages";
            }
        }

        /*public override string Key
        {
            get
            {
                return "pages";
            }
        }*/

        public override int Order
        {
            get
            {
                return 5;
            }
        }

        private void ManageDrafts(string submodule)
        {
            subModules.Add("drafts", "Draft Pages");
            if (submodule != "drafts") return;

            ManagePages(true);
        }

        private void ManagePages(string submodule)
        {
            subModules.Add("manage", "Manage Pages");
            if (submodule != "manage" && !string.IsNullOrEmpty(submodule)) return;

            ManagePages(false);
        }

        private void ManagePages(bool drafts)
        {
            if (Request.QueryString["delete"] != null)
            {
                try
                {
                    long.Parse(Request.QueryString["id"]);
                    ManagePagesDelete();
                    return;
                }
                catch
                {
                }
            }

            template.SetTemplate("Pages", "account_pages_manage");

            string status = "PUBLISH";

            if (drafts)
            {
                status = "DRAFT";
            }

            DataTable pagesTable = db.Query(string.Format("SELECT upg.page_id, upg.page_parent_path, upg.page_slug, upg.page_title, upg.page_modified_ut FROM user_pages upg WHERE upg.user_id = {0} AND upg.page_status = '{1}' AND upg.page_list_only = 0 ORDER BY upg.page_order",
                loggedInMember.UserId, status));

            for (int i = 0; i < pagesTable.Rows.Count; i++)
            {
                DataRow pagesRow = pagesTable.Rows[i];

                VariableCollection pagesVariableCollection = template.CreateChild("page_list");

                int level = 0;
                if ((string)pagesTable.Rows[i]["page_parent_path"] != "")
                {
                    level = ((string)pagesRow["page_parent_path"]).Split('/').Length;
                }
                string levelString = "";
                for (int j = 0; j < level; j++)
                {
                    levelString += "&mdash; ";
                }

                pagesVariableCollection.Parse("TITLE", levelString + HttpUtility.HtmlEncode((string)pagesRow["page_title"]));
                pagesVariableCollection.Parse("UPDATED", tz.MysqlToString(pagesRow["page_modified_ut"]));
                if ((string)pagesTable.Rows[i]["page_parent_path"] != "")
                {
                    pagesVariableCollection.Parse("U_VIEW", string.Format("/{0}/{1}/{2}",
                        loggedInMember.UserName, (string)pagesRow["page_parent_path"], (string)pagesRow["page_slug"]));
                }
                else
                {
                    pagesVariableCollection.Parse("U_VIEW", string.Format("/{0}/{1}",
                        loggedInMember.UserName, (string)pagesRow["page_slug"]));
                }

                pagesVariableCollection.Parse("U_EDIT", string.Format("/account/pages/write?action=edit&id={0}",
                    (long)pagesRow["page_id"]));
                pagesVariableCollection.Parse("U_DELETE", string.Format("/account/pages/write?action=delete&id={0}",
                    (long)pagesRow["page_id"]));

                if (i % 2 == 0)
                {
                    pagesVariableCollection.Parse("INDEX_EVEN", "TRUE");
                }
            }
        }

        private void ManagePagesDelete()
        {
        }

        #region "Write Module"

        private void WritePage(string submodule)
        {
            subModules.Add("write", "Write New Page");
            if (submodule != "write") return;

            if (Request.Form["publish"] != null || Request.Form["save"] != null)
            {
                WritePageSave();
            }

            template.SetTemplate("Pages", "account_write");

            long pageId = 0;
            long pageParentId = 0;
            byte licenseId = 0;
            ushort pagePermissions = 4369;
            string pageTitle = (Request.Form["title"] != null) ? Request.Form["title"] : "";
            string pageSlug = (Request.Form["slug"] != null) ? Request.Form["slug"] : "";
            string pageText = (Request.Form["post"] != null) ? Request.Form["post"] : "";
            string pagePath = "";
            Classifications pageClassification = Classifications.Everyone;

            try
            {
                if (Request.Form["license"] != null)
                {
                    licenseId = Functions.GetLicense();
                }
                if (Request.Form["id"] != null)
                {
                    pageId = long.Parse(Request.Form["id"]);
                    pagePermissions = Functions.GetPermission();
                }
                if (Request.Form["page-parent"] != null)
                {
                    pageParentId = long.Parse(Request.Form["page-parent"]);
                }
            }
            catch
            {
            }

            if (Request.QueryString["id"] != null)
            {
                try
                {
                    pageId = long.Parse(Request.QueryString["id"]);
                }
                catch
                {
                }
            }

            if (pageId > 0)
            {
                if (Request.QueryString["action"] == "delete")
                {
                    try
                    {
                        Page page = new Page(db, loggedInMember, pageId);

                        if (page.Delete(core, loggedInMember))
                        {
                            SetRedirectUri(AccountModule.BuildModuleUri("pages", "manage"));
                            Display.ShowMessage("Page Deleted", "The page has been deleted from the database.");
                            return;
                        }
                        else
                        {
                            Display.ShowMessage("Error", "Could not delete the page.");
                            return;
                        }
                    }
                    catch (PageNotFoundException)
                    {
                        Display.ShowMessage("Error", "Could not delete the page.");
                        return;
                    }
                }
                else if (Request.QueryString["action"] == "edit")
                {
                    DataTable pageTable = db.Query(string.Format("SELECT upg.page_id, upg.page_text, upg.page_license, upg.page_access, upg.page_title, upg.page_slug, upg.page_parent_path, upg.page_parent_id, upg.page_classification FROM user_pages upg WHERE upg.page_id = {0};",
                        pageId));

                    if (pageTable.Rows.Count == 1)
                    {
                        pageParentId = (long)pageTable.Rows[0]["page_parent_id"];
                        pageTitle = (string)pageTable.Rows[0]["page_title"];
                        pageSlug = (string)pageTable.Rows[0]["page_slug"];
                        pagePermissions = (ushort)pageTable.Rows[0]["page_access"];
                        licenseId = (byte)pageTable.Rows[0]["page_license"];

                        pageText = (string)pageTable.Rows[0]["page_text"];

                        if (string.IsNullOrEmpty((string)pageTable.Rows[0]["page_parent_path"]))
                        {
                            pagePath = string.Format("{0}/", pageSlug);
                        }
                        else
                        {
                            pagePath = string.Format("{0}/{1}/", (string)pageTable.Rows[0]["page_parent_path"], pageSlug);
                        }

                        pageClassification = (Classifications)(byte)pageTable.Rows[0]["page_classification"];
                    }
                }
            }

            DataTable pagesTable = db.Query(string.Format("SELECT page_id, page_slug, page_parent_path FROM user_pages WHERE user_id = {0} ORDER BY page_order ASC;",
                loggedInMember.UserId));

            Dictionary<string, string> pages = new Dictionary<string, string>();
            List<string> disabledItems = new List<string>();
            pages.Add("0", "/");

            foreach (DataRow pageRow in pagesTable.Rows)
            {
                if (string.IsNullOrEmpty((string)pageRow["page_parent_path"]))
                {
                    pages.Add(((long)pageRow["page_id"]).ToString(), (string)pageRow["page_slug"] + "/");
                }
                else
                {
                    pages.Add(((long)pageRow["page_id"]).ToString(), (string)pageRow["page_parent_path"] + "/" + (string)pageRow["page_slug"] + "/");
                }

                if (pageId > 0)
                {
                    if (((string)pageRow["page_parent_path"] + "/").StartsWith(pagePath))
                    {
                        disabledItems.Add(((long)pageRow["page_id"]).ToString());
                    }
                }
            }

            List<string> permissions = new List<string>();
            permissions.Add("Can Read");

            if (pageId > 0)
            {
                disabledItems.Add(pageId.ToString());
            }
            //template.Parse("S_PAGE_PARENT", Functions.BuildSelectBox("page-parent", pages, pageParentId.ToString(), disabledItems));
            //template.Parse("S_PAGE_CLASSIFICATION", Classification.BuildClassificationBox(pageClassification));
            //template.Parse("S_PAGE_LICENSE", ContentLicense.BuildLicenseSelectBox(db, licenseId));
            //template.Parse("S_PAGE_PERMS", Functions.BuildPermissionsBox(pagePermissions, permissions));
            Display.ParseLicensingBox(template, "S_PAGE_LICENSE", licenseId);
            Display.ParseClassification(template, "S_PAGE_CLASSIFICATION", pageClassification);
            Display.ParseSelectBox(template, "S_PAGE_PARENT", "page-parent", pages, pageParentId.ToString(), disabledItems);

            Display.ParsePermissionsBox(template, "S_PAGE_PERMS", pagePermissions, permissions);

            template.Parse("S_TITLE", pageTitle);
            template.Parse("S_SLUG", pageSlug);
            template.Parse("S_PAGE_TEXT", pageText);
            template.Parse("S_ID", pageId.ToString());
        }

        private void WritePageSave()
        {
            string slug = Request.Form["slug"];
            string title = Request.Form["title"];
            string pageBody = Request.Form["post"];
            long parent = 0;
            ushort order = 0;
            ushort oldOrder = 0;
            long pageId = 0;
            bool parentChanged = false;
            bool titleChanged = false;
            PageStatus status = PageStatus.Publish;

            if (Request.Form["publish"] != null)
            {
                status = PageStatus.Publish;
            }

            if (Request.Form["save"] != null)
            {
                status = PageStatus.Draft;
            }

            pageId = Functions.FormLong("id", 0);
            parent = Functions.FormLong("page-parent", 0);

            try
            {
                if (pageId > 0)
                {
                    try
                    {
                        Page page = new Page(core.db, core.session.LoggedInMember, pageId);

                        page.Update(core, core.session.LoggedInMember, title, ref slug, parent, pageBody, status, Functions.GetPermission(), Functions.GetLicense(), Classification.RequestClassification());
                    }
                    catch (PageNotFoundException)
                    {
                    }
                }
                else
                {
                    Page.Create(core, core.session.LoggedInMember, title, ref slug, parent, pageBody, status, Functions.GetPermission(), Functions.GetLicense(), Classification.RequestClassification());
                }
            }
            catch (PageTitleNotValidException)
            {
                SetError("You must give the page a title.");
                return;
            }
            catch (PageSlugNotValidException)
            {
                SetError("You must specify a page slug.");
                return;
            }
            catch (PageSlugNotUniqueException)
            {
                SetError("You must give your page a different name.");
                return;
            }
            catch (PageContentEmptyException)
            {
                SetError("You cannot save empty pages. You must post some content.");
                return;
            }
            catch (PageOwnParentException)
            {
                SetError("You cannot have a page as it's own parent.");
                return;
            }

            if (status == PageStatus.Draft)
            {
                SetRedirectUri(AccountModule.BuildModuleUri("pages", "drafts"));
                Display.ShowMessage("Draft Saved", "Your draft has been saved.");
            }
            else
            {
                SetRedirectUri(AccountModule.BuildModuleUri("pages", "manage"));
                Display.ShowMessage("Page Published", "Your page has been published");
            }
        }

        #endregion

        public void ManageLists(string submodule)
        {
            subModules.Add("lists", "Manage Lists");
            if (submodule != "lists") return;

            if (Request.Form["save"] != null)
            {
                ManageListsSave();
                return;
            }
            else if (Request.QueryString["mode"] == "remove")
            {
                ManageListRemove();
                return;
            }
            else if (Request.QueryString["mode"] == "delete")
            {
                ManageListDelete();
                return;
            }
            else if (Request.QueryString["mode"] == "edit")
            {
                ManageListEdit();
                return;
            }

            ushort listPermissions = 4369;

            template.SetTemplate("Pages", "account_lists");

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

            //template.Parse("S_LIST_TYPES", Functions.BuildSelectBox("type", listTypes, "1"));
            //template.Parse("S_LIST_PERMS", Functions.BuildPermissionsBox(listPermissions, permissions));
            Display.ParseSelectBox(template, "S_LIST_TYPES", "type", listTypes, "1");
            Display.ParsePermissionsBox(template, "S_LIST_PERMS", listPermissions, permissions);
        }

        public void ManageListsSave()
        {
            if (Request.Form["mode"] == "append")
            {
                ManageListAppend();
                return;
            }

            string title = Request.Form["title"];
            string slug = Request.Form["title"];
            string listAbstract = Request.Form["abstract"];
            short type = 1;
            long listId = 0;

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
            slug = Regex.Replace(normalisedSlug, @"([\W]+)", "-");

            try
            {
                // edit page
                listId = long.Parse(Request.Form["id"]);
                type = short.Parse(Request.Form["type"]);

                DataTable pageTable = db.Query(string.Format("SELECT list_id FROM user_lists WHERE list_id = {0} AND user_id = {1};",
                    listId, loggedInMember.UserId));

                if (pageTable.Rows.Count == 1)
                {
                }
                else
                {
                    Display.ShowMessage("List Error", "You submitted invalid information. Go back and try again.");
                    return;
                }
            }
            catch (Exception)
            {
            }

            // new
            if (listId == 0)
            {
                // check that a list with the slug given does not already exist
                if (db.Query(string.Format("SELECT list_path FROM user_lists WHERE user_id = {0} AND list_path = '{1}';",
                    loggedInMember.UserId, Mysql.Escape(slug))).Rows.Count == 0)
                {
                    // verify that the type exists
                    DataTable listTypeTable = db.Query(string.Format("SELECT list_type_id FROM list_types WHERE list_type_id = {0};",
                        type));
                    if (listTypeTable.Rows.Count == 1)
                    {
                        listId = db.UpdateQuery(string.Format("INSERT INTO user_lists (user_id, list_title, list_path, list_type, list_abstract, list_access) VALUES ({0}, '{1}', '{2}', {3}, '{4}', {5});",
                            loggedInMember.UserId, Mysql.Escape(title), Mysql.Escape(slug), type, Mysql.Escape(listAbstract), Functions.GetPermission()));

                        SetRedirectUri(AccountModule.BuildModuleUri("pages", "lists"));
                        Display.ShowMessage("List Created", "You have created a new list");
                    }
                    else
                    {
                        Display.ShowMessage("List Error", "You submitted invalid information. Go back and try again.");
                        return;
                    }
                }
                else
                {
                    Display.ShowMessage("List Error", "You have already created a list with the same name, go back and give another name.");
                    return;
                }
            }

            // edit
            if (listId > 0)
            {
                // check that a list with the slug given does not already exist, ignoring itself
                if (db.Query(string.Format("SELECT list_path FROM user_lists WHERE user_id = {0} AND list_path = '{1}' AND list_id <> {2};",
                    loggedInMember.UserId, Mysql.Escape(slug), listId)).Rows.Count == 0)
                {
                    db.UpdateQuery(string.Format("UPDATE user_lists SET list_title = '{1}', list_access = {2}, list_path = '{3}', list_abstract = '{4}', list_type = {5} WHERE list_id = {0}",
                        listId, Mysql.Escape(title), Functions.GetPermission(), Mysql.Escape(slug), Mysql.Escape(listAbstract), type));

                    SetRedirectUri(AccountModule.BuildModuleUri("pages", "lists"));
                    Display.ShowMessage("List Saved", "You have saved the list");
                }
                else
                {
                    Display.ShowMessage("List Error", "You have already created a list with the same name, go back and give another name.");
                    return;
                }
            }
        }

        /// <summary>
        /// Edit a list
        /// </summary>
        public void ManageListEdit()
        {
            long listId = 0;

            string listTitle = "";
            string listSlug = "";
            string listAbstract = "";
            ushort listPermissions = 0x1111;
            short listType = 1;

            try
            {
                listId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("List Error", "You submitted invalid information. Go back and try again.");
                return;
            }

            template.SetTemplate("Pages", "account_list_edit");

            DataTable listTable = db.Query(string.Format("SELECT list_id, list_title, list_access, list_path, list_abstract, list_type FROM user_lists WHERE user_id = {0} AND list_id = {1};",
                loggedInMember.UserId, listId));

            if (listTable.Rows.Count == 1)
            {
                listTitle = (string)listTable.Rows[0]["list_title"];
                listPermissions = (ushort)listTable.Rows[0]["list_access"];
                listSlug = (string)listTable.Rows[0]["list_path"];
                listAbstract = (string)listTable.Rows[0]["list_abstract"];
                listType = (short)listTable.Rows[0]["list_type"];

                DataTable listTypesTable = db.Query("SELECT list_type_id, list_type_title FROM list_types ORDER BY list_type_title ASC");

                Dictionary<string, string> listTypes = new Dictionary<string, string>();

                for (int i = 0; i < listTypesTable.Rows.Count; i++)
                {
                    listTypes.Add(((short)listTypesTable.Rows[i]["list_type_id"]).ToString(),
                        (string)listTypesTable.Rows[i]["list_type_title"]);
                }

                List<string> permissions = new List<string>();
                permissions.Add("Can Read");

                //template.Parse("S_LIST_TYPES", Functions.BuildSelectBox("type", listTypes, listType.ToString()));
                //template.Parse("S_LIST_PERMS", Functions.BuildPermissionsBox(listPermissions, permissions));
                Display.ParseSelectBox(template, "S_LIST_TYPES", "type", listTypes, listType.ToString());
                Display.ParsePermissionsBox(template, "S_LIST_PERMS", listPermissions, permissions);

                template.Parse("S_LIST_TITLE", listTitle);
                template.Parse("S_LIST_SLUG", listSlug);
                template.Parse("S_LIST_ABSTRACT", listAbstract);

                template.Parse("S_LIST_ID", ((long)listTable.Rows[0]["list_id"]).ToString());
            }
            else
            {
                Display.ShowMessage("List Error", "You submitted invalid information. Go back and try again. List may have already been deleted.");
                return;
            }
        }

        /// <summary>
        /// Delete the list itself
        /// </summary>
        public void ManageListDelete()
        {
            AuthoriseRequestSid();

            long listId = 0;

            try
            {
                listId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("List Error", "You submitted invalid information. Go back and try again.");
                return;
            }

            DataTable listTable = db.Query(string.Format("SELECT list_items FROM user_lists WHERE user_id = {0} AND list_id = {1};",
                loggedInMember.UserId, listId));

            if (listTable.Rows.Count == 1)
            {
                db.BeginTransaction();
                db.UpdateQuery(string.Format("DELETE FROM list_items WHERE list_id = {0}",
                    listId));

                db.UpdateQuery(string.Format("DELETE FROM user_lists WHERE user_id = {0} AND list_id = {1}",
                    loggedInMember.UserId, listId));

                SetRedirectUri(AccountModule.BuildModuleUri("pages", "lists"));
                Display.ShowMessage("List Deleted", "You have deleted a list.");
                return;
            }
            else
            {
                Display.ShowMessage("List Error", "You submitted invalid information. Go back and try again. List may have already been deleted.");
                return;
            }
        }

        /// <summary>
        /// Add an item onto a list
        /// </summary>
        public void ManageListAppend()
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
                List list = new List(db, loggedInMember, listId);

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

        /// <summary>
        /// Remove an item from a list
        /// </summary>
        public void ManageListRemove()
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

    }
}
