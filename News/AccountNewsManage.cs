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
using BoxSocial.Groups;

namespace BoxSocial.Applications.News
{
    [AccountSubModule(AppPrimitives.Group, "news", "manage", true)]
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

        /// <summary>
        /// Initializes a new instance of the AccountNewsManage class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountNewsManage(Core core)
            : base(core)
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

            News news = new News(core, Owner);
            List<Article> articles = news.GetArticles(core.TopLevelPageNumber, 25);

            foreach (Article article in articles)
            {
                VariableCollection articlesVariableCollection = template.CreateChild("news_list");

                DateTime postedTime = article.GetCreatedDate(tz);

                articlesVariableCollection.Parse("COMMENTS", article.Comments.ToString());
                articlesVariableCollection.Parse("TITLE", article.ArticleSubject);
                articlesVariableCollection.Parse("POSTED", tz.DateTimeToString(postedTime));

                articlesVariableCollection.Parse("U_VIEW", article.Uri);

                articlesVariableCollection.Parse("U_EDIT", BuildUri("write", "edit", article.Id));
                articlesVariableCollection.Parse("U_DELETE", BuildUri("write", "delete", article.Id));

            }

            if (Owner is UserGroup)
            {
                core.Display.ParsePagination(template, BuildUri(), 25, ((UserGroup)Owner).GroupInfo.NewsArticles);
            }
        }
	}
}
