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
    public class Default
    {
        public static void Show(object sender, ShowPPageEventArgs e)
        {
            string pageName = e.Slug;

            char[] trimStartChars = { '.', '/' };
            if (pageName != null)
            {
                pageName = pageName.TrimEnd('/').TrimStart(trimStartChars);
            }

            try
            {
                Page thePage = new Page(e.Core, e.Page.Owner, pageName);
                Show(e.Core, e.Page.Owner, thePage);
            }
            catch (PageNotFoundException)
            {
                e.Core.Functions.Generate404();
            }
        }

        private static void Show(Core core, Primitive owner, Page thePage)
        {
            core.Template.SetTemplate("Pages", "viewpage");

            core.Display.ParsePageList(owner, true, thePage);

            if (!thePage.Access.Can("VIEW"))
            {
                core.Functions.Generate403();
                return;
            }

            BoxSocial.Internals.Classification.ApplyRestrictions(core, thePage.Classification);

            core.Template.Parse("PAGE_TITLE", thePage.Title);

            if (!string.IsNullOrEmpty(thePage.BodyCache))
            {
                core.Display.ParseBbcodeCache("PAGE_BODY", thePage.BodyCache);
            }
            else
            {
                core.Display.ParseBbcode(core.Template, "PAGE_BODY", thePage.Body, thePage.Owner, true, null, null);
            }

            if (core.Session.LoggedInMember != null)
            {
                if (owner is User && owner.Id != core.Session.LoggedInMember.UserId)
                {
                    core.Db.UpdateQuery(string.Format("UPDATE user_pages SET page_views = page_views + 1 WHERE page_item_id = {0} AND page_item_type_id = {1} AND page_id = '{2}';",
                        owner.Id, owner.TypeId, thePage.PageId));
                }
                else
                {
                    core.Db.UpdateQuery(string.Format("UPDATE user_pages SET page_views = page_views + 1 WHERE page_item_id = {0} AND page_item_type_id = {1} AND page_id = '{2}';",
                        owner.Id, owner.TypeId, thePage.PageId));
                }
            }

            if (thePage.License != null)
            {
                if (!string.IsNullOrEmpty(thePage.License.Title))
                {
                    core.Template.Parse("PAGE_LICENSE", thePage.License.Title);
                }
                if (!string.IsNullOrEmpty(thePage.License.Icon))
                {
                    core.Template.Parse("I_PAGE_LICENSE", thePage.License.Icon);
                }
                if (!string.IsNullOrEmpty(thePage.License.Link))
                {
                    core.Template.Parse("U_PAGE_LICENSE", thePage.License.Link);
                }
            }

            switch (thePage.Classification)
            {
                case Classifications.Everyone:
                    core.Template.Parse("PAGE_CLASSIFICATION", "Suitable for Everyone");
                    core.Template.Parse("I_PAGE_CLASSIFICATION", "rating_e.png");
                    break;
                case Classifications.Mature:
                    core.Template.Parse("PAGE_CLASSIFICATION", "Suitable for Mature Audiences 15+");
                    core.Template.Parse("I_PAGE_CLASSIFICATION", "rating_15.png");
                    break;
                case Classifications.Restricted:
                    core.Template.Parse("PAGE_CLASSIFICATION", "Retricted to Audiences 18+");
                    core.Template.Parse("I_PAGE_CLASSIFICATION", "rating_18.png");
                    break;
            }

            DateTime pageDateTime = thePage.GetModifiedDate(core.Tz);
            core.Template.Parse("PAGE_LAST_MODIFIED", core.Tz.DateTimeToString(pageDateTime));
            core.Template.Parse("PAGE_VIEWS", thePage.Views.ToString());
            core.Template.Parse("LAST_MODIFIED_PAGE_VIEWS", string.Format(core.Prose.GetString("LAST_MODIFIED_PAGE_VIEWS"), core.Tz.DateTimeToString(pageDateTime), thePage.Views.ToString()));

            List<string[]> breadCrumbParts = new List<string[]>();
            if (thePage.Parents != null)
            {
                foreach (ParentTreeNode ptn in thePage.Parents.Nodes)
                {
                    breadCrumbParts.Add(new string[] { ptn.ParentSlug, ptn.ParentTitle });
                }
            }

            if (thePage.Id > 0)
            {
                breadCrumbParts.Add(new string[] { thePage.Slug, thePage.Title });
            }

            owner.ParseBreadCrumbs(breadCrumbParts);

            if (thePage.Access.Can("EDIT"))
            {
                core.Template.Parse("U_EDIT", core.Hyperlink.BuildAccountSubModuleUri(owner, "pages", "write", "edit", thePage.PageId, true));
            }
        }
    }
}
