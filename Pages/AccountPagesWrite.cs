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
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Pages
{
    [AccountSubModule(AppPrimitives.Member | AppPrimitives.Group, "pages", "write")]
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
            string pageTitle = (core.Http.Form["title"] != null) ? core.Http.Form["title"] : "";
            string pageSlug = (core.Http.Form["slug"] != null) ? core.Http.Form["slug"] : "";
            string pageText = (core.Http.Form["post"] != null) ? core.Http.Form["post"] : "";
            string pagePath = "";
            Classifications pageClassification = Classifications.Everyone;

            try
            {
                if (core.Http.Form["license"] != null)
                {
                    licenseId = core.Functions.GetLicense();
                }
                if (core.Http.Form["id"] != null)
                {
                    pageId = long.Parse(core.Http.Form["id"]);
                }
                if (core.Http.Form["page-parent"] != null)
                {
                    pageParentId = long.Parse(core.Http.Form["page-parent"]);
                }
            }
            catch
            {
            }

            if (core.Http.Query["id"] != null)
            {
                try
                {
                    pageId = long.Parse(core.Http.Query["id"]);
                }
                catch
                {
                }
            }

            if (pageId > 0)
            {
                if (core.Http.Query["mode"] == "edit")
                {
                    try
                    {
                        Page page = new Page(core, Owner, pageId);

                        pageParentId = page.ParentId;
                        pageTitle = page.Title;
                        pageSlug = page.Slug;
                        //pagePermissions = page.Permissions;
                        licenseId = page.LicenseId;
                        pageText = page.Body;
                        pagePath = page.FullPath;
                        pageClassification = page.Classification;
                    }
                    catch (PageNotFoundException)
                    {
                        DisplayGenericError();
                    }
                }
            }

            Pages myPages = new Pages(core, Owner);
            List<Page> pagesList = myPages.GetPages(false, true);

            SelectBox pagesSelectBox = new SelectBox("page-parent");
            pagesSelectBox.Add(new SelectBoxItem("0", "/"));

            foreach (Page page in pagesList)
            {
                SelectBoxItem item = new SelectBoxItem(page.Id.ToString(), page.FullPath);
                pagesSelectBox.Add(item);

                if (pageId > 0)
                {
                    if (page.FullPath.StartsWith(pagePath))
                    {
                        item.Selectable = false;
                    }
                }
            }

            List<string> permissions = new List<string>();
            permissions.Add("Can Read");

            if (pageId > 0 && pagesSelectBox.ContainsKey(pageId.ToString()))
            {
                pagesSelectBox[pageId.ToString()].Selectable = false;
            }

            pagesSelectBox.SelectedKey = pageParentId.ToString();

            core.Display.ParseLicensingBox(template, "S_PAGE_LICENSE", licenseId);
            core.Display.ParseClassification(template, "S_PAGE_CLASSIFICATION", pageClassification);
            template.Parse("S_PAGE_PARENT", pagesSelectBox);

            //core.Display.ParsePermissionsBox(template, "S_PAGE_PERMS", pagePermissions, permissions);

            template.Parse("S_TITLE", pageTitle);
            template.Parse("S_SLUG", pageSlug);
            template.Parse("S_PAGE_TEXT", pageText);
            template.Parse("S_ID", pageId.ToString());

            Save(new EventHandler(AccountPagesWrite_Save));
            if (core.Http.Form["publish"] != null)
            {
                AccountPagesWrite_Save(this, new EventArgs());
            }
        }

        void AccountPagesWrite_Save(object sender, EventArgs e)
        {
            string slug = core.Http.Form["slug"];
            string title = core.Http.Form["title"];
            string pageBody = core.Http.Form["post"];
            long parent = 0;
            long pageId = 0;
            PageStatus status = PageStatus.Publish;

            if (core.Http.Form["publish"] != null)
            {
                status = PageStatus.Publish;
            }

            if (core.Http.Form["save"] != null)
            {
                status = PageStatus.Draft;
            }

            pageId = core.Functions.FormLong("id", 0);
            parent = core.Functions.FormLong("page-parent", 0);

            try
            {
                if (pageId > 0)
                {
                    try
                    {
                        Page page = new Page(core, Owner, pageId);

                        page.Update(core, Owner, title, ref slug, parent, pageBody, status, core.Functions.GetLicense(), core.Functions.GetClassification());
                    }
                    catch (PageNotFoundException)
                    {
                        DisplayGenericError();
                    }
                }
                else
                {
                    Page.Create(core, Owner, title, ref slug, parent, pageBody, status, core.Functions.GetLicense(), core.Functions.GetClassification());
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
                core.Display.ShowMessage("Draft Saved", "Your draft has been saved.");
            }
            else
            {
                SetRedirectUri(BuildUri("manage"));
                core.Display.ShowMessage("Page Published", "Your page has been published");
            }
        }

        void AccountPagesWrite_Delete(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long pageId = core.Functions.RequestLong("id", 0);

            try
            {
                Page page = new Page(core, Owner, pageId);

                if (page.Delete(core, Owner))
                {
                    SetRedirectUri(BuildUri("manage"));
                    core.Display.ShowMessage("Page Deleted", "The page has been deleted from the database.");
                    return;
                }
                else
                {
                    core.Display.ShowMessage("Error", "Could not delete the page.");
                    return;
                }
            }
            catch (PageNotFoundException)
            {
                core.Display.ShowMessage("Error", "Could not delete the page.");
                return;
            }
        }
    }
}
