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

namespace BoxSocial.Applications.Blog
{
    [AccountSubModule("blog", "drafts")]
    public class AccountBlogDrafts : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Draft Blog Posts";
            }
        }

        public override int Order
        {
            get
            {
                return 3;
            }
        }

        public AccountBlogDrafts()
        {
            this.Load += new EventHandler(AccountBlogDrafts_Load);
            this.Show += new EventHandler(AccountBlogDrafts_Show);
        }

        void AccountBlogDrafts_Load(object sender, EventArgs e)
        {
        }

        void AccountBlogDrafts_Show(object sender, EventArgs e)
        {
            SetTemplate("account_blog_manage");

            Blog myBlog = new Blog(core, loggedInMember);
            ushort readAccessLevel = 0x0000;
            List<BlogEntry> blogEntries = myBlog.GetDrafts(null, -1, -1, -1, 1, 50, ref readAccessLevel);

            int i = 0;
            foreach (BlogEntry be in blogEntries)
            {
                VariableCollection blogVariableCollection = template.CreateChild("blog_list");

                DateTime postedTime = be.GetCreatedDate(tz);

                blogVariableCollection.Parse("COMMENTS", be.Comments.ToString());
                blogVariableCollection.Parse("TITLE", be.Title);
                blogVariableCollection.Parse("POSTED", tz.DateTimeToString(postedTime));

                blogVariableCollection.Parse("U_VIEW", Linker.BuildBlogPostUri(loggedInMember, postedTime.Year, postedTime.Month, be.Id));

                blogVariableCollection.Parse("U_EDIT", BuildUri("write", "edit", be.Id));
                blogVariableCollection.Parse("U_DELETE", BuildUri("write", "delete", be.Id));

                if (i % 2 == 0)
                {
                    blogVariableCollection.Parse("INDEX_EVEN", "TRUE");
                }
                i++;
            }
        }
    }
}
