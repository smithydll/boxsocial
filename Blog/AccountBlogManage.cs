﻿/*
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
        public AccountBlogManage(Core core)
            : base (core)
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
            int p = core.Functions.RequestInt("p", 1);

            try
            {
                myBlog = new Blog(core, LoggedInMember);
            }
            catch (InvalidBlogException)
            {
                myBlog = Blog.Create(core);
            }

            ushort readAccessLevel = 0x0000;
            List<BlogEntry> blogEntries = myBlog.GetEntries(null, null, -1, -1, -1, p, 25, ref readAccessLevel);

            foreach (BlogEntry be in blogEntries)
            {
                VariableCollection blogVariableCollection = template.CreateChild("blog_list");

                DateTime postedTime = be.GetCreatedDate(tz);

                blogVariableCollection.Parse("COMMENTS", be.Comments.ToString());
                blogVariableCollection.Parse("TITLE", be.Title);
                blogVariableCollection.Parse("POSTED", tz.DateTimeToString(postedTime));

                blogVariableCollection.Parse("U_VIEW", core.Uri.BuildBlogPostUri(LoggedInMember, postedTime.Year, postedTime.Month, be.Id));

                blogVariableCollection.Parse("U_EDIT", BuildUri("write", "edit", be.Id));
                blogVariableCollection.Parse("U_DELETE", BuildUri("write", "delete", be.Id));
            }

            core.Display.ParsePagination(template, "PAGINATION", BuildUri(), p, (int)(Math.Ceiling(myBlog.Entries / 25.0)), false);
        }
    }
}
