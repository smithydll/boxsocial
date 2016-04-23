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
    [AccountSubModule("blog", "trackback")]
    public class AccountBlogTrackback : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return core.Prose.GetString("Blog", "MANAGE_TRACKBACKS");
            }
        }

        public override int Order
        {
            get
            {
                return 5;
            }
        }

        public AccountBlogTrackback(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountBlogTrackback_Load);
            this.Show += new EventHandler(AccountBlogTrackback_Show);
        }

        void AccountBlogTrackback_Load(object sender, EventArgs e)
        {
        }

        void AccountBlogTrackback_Show(object sender, EventArgs e)
        {
            SetTemplate("account_blog_trackback");

            Blog myBlog;

            try
            {
                myBlog = new Blog(core, LoggedInMember);
            }
            catch (InvalidBlogException)
            {
                myBlog = Blog.Create(core);
            }

            List<TrackBack> trackBacksUnapproved = myBlog.GetTrackBacksUnapproved(core.TopLevelPageNumber, 10);
        }
    }
}
