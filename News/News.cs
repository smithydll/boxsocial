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
		
		public List<Article> GetArticles(int offset, int count)
		{
			List<Article> articles = new List<Article>();
			
			SelectQuery query = Article.GetSelectQueryStub(typeof(Article));
			query.AddCondition("article_item_id", owner.Id);
			query.AddCondition("article_item_type", owner.Type);
			query.AddSort(SortOrder.Ascending, "article_time_ut");
			
			DataTable articlesDataTable = db.Query(query);
			
			foreach (DataRow dr in articlesDataTable.Rows)
			{
				articles.Add(new Article(core, dr));
			}
			
			return articles;
		}
		
		public static void Show(Core core, GPage page)
		{
			page.template.SetTemplate("News", "viewnews");
		}
	}
}
