﻿/*
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

namespace BoxSocial.Applications.Pages
{
    [AccountSubModule(AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Musician, "pages", "manage", true)]
    public class AccountPagesManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return core.Prose.GetString("MANAGE_PAGES");
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
        public AccountPagesManage(Core core, Primitive owner)
            : base(core, owner)
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

            template.Parse("U_NEW_PAGE", BuildUri("write"));

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
                pagesVariableCollection.Parse("VIEWS", page.Info.ViewedTimes.ToString());

                pagesVariableCollection.Parse("U_VIEW", page.Uri);
                pagesVariableCollection.Parse("U_EDIT", BuildUri("write", "edit", page.Id));
                pagesVariableCollection.Parse("U_PERMS", Access.BuildAclUri(core, page));
                pagesVariableCollection.Parse("U_STATISTICS", core.Hyperlink.AppendAbsoluteSid(string.Format("/api/statistics?mode=item&id={0}&type={1}", page.Id, ItemType.GetTypeId(core, typeof(Page))), true));
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
