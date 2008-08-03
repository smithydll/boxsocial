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
using System.IO;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Pages
{
    [AccountSubModule("pages", "write")]
    public class AccountPagesWrite : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Write New Page";
            }
        }

        public override int Order
        {
            get
            {
                return 2;
            }
        }

        public AccountPagesWrite()
        {
            this.Load += new EventHandler(AccountPagesWrite_Load);
            this.Show += new EventHandler(AccountPagesWrite_Show);
        }

        void AccountPagesWrite_Load(object sender, EventArgs e)
        {
            AddModeHandler("delete", new ModuleModeHandler(AccountPagesWrite_Delete));
        }

        void AccountPagesWrite_Show(object sender, EventArgs e)
        {
            SetTemplate("account_write");

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
                if (Request.QueryString["mode"] == "edit")
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
                LoggedInMember.UserId));

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

            Display.ParseLicensingBox(template, "S_PAGE_LICENSE", licenseId);
            Display.ParseClassification(template, "S_PAGE_CLASSIFICATION", pageClassification);
            Display.ParseSelectBox(template, "S_PAGE_PARENT", "page-parent", pages, pageParentId.ToString(), disabledItems);

            Display.ParsePermissionsBox(template, "S_PAGE_PERMS", pagePermissions, permissions);

            template.Parse("S_TITLE", pageTitle);
            template.Parse("S_SLUG", pageSlug);
            template.Parse("S_PAGE_TEXT", pageText);
            template.Parse("S_ID", pageId.ToString());

            Save(new EventHandler(AccountPagesWrite_Save));
            if (Request.Form["publish"] != null)
            {
                AccountPagesWrite_Save(this, new EventArgs());
            }
        }

        void AccountPagesWrite_Save(object sender, EventArgs e)
        {
            string slug = Request.Form["slug"];
            string title = Request.Form["title"];
            string pageBody = Request.Form["post"];
            long parent = 0;
            long pageId = 0;
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
                        Page page = new Page(core, core.session.LoggedInMember, pageId);

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
                SetRedirectUri(BuildUri("drafts"));
                Display.ShowMessage("Draft Saved", "Your draft has been saved.");
            }
            else
            {
                SetRedirectUri(BuildUri("manage"));
                Display.ShowMessage("Page Published", "Your page has been published");
            }
        }

        void AccountPagesWrite_Delete(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long pageId = Functions.RequestLong("id", 0);

            try
            {
                Page page = new Page(core, LoggedInMember, pageId);

                if (page.Delete(core, LoggedInMember))
                {
                    SetRedirectUri(BuildUri("mange"));
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
    }
}
