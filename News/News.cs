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
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.News
{
	public class News
	{
		private Core core;
		private Mysql db;
		private Primitive owner;
		
		public News(Core core, Primitive owner)
		{
			this.core = core;
			this.db = core.db;
			this.owner = owner;
		}
		
		public List<Article> GetArticles(int currentPage, int count)
		{
			List<Article> articles = new List<Article>();
			
			SelectQuery query = Article.GetSelectQueryStub(typeof(Article));
            ItemKey ik = new ItemKey(owner.Id, owner.GetType().FullName);
			query.AddCondition("article_item_id", ik.Id);
			query.AddCondition("article_item_type_id", ik.TypeId);
			query.AddSort(SortOrder.Descending, "article_time_ut");
            query.LimitStart = (currentPage - 1) * count;
            query.LimitCount = count;
			
			DataTable articlesDataTable = db.Query(query);
			
			foreach (DataRow dr in articlesDataTable.Rows)
			{
				articles.Add(new Article(core, dr));
			}
			
			foreach (Article article in articles)
            {
				core.LoadUserProfile(article.PosterId);
			}
			
			return articles;
		}
		
		public static void Show(Core core, GPage page)
		{
            int p = Functions.RequestInt("p", 1);
			page.template.SetTemplate("News", "viewnews");

            News news = new News(core, page.Group);

            List<Article> articles = news.GetArticles(p, 10);
			
			page.template.Parse("NEWS_COUNT", articles.Count.ToString());

            foreach (Article article in articles)
            {
                VariableCollection articleVariableCollection = page.template.CreateChild("news_list");

                core.Display.ParseBbcode(articleVariableCollection, "BODY", article.ArticleBody);
                articleVariableCollection.Parse("TITLE", article.ArticleSubject);
				articleVariableCollection.Parse("U_ARTICLE", article.Uri);
				articleVariableCollection.Parse("U_POSTER", article.Poster.Uri);
				articleVariableCollection.Parse("USERNAME", article.Poster.DisplayName);
				articleVariableCollection.Parse("COMMENTS", article.Comments.ToString());
				articleVariableCollection.Parse("DATE", core.tz.DateTimeToString(article.GetCreatedDate(core.tz)));
            }

            core.Display.ParsePagination(page.template, "PAGINATION", news.Uri, p, (int)Math.Ceiling((double)page.Group.Info.NewsArticles / 10), false);
			
			List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "news", "News" });

            page.Group.ParseBreadCrumbs(breadCrumbParts);
		}
		
		public string Uri
		{
			get
			{
				return owner.UriStub;
			}
		}
	}
}
