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
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Blog
{
    public class AppInfo : Application
    {
        public override void Initialise(Core core)
        {
            this.core = core;

            core.PageHooks += new Core.HookHandler(core_PageHooks);
            core.LoadApplication += new Core.LoadHandler(core_LoadApplication);

            core.RegisterCommentHandle("BLOGPOST", blogCanPostComment, blogCanDeleteComment, blogAdjustCommentCount);
        }

        void core_LoadApplication(Core core, object sender)
        {
            this.core = core;

            core.RegisterApplicationPage(@"^/blog(|/)$", showBlog, 1);
            core.RegisterApplicationPage(@"^/blog/category/([a-z0-9\-]+)(|/)$", showBlogCategory, 2);
            core.RegisterApplicationPage(@"^/blog/([0-9]{4})(|/)$", showBlogYear, 3);
            core.RegisterApplicationPage(@"^/blog/([0-9]{4})/([0-9]{1,2})(|/)$", showBlogMonth, 4);
            core.RegisterApplicationPage(@"^/blog/([0-9]{4})/([0-9]{1,2})/([0-9]+)(|/)$", showBlogPost, 5);
        }

        private bool blogCanPostComment(long itemId, Member member)
        {
            BlogEntry blogEntry = new BlogEntry(core.db, itemId);
            blogEntry.BlogEntryAccess.SetViewer(member);

            if (blogEntry.BlogEntryAccess.CanComment)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool blogCanDeleteComment(long itemId, Member member)
        {
            BlogEntry blogEntry = new BlogEntry(core.db, itemId);

            if (blogEntry.OwnerId == member.UserId)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void blogAdjustCommentCount(long itemId, int adjustment)
        {
            core.db.UpdateQuery(string.Format("UPDATE blog_postings SET post_comments = post_comments + {1} WHERE post_id = {0};",
                itemId, adjustment), false);
        }

        private void showBlog(Core core, object sender)
        {
            if (sender is PPage)
            {
                Blog.Show(core, (PPage)sender, "", -1, -1, -1);
            }
        }

        private void showBlogCategory(Core core, object sender)
        {
            if (sender is PPage)
            {
                Blog.Show(core, (PPage)sender, core.PagePathParts[1].Value, -1, -1, -1);
            }
        }

        private void showBlogYear(Core core, object sender)
        {
            if (sender is PPage)
            {
                Blog.Show(core, (PPage)sender, "", -1, int.Parse(core.PagePathParts[1].Value), -1);
            }
        }

        private void showBlogMonth(Core core, object sender)
        {
            if (sender is PPage)
            {
                Blog.Show(core, (PPage)sender, "", -1, int.Parse(core.PagePathParts[1].Value), int.Parse(core.PagePathParts[2].Value));
            }
        }

        private void showBlogPost(Core core, object sender)
        {
            if (sender is PPage)
            {
                Blog.Show(core, (PPage)sender, "", int.Parse(core.PagePathParts[3].Value), int.Parse(core.PagePathParts[1].Value), int.Parse(core.PagePathParts[2].Value));
            }
        }

        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.Member;
        }

        void core_PageHooks(Core core, object sender)
        {

        }
    }
}
