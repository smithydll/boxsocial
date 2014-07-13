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
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Blog
{
    /// <summary>
    /// Account sub module for managing blog posts
    /// </summary>
    [AccountSubModule("blog", "manage", true)]
    public class AccountBlogManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return core.Prose.GetString("Blog", "MANAGE_BLOG_POSTS");
            }
        }

        public override int Order
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountBlogManage class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountBlogManage(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountBlogManage_Load);
            this.Show += new EventHandler(AccountBlogManage_Show);
        }

        void AccountBlogManage_Load(object sender, EventArgs e)
        {
        }

        void AccountBlogManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_blog_manage");

            Blog myBlog;

            try
            {
                myBlog = new Blog(core, LoggedInMember);
            }
            catch (InvalidBlogException)
            {
                myBlog = Blog.Create(core);
            }

            if (myBlog.Access.Can("POST_ITEMS"))
            {
                template.Parse("U_WRITE_NEW_POST", BuildUri("write"));
            }
            template.Parse("U_PERMISSIONS", core.Hyperlink.AppendAbsoluteSid(string.Format("/api/acl?id={0}&type={1}", myBlog.Id, ItemType.GetTypeId(typeof(Blog))), true));

            List<BlogEntry> blogEntries = myBlog.GetEntries(null, null, -1, -1, -1, core.TopLevelPageNumber, 25);

            foreach (BlogEntry be in blogEntries)
            {
                VariableCollection blogVariableCollection = template.CreateChild("blog_list");

                DateTime postedTime = be.GetCreatedDate(tz);

                blogVariableCollection.Parse("COMMENTS", be.Comments.ToString());
                blogVariableCollection.Parse("TITLE", be.Title);
                blogVariableCollection.Parse("POSTED", tz.DateTimeToString(postedTime));

                blogVariableCollection.Parse("U_VIEW", core.Hyperlink.BuildBlogPostUri(LoggedInMember, postedTime.Year, postedTime.Month, be.Id));

                blogVariableCollection.Parse("U_EDIT", BuildUri("write", "edit", be.Id));
                blogVariableCollection.Parse("U_EDIT_PERMISSIONS", core.Hyperlink.AppendAbsoluteSid(string.Format("/api/acl?id={0}&type={1}", be.Id, ItemType.GetTypeId(typeof(BlogEntry))), true));
                blogVariableCollection.Parse("U_DELETE", BuildUri("write", "delete", be.Id));
            }

            core.Display.ParsePagination(template, BuildUri(), 25, myBlog.Entries);
        }
    }
}
