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
    [AccountSubModule("blog", "preferences")]
    public class AccountBlogPreferences : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return core.Prose.GetString("Blog", "BLOG_PREFERENCES");
            }
        }

        public override int Order
        {
            get
            {
                return 5;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountBlogPreferences class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountBlogPreferences(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountBlogPreferences_Load);
            this.Show += new EventHandler(AccountBlogPreferences_Show);
        }

        void AccountBlogPreferences_Load(object sender, EventArgs e)
        {
        }

        void AccountBlogPreferences_Show(object sender, EventArgs e)
        {
            SetTemplate("account_blog_preferences");

            Blog myBlog;
            try
            {
                myBlog = new Blog(core, session.LoggedInMember);
            }
            catch (InvalidBlogException)
            {
                myBlog = Blog.Create(core);
            }

            List<string> permissions = new List<string>();
            permissions.Add("Can Read");
            permissions.Add("Can Comment");

            template.Parse("S_TITLE", myBlog.Title);
            //core.Display.ParsePermissionsBox(template, "S_BLOG_PERMS", myBlog.Permissions, permissions);

            Save(new EventHandler(AccountBlogPreferences_Save));
        }

        void AccountBlogPreferences_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            string title = core.Http.Form["title"];

            UpdateQuery uQuery = new UpdateQuery("user_blog");
            uQuery.AddCondition("user_id", core.LoggedInMemberId);
            uQuery.AddField("blog_title", title);

            db.Query(uQuery);

            SetRedirectUri(BuildUri());
            core.Display.ShowMessage("Blog Preferences Updated", "Your blog preferences have been successfully updated.");
        }
    }
}
