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
using System.Web.Security;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Pages
{
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

        public override string Key
        {
            get
            {
                return "pages";
            }
        }

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

            template.SetTemplate("account_pages_manage.html");

            string status = "PUBLISH";

            if (drafts)
            {
                status = "DRAFT";
            }

            DataTable pagesTable = db.SelectQuery(string.Format("SELECT upg.page_id, upg.page_parent_path, upg.page_slug, upg.page_title, upg.page_modified_ut FROM user_pages upg WHERE upg.user_id = {0} AND upg.page_status = '{1}' ORDER BY upg.page_order",
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

                pagesVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode(levelString + (string)pagesRow["page_title"]));
                pagesVariableCollection.ParseVariables("UPDATED", HttpUtility.HtmlEncode(tz.MysqlToString(pagesRow["page_modified_ut"])));
                if ((string)pagesTable.Rows[i]["page_parent_path"] != "")
                {
                    pagesVariableCollection.ParseVariables("U_VIEW", HttpUtility.HtmlEncode(string.Format("/{0}/{1}/{2}",
                        loggedInMember.UserName, (string)pagesRow["page_parent_path"], (string)pagesRow["page_slug"])));
                }
                else
                {
                    pagesVariableCollection.ParseVariables("U_VIEW", HttpUtility.HtmlEncode(string.Format("/{0}/{1}",
                        loggedInMember.UserName, (string)pagesRow["page_slug"])));
                }

                pagesVariableCollection.ParseVariables("U_EDIT", HttpUtility.HtmlEncode(string.Format("/account/?module=pages&amp;sub=write&amp;action=edit&amp;id={0}",
                    (long)pagesRow["page_id"])));
                pagesVariableCollection.ParseVariables("U_DELETE", HttpUtility.HtmlEncode(string.Format("/account/?module=pages&amp;sub=write&amp;action=delete&amp;id={0}",
                    (long)pagesRow["page_id"])));

                if (i % 2 == 0)
                {
                    pagesVariableCollection.ParseVariables("INDEX_EVEN", "TRUE");
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

            template.SetTemplate("account_write.html");

            long pageId = 0;
            long pageParentId = 0;
            byte licenseId = 0;
            ushort pagePermissions = 4369;
            string pageTitle = (Request.Form["title"] != null) ? Request.Form["title"] : "";
            string pageSlug = (Request.Form["slug"] != null) ? Request.Form["slug"] : "";
            string pageText = (Request.Form["post"] != null) ? Request.Form["post"] : "";
            string pagePath = "";

            try
            {
                if (Request.Form["license"] != null)
                {
                    licenseId = byte.Parse(Request.Form["license"]);
                }
                if (Request.Form["id"] != null)
                {
                    pageId = long.Parse(Request.Form["id"]);
                    pagePermissions = Functions.GetPermission(Request);
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
                    DataTable pageTable = db.SelectQuery(string.Format("SELECT upg.page_order FROM user_pages upg WHERE upg.page_id = {0} AND upg.user_id = {1};",
                        pageId, loggedInMember.UserId));

                    if (pageTable.Rows.Count == 1)
                    {

                        db.UpdateQuery(string.Format("UPDATE user_pages SET page_order = page_order - 1 WHERE page_order >= {0} AND user_id = {1}",
                            (ushort)pageTable.Rows[0]["page_order"], loggedInMember.UserId), true);

                        db.UpdateQuery(string.Format("DELETE FROM user_pages WHERE user_id = {0} AND page_id = {1};",
                            loggedInMember.UserId, pageId), false);

                        template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode("/account/?module=pages&sub=manage"));
                        Display.ShowMessage(core, "Page Deleted", "The page has been deleted from the database.");
                        return;
                    }

                }
                else if (Request.QueryString["action"] == "edit")
                {
                    DataTable pageTable = db.SelectQuery(string.Format("SELECT upg.page_id, upg.page_text, upg.page_license, upg.page_access, upg.page_title, upg.page_slug, upg.page_parent_path, upg.page_parent_id FROM user_pages upg WHERE upg.page_id = {0};",
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
                    }
                }
            }

            DataTable pagesTable = db.SelectQuery(string.Format("SELECT page_id, page_slug, page_parent_path FROM user_pages WHERE user_id = {0} ORDER BY page_order ASC;",
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
            template.ParseVariables("S_PAGE_PARENT", Functions.BuildSelectBox("page-parent", pages, pageParentId.ToString(), disabledItems));
            template.ParseVariables("S_PAGE_LICENSE", ContentLicense.BuildLicenseSelectBox(db, licenseId));
            template.ParseVariables("S_PAGE_PERMS", Functions.BuildPermissionsBox(pagePermissions, permissions));

            template.ParseVariables("S_TITLE", HttpUtility.HtmlEncode(pageTitle));
            template.ParseVariables("S_SLUG", HttpUtility.HtmlEncode(pageSlug));
            template.ParseVariables("S_PAGE_TEXT", HttpUtility.HtmlEncode(pageText));
            template.ParseVariables("S_ID", HttpUtility.HtmlEncode(pageId.ToString()));
        }

        private void WritePageSave()
        {
            long parent = 0;
            string slug = Request.Form["slug"];
            string title = Request.Form["title"];
            string pageBody = Request.Form["post"];
            string parentPath = "";
            ushort order = 0;
            ushort oldOrder = 0;
            short license = 0;
            long pageId = 0;
            bool parentChanged = false;
            bool titleChanged = false;
            string status = "PUBLISH";

            if (string.IsNullOrEmpty(slug))
            {
                slug = title;
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
            slug = Regex.Replace(normalisedSlug, @"([\W]+)", "-");

            try
            {
                parent = long.Parse(Request.Form["page-parent"]);
                license = short.Parse(Request.Form["license"]);
            }
            catch
            {
            }

            if (Request.Form["publish"] != null)
            {
                status = "PUBLISH";
            }

            if (Request.Form["save"] != null)
            {
                status = "DRAFT";
            }

            // validate title;

            if (string.IsNullOrEmpty(title))
            {
                template.ParseVariables("ERROR", "You must give the page a title.");
                return;
            }

            if (string.IsNullOrEmpty(slug))
            {
                template.ParseVariables("ERROR", "You must specify a page slug.");
                return;
            }

            if ((!Functions.CheckPageNameValid(slug)) && parent == 0)
            {
                template.ParseVariables("ERROR", "You must give your page a different name.");
                return;
            }

            if (string.IsNullOrEmpty(pageBody))
            {
                template.ParseVariables("ERROR", "You cannot save empty pages. You must post some content.");
                return;
            }

            try
            {
                // edit page
                pageId = long.Parse(Request.Form["id"]);

                DataTable pageTable = db.SelectQuery(string.Format("SELECT page_title, page_parent_id, page_order FROM user_pages WHERE page_id = {0} AND user_id = {1};",
                    pageId, loggedInMember.UserId));

                if (pageTable.Rows.Count == 1)
                {
                    if ((long)pageTable.Rows[0]["page_parent_id"] != parent)
                    {
                        parentChanged = true;
                    }

                    if ((string)pageTable.Rows[0]["page_title"] != title)
                    {
                        titleChanged = true;
                    }

                    order = (ushort)pageTable.Rows[0]["page_order"];
                    oldOrder = (ushort)pageTable.Rows[0]["page_order"];
                }
            }
            catch (Exception)
            {
            }

            // now page page id, do more checks

            if (pageId > 0 && pageId == parent)
            {
                template.ParseVariables("ERROR", "You cannot have a page as it's own parent.");
                return;
            }

            DataTable pagesTable = db.SelectQuery(string.Format("SELECT page_title FROM user_pages WHERE user_id = {0} AND page_id <> {1} AND page_slug = '{2}' AND page_parent_id = {3}",
                loggedInMember.UserId, pageId, Mysql.Escape(slug), parent));

            if (pagesTable.Rows.Count > 0)
            {
                template.ParseVariables("ERROR", "You must give your page a different name, a page already has that name.");
                return;
            }

            if (pageId == 0 || (pageId > 0 && (parentChanged || titleChanged)))
            {
                DataTable parentTable = db.SelectQuery(string.Format("SELECT page_id, page_slug, page_parent_path, page_order FROM user_pages WHERE user_id = {0} AND page_id = {1}",
                    loggedInMember.UserId, parent));

                if (parentTable.Rows.Count == 1)
                {
                    if (string.IsNullOrEmpty((string)parentTable.Rows[0]["page_parent_path"]))
                    {
                        parentPath = (string)parentTable.Rows[0]["page_slug"];
                    }
                    else
                    {
                        parentPath = (string)parentTable.Rows[0]["page_parent_path"] + "/" + (string)parentTable.Rows[0]["page_slug"];
                    }
                }
                else
                {
                    // we couldn't find a parent so set to zero
                    parent = 0;
                }

                DataTable orderTable = db.SelectQuery(string.Format("SELECT page_id, page_order FROM user_pages WHERE page_id <> {3} AND page_title > '{0}' AND page_parent_path = '{1}' AND user_id = {2} ORDER BY page_title ASC LIMIT 1",
                    Mysql.Escape(title), Mysql.Escape(parentPath), loggedInMember.UserId, pageId));

                if (orderTable.Rows.Count == 1)
                {
                    order = (ushort)orderTable.Rows[0]["page_order"];

                    if (order == oldOrder + 1 && pageId > 0)
                    {
                        order = oldOrder;
                    }
                }
                else if (parent > 0 && parentTable.Rows.Count == 1)
                {
                    order = (ushort)((ushort)parentTable.Rows[0]["page_order"] + 1);
                }
                else
                {
                    orderTable = db.SelectQuery(string.Format("SELECT MAX(page_order) + 1 as max_order FROM user_pages WHERE user_id = {0} AND page_id <> {1}",
                        loggedInMember.UserId, pageId));

                    if (orderTable.Rows.Count == 1)
                    {
                        if (!(orderTable.Rows[0]["max_order"] is DBNull))
                        {
                            order = (ushort)(ulong)orderTable.Rows[0]["max_order"];
                        }
                    }
                }
            }

            // save new
            if (pageId == 0)
            {

                if (order < 0)
                {
                    order = 0;
                }

                db.UpdateQuery(string.Format("UPDATE user_pages SET page_order = page_order + 1 WHERE page_order >= {0} AND user_id = {1}",
                    order, loggedInMember.UserId), true);

                db.UpdateQuery(string.Format("INSERT INTO user_pages (user_id, page_slug, page_parent_path, page_date_ut, page_title, page_modified_ut, page_ip, page_text, page_license, page_access, page_order, page_parent_id, page_status) VALUES ({0}, '{1}', '{2}', UNIX_TIMESTAMP(), '{3}', UNIX_TIMESTAMP(), '{4}', '{5}', {6}, {7}, {8}, {9}, '{10}')",
                    loggedInMember.UserId, Mysql.Escape(slug), Mysql.Escape(parentPath), Mysql.Escape(title), Mysql.Escape(session.IPAddress.ToString()), Mysql.Escape(pageBody), license, Functions.GetPermission(Request), order, parent, Mysql.Escape(status)), false);

                if (status == "DRAFT")
                {
                    template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode("/account/?module=pages&sub=drafts"));
                    Display.ShowMessage(core, "New Draft Saved", "Your draft has been saved.");
                }
                else
                {
                    template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode("/account/?module=pages&sub=manage"));
                    Display.ShowMessage(core, "New Page Published", "Your page has been published");
                }

            }

            if (pageId > 0)
            {

                if (order != oldOrder)
                {
                    db.UpdateQuery(string.Format("UPDATE user_pages SET page_order = page_order - 1 WHERE page_order >= {0} AND user_id = {1}",
                        oldOrder, loggedInMember.UserId), true);

                    db.UpdateQuery(string.Format("UPDATE user_pages SET page_order = page_order + 1 WHERE page_order >= {0} AND user_id = {1}",
                        order, loggedInMember.UserId), true);
                }

                string changeParent = "";
                string changeTitle = "";

                if (parentChanged)
                {
                    changeParent = string.Format("page_parent_path = '{0}', page_parent_id = {1},",
                        Mysql.Escape(parentPath), parent);
                }

                if (titleChanged)
                {
                    changeTitle = string.Format("page_title = '{0}',",
                        title);
                }

                db.UpdateQuery(string.Format("UPDATE user_pages SET {0} {1} page_slug = '{2}', page_modified_ut = UNIX_TIMESTAMP(), page_ip = '{3}', page_text = '{4}', page_license = {5}, page_access = {6}, page_order = {7}, page_status = '{10}' WHERE page_id = {8} AND user_id = {9};",
                    changeParent, changeTitle, Mysql.Escape(slug), session.IPAddress.ToString(), Mysql.Escape(pageBody), license, Functions.GetPermission(Request), order, pageId, loggedInMember.UserId, status), false);
            }

            if (status == "DRAFT")
            {
                template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode("/account/?module=pages&sub=drafts"));
                Display.ShowMessage(core, "Draft Saved", "Your draft has been saved.");
            }
            else
            {
                template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode("/account/?module=pages&sub=manage"));
                Display.ShowMessage(core, "Page Published", "Your page has been published");
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

            template.SetTemplate("account_lists.html");

            DataTable listsTable = db.SelectQuery(string.Format("SELECT list_id, list_title, list_items, list_type_title, list_path FROM user_lists INNER JOIN list_types ON list_type_id = list_type WHERE user_id = {0}",
                loggedInMember.UserId));

            for (int i = 0; i < listsTable.Rows.Count; i++)
            {
                VariableCollection listVariableCollection = template.CreateChild("list_list");

                listVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode((string)listsTable.Rows[i]["list_title"]));
                listVariableCollection.ParseVariables("TYPE", HttpUtility.HtmlEncode((string)listsTable.Rows[i]["list_type_title"]));
                listVariableCollection.ParseVariables("ITEMS", HttpUtility.HtmlEncode(((uint)listsTable.Rows[i]["list_items"]).ToString()));

                listVariableCollection.ParseVariables("U_VIEW", HttpUtility.HtmlEncode(ZzUri.BuildListUri(loggedInMember, (string)listsTable.Rows[i]["list_path"])));
                listVariableCollection.ParseVariables("U_DELETE", HttpUtility.HtmlEncode(ZzUri.BuildDeleteListUri((long)listsTable.Rows[i]["list_id"])));
                listVariableCollection.ParseVariables("U_EDIT", HttpUtility.HtmlEncode(ZzUri.BuildEditListUri((long)listsTable.Rows[i]["list_id"])));
            }

            DataTable listTypesTable = db.SelectQuery("SELECT list_type_id, list_type_title FROM list_types ORDER BY list_type_title ASC");

            Dictionary<string, string> listTypes = new Dictionary<string, string>();

            for (int i = 0; i < listTypesTable.Rows.Count; i++)
            {
                listTypes.Add(((short)listTypesTable.Rows[i]["list_type_id"]).ToString(),
                    (string)listTypesTable.Rows[i]["list_type_title"]);
            }

            List<string> permissions = new List<string>();
            permissions.Add("Can Read");

            template.ParseVariables("S_LIST_TYPES", Functions.BuildSelectBox("type", listTypes, "1"));
            template.ParseVariables("S_LIST_PERMS", Functions.BuildPermissionsBox(listPermissions, permissions));
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

                DataTable pageTable = db.SelectQuery(string.Format("SELECT list_id FROM user_lists WHERE list_id = {0} AND user_id = {1};",
                    listId, loggedInMember.UserId));

                if (pageTable.Rows.Count == 1)
                {
                }
                else
                {
                    Display.ShowMessage(core, "List Error", "You submitted invalid information. Go back and try again.");
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
                if (db.SelectQuery(string.Format("SELECT list_path FROM user_lists WHERE user_id = {0} AND list_path = '{1}';",
                    loggedInMember.UserId, Mysql.Escape(slug))).Rows.Count == 0)
                {
                    // verify that the type exists
                    DataTable listTypeTable = db.SelectQuery(string.Format("SELECT list_type_id FROM list_types WHERE list_type_id = {0};",
                        type));
                    if (listTypeTable.Rows.Count == 1)
                    {
                        listId = db.UpdateQuery(string.Format("INSERT INTO user_lists (user_id, list_title, list_path, list_type, list_abstract, list_access) VALUES ({0}, '{1}', '{2}', {3}, '{4}', {5});",
                            loggedInMember.UserId, Mysql.Escape(title), Mysql.Escape(slug), type, Mysql.Escape(listAbstract), Functions.GetPermission(Request)));

                        template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode("/account/?module=pages&sub=lists"));
                        Display.ShowMessage(core, "List Created", "You have created a new list");
                    }
                    else
                    {
                        Display.ShowMessage(core, "List Error", "You submitted invalid information. Go back and try again.");
                        return;
                    }
                }
                else
                {
                    Display.ShowMessage(core, "List Error", "You have already created a list with the same name, go back and give another name.");
                    return;
                }
            }

            // edit
            if (listId > 0)
            {
                // check that a list with the slug given does not already exist, ignoring itself
                if (db.SelectQuery(string.Format("SELECT list_path FROM user_lists WHERE user_id = {0} AND list_path = '{1}' AND list_id <> {2};",
                    loggedInMember.UserId, Mysql.Escape(slug), listId)).Rows.Count == 0)
                {
                    db.UpdateQuery(string.Format("UPDATE user_lists SET list_title = '{1}', list_access = {2}, list_path = '{3}', list_abstract = '{4}', list_type = {5} WHERE list_id = {0}",
                        listId, Mysql.Escape(title), Functions.GetPermission(Request), Mysql.Escape(slug), Mysql.Escape(listAbstract), type));

                    template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode("/account/?module=pages&sub=lists"));
                    Display.ShowMessage(core, "List Saved", "You have saved the list");
                }
                else
                {
                    Display.ShowMessage(core, "List Error", "You have already created a list with the same name, go back and give another name.");
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
                Display.ShowMessage(core, "List Error", "You submitted invalid information. Go back and try again.");
                return;
            }

            template.SetTemplate("account_list_edit.html");

            DataTable listTable = db.SelectQuery(string.Format("SELECT list_id, list_title, list_access, list_path, list_abstract, list_type FROM user_lists WHERE user_id = {0} AND list_id = {1};",
                loggedInMember.UserId, listId));

            if (listTable.Rows.Count == 1)
            {
                listTitle = (string)listTable.Rows[0]["list_title"];
                listPermissions = (ushort)listTable.Rows[0]["list_access"];
                listSlug = (string)listTable.Rows[0]["list_path"];
                listAbstract = (string)listTable.Rows[0]["list_abstract"];
                listType = (short)listTable.Rows[0]["list_type"];

                DataTable listTypesTable = db.SelectQuery("SELECT list_type_id, list_type_title FROM list_types ORDER BY list_type_title ASC");

                Dictionary<string, string> listTypes = new Dictionary<string, string>();

                for (int i = 0; i < listTypesTable.Rows.Count; i++)
                {
                    listTypes.Add(((short)listTypesTable.Rows[i]["list_type_id"]).ToString(),
                        (string)listTypesTable.Rows[i]["list_type_title"]);
                }

                List<string> permissions = new List<string>();
                permissions.Add("Can Read");

                template.ParseVariables("S_LIST_TYPES", Functions.BuildSelectBox("type", listTypes, listType.ToString()));
                template.ParseVariables("S_LIST_PERMS", Functions.BuildPermissionsBox(listPermissions, permissions));

                template.ParseVariables("S_LIST_TITLE", HttpUtility.HtmlEncode(listTitle));
                template.ParseVariables("S_LIST_SLUG", HttpUtility.HtmlEncode(listSlug));
                template.ParseVariables("S_LIST_ABSTRACT", HttpUtility.HtmlEncode(listAbstract));

                template.ParseVariables("S_LIST_ID", HttpUtility.HtmlEncode(((long)listTable.Rows[0]["list_id"]).ToString()));
            }
            else
            {
                Display.ShowMessage(core, "List Error", "You submitted invalid information. Go back and try again. List may have already been deleted.");
                return;
            }
        }

        /// <summary>
        /// Delete the list itself
        /// </summary>
        public void ManageListDelete()
        {
            if (Request.QueryString["sid"] != session.SessionId)
            {
                Display.ShowMessage(core, "Unauthorised", "You are unauthorised to do this action.");
                return;
            }

            long listId = 0;

            try
            {
                listId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage(core, "List Error", "You submitted invalid information. Go back and try again.");
                return;
            }

            DataTable listTable = db.SelectQuery(string.Format("SELECT list_items FROM user_lists WHERE user_id = {0} AND list_id = {1};",
                loggedInMember.UserId, listId));

            if (listTable.Rows.Count == 1)
            {
                db.UpdateQuery(string.Format("DELETE FROM list_items WHERE list_id = {0}",
                    listId), true);

                db.UpdateQuery(string.Format("DELETE FROM user_lists WHERE user_id = {0} AND list_id = {1}",
                    loggedInMember.UserId, listId), false);

                template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode("/account/?module=pages&sub=lists"));
                Display.ShowMessage(core, "List Deleted", "You have deleted a list.");
                return;
            }
            else
            {
                Display.ShowMessage(core, "List Error", "You submitted invalid information. Go back and try again. List may have already been deleted.");
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
                ajax = bool.Parse(Request.Form["ajax"]);
            }
            catch { }

            try
            {
                listId = long.Parse(Request.Form["id"]);
            }
            catch
            {
                if (ajax)
                {
                    Response.Write("error");
                    return;
                }
                else
                {
                    Display.ShowMessage(core, "List Error", "You submitted invalid information. Go back and try again.");
                    return;
                }
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
            DataTable listTable = db.SelectQuery(string.Format("SELECT list_id, list_path FROM user_lists WHERE user_id = {0} AND list_id = {1};",
                loggedInMember.UserId, listId));
            if (listTable.Rows.Count == 1)
            {
                DataTable listItemTextTable = db.SelectQuery(string.Format("SELECT list_item_text_id FROM list_items_text WHERE list_item_text_normalised = '{0}';",
                    Mysql.Escape(slug)));

                if (listItemTextTable.Rows.Count == 1)
                {
                    // already exists
                    db.UpdateQuery(string.Format("INSERT INTO list_items (list_id, list_item_text_id) VALUES ({0}, {1})",
                        listId, (long)listItemTextTable.Rows[0]["list_item_text_id"]), true);
                }
                else // insert new
                {
                    long listItemTextId = db.UpdateQuery(string.Format("INSERT INTO list_items_text (list_item_text, list_item_text_normalised) VALUES ('{0}', '{1}');",
                        Mysql.Escape(text), Mysql.Escape(slug)), true);

                    db.UpdateQuery(string.Format("INSERT INTO list_items (list_id, list_item_text_id) VALUES ({0}, {1})",
                        listId, listItemTextId), true);
                }

                db.UpdateQuery(string.Format("UPDATE user_lists SET list_items = list_items + 1 WHERE user_id = {0} AND list_id = {1}",
                    loggedInMember.UserId, listId), false);

                if (ajax)
                {
                    Response.Write(HttpUtility.HtmlEncode(text));

                    if (db != null)
                    {
                        db.CloseConnection();
                    }
                    Response.End();
                    return;
                }
                else
                {
                    template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode(ZzUri.BuildListUri(loggedInMember, (string)listTable.Rows[0]["list_path"])));
                    Display.ShowMessage(core, "List Updated", "You have successfully appended an item to your list.");
                }
            }
            else
            {
                if (ajax)
                {
                    Response.Write("error");
                    return;
                }
                else
                {
                    Display.ShowMessage(core, "List Error", "You submitted invalid information. Go back and try again.");
                    return;
                }
            }

        }

        /// <summary>
        /// Remove an item from a list
        /// </summary>
        public void ManageListRemove()
        {
            if (Request.QueryString["sid"] != session.SessionId)
            {
                Display.ShowMessage(core, "Unauthorised", "You are unauthorised to do this action.");
                return;
            }

            long itemId = 0;
            long listId = 0;

            try
            {
                itemId = long.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage(core, "List Error", "You submitted invalid information. Go back and try again.");
                return;
            }

            DataTable listItemTable = db.SelectQuery(string.Format("SELECT li.list_id, ul.list_path FROM list_items li INNER JOIN user_lists ul ON ul.list_id = li.list_id WHERE li.list_item_id = {0};",
                itemId));

            if (listItemTable.Rows.Count == 1)
            {
                listId = (long)listItemTable.Rows[0]["list_id"];

                db.UpdateQuery(string.Format("DELETE FROM list_items WHERE list_item_id = {0} AND list_id = {1}",
                    itemId, listId), true);

                db.UpdateQuery(string.Format("UPDATE user_lists SET list_items = list_items - 1 WHERE user_id = {0} AND list_id = {1}",
                        loggedInMember.UserId, listId), false);

                template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode(ZzUri.BuildListUri(loggedInMember, (string)listItemTable.Rows[0]["list_path"])));
                Display.ShowMessage(core, "List Updated", "You have successfully removed an item from your list.");
            }
            else
            {
                Display.ShowMessage(core, "List Error", "You submitted invalid information. Go back and try again.");
                return;
            }
        }

    }
}
