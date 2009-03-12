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
using BoxSocial.Groups;
using BoxSocial.Groups;

namespace BoxSocial.Applications.News
{
    [AccountSubModule("news", "manage", true)]
    public class AccountNewsManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Manage News";
            }
        }

        public override int Order
        {
            get
            {
                return 1;
            }
        }

        public AccountNewsManage()
        {
            this.Load += new EventHandler(AccountNewsManage_Load);
            this.Show += new EventHandler(AccountNewsManage_Show);
        }

        void AccountNewsManage_Load(object sender, EventArgs e)
        {
        }

        void AccountNewsManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_news_manage");

            int p = Functions.RequestInt("p", 1);
			News news = new News(core, Owner);
            List<Article> articles = news.GetArticles(p, 10); 

            foreach (Article article in articles)
            {
                VariableCollection articlesVariableCollection = template.CreateChild("news_list");
            }
			
			if (Owner is UserGroup)
{
			//Display.ParsePagination(template, "PAGINATION", BuildUri(), p, (int)Math.Ceiling(((UserGroup)Owner).NewsArticles / 50.0));
}
        }
	}
}
