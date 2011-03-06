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
    [AccountSubModule(AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Musician, "pages", "manage", true)]
    public class AccountPagesManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Manage Pages";
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
        /// Initializes a new instance of the AccountPagesManage class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountPagesManage(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountPagesManage_Load);
            this.Show += new EventHandler(AccountPagesManage_Show);
        }

        void AccountPagesManage_Load(object sender, EventArgs e)
        {
        }

        void AccountPagesManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_pages_manage");

            Pages myPages = new Pages(core, Owner);

            List<Page> pages = myPages.GetPages(false);

            long i = 0;
            foreach (Page page in pages)
            {
                VariableCollection pagesVariableCollection = template.CreateChild("page_list");

                int level = 0;
                level = page.FullPath.Split('/').Length - 1;
                string levelString = "";
                for (int j = 0; j < level; j++)
                {
                    levelString += "— ";
                }

                pagesVariableCollection.Parse("TITLE", levelString + page.Title);
                pagesVariableCollection.Parse("UPDATED", tz.DateTimeToString(page.GetModifiedDate(tz)));
                pagesVariableCollection.Parse("U_VIEW", page.Uri);
                pagesVariableCollection.Parse("U_EDIT", BuildUri("write", "edit", page.Id));
                pagesVariableCollection.Parse("U_PERMS", Access.BuildAclUri(core, page));
                pagesVariableCollection.Parse("U_DELETE", BuildUri("write", "delete", page.Id));

                if (i % 2 == 0)
                {
                    pagesVariableCollection.Parse("INDEX_EVEN", "TRUE");
                }
                i++;
            }
        }
    }
}
