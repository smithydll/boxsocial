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
    // NewsSettings table
    [DataTable("news_settings", "NEWSSETTINGS")]
    [Permission("COMMENT_ARTICLES", "Can comment on the news articles", PermissionTypes.Interact)]
    [Permission("CREATE_ARTICLES", "Can post news articles", PermissionTypes.CreateAndEdit)]
	public class News : NumberedItem, IPermissibleItem
	{
        [DataField("news_id", DataFieldKeys.Primary)]
        private long newsId;
        [DataField("news_item", DataFieldKeys.Index)]
        private ItemKey ownerKey;
        [DataField("news_title", 127)]
        private string newsTitle;
        [DataField("news_items_per_page")]
        private int newsItemsPerPage;

		private Primitive owner;
        private Access newsAccess;

        public string Title
        {
            get
            {
                return newsTitle;
            }
            set
            {
                SetProperty("newsTitle", value);
            }
        }

        public int ItemsPerPage
        {
            get
            {
                return newsItemsPerPage;
            }
            set
            {
                SetProperty("newsItemsPerPage", value);
            }
        }

        public News(Core core, long newsId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(News_ItemLoad);

            try
            {
                LoadItem(newsId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidNewsException();
            }
        }

        public News(Core core, DataRow newsDataRow)
			: base(core)
		{
            ItemLoad += new ItemLoadHandler(News_ItemLoad);
			
			try
			{
                loadItemInfo(newsDataRow);
			}
		    catch (InvalidItemException)
			{
                throw new InvalidNewsException();
			}
		}
		
		public News(Core core, Primitive owner)
            : base (core)
		{
            ItemLoad += new ItemLoadHandler(News_ItemLoad);

			this.owner = owner;

            try
            {
                LoadItem("news_item_id", "news_item_type_id", owner);
            }
            catch (InvalidItemException)
            {
                throw new InvalidNewsException();
            }
		}

        void News_ItemLoad()
        {
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

        public static News Create(Core core, Primitive owner, string title, int itemsPerPage)
        {
            Item item = Item.Create(core, typeof(News), new FieldValuePair("news_item_id", owner.Id),
                new FieldValuePair("news_item_type_id", owner.TypeId),
                new FieldValuePair("news_title", title),
                new FieldValuePair("news_items_per_page", itemsPerPage));

            return (News)item;
        }
		
		public static void Show(object sender, ShowGPageEventArgs e)
		{
			e.Template.SetTemplate("News", "viewnews");

            News news = null;
            try
            {
                news = new News(e.Core, e.Page.Owner);
            }
            catch (InvalidNewsException)
            {
                news = News.Create(e.Core, e.Page.Owner, e.Page.Owner.TitleNameOwnership + " News", 10);
            }

            List<Article> articles = news.GetArticles(e.Page.TopLevelPageNumber, 10);
			
			e.Template.Parse("NEWS_COUNT", articles.Count.ToString());

            foreach (Article article in articles)
            {
                VariableCollection articleVariableCollection = e.Template.CreateChild("news_list");

                e.Core.Display.ParseBbcode(articleVariableCollection, "BODY", article.ArticleBody);
                articleVariableCollection.Parse("TITLE", article.ArticleSubject);
				articleVariableCollection.Parse("U_ARTICLE", article.Uri);
				articleVariableCollection.Parse("U_POSTER", article.Poster.Uri);
				articleVariableCollection.Parse("USERNAME", article.Poster.DisplayName);
				articleVariableCollection.Parse("COMMENTS", article.Comments.ToString());
				articleVariableCollection.Parse("DATE", e.Core.Tz.DateTimeToString(article.GetCreatedDate(e.Core.Tz)));
            }

            if (news.Access.Can("CREATE_ARTICLES"))
            {
                e.Template.Parse("U_CREATE_ARTICLE", news.Owner.AccountUriStub);
                e.Template.Parse("L_POST_NEWS_ARTICLE", "New Article");
            }

            e.Core.Display.ParsePagination(e.Template, "PAGINATION", news.Uri, e.Page.TopLevelPageNumber, (int)Math.Ceiling((double)e.Page.Group.Info.NewsArticles / 10), false);
			
			List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "news", "News" });

            e.Page.Group.ParseBreadCrumbs(breadCrumbParts);
		}
		
		public override string Uri
		{
			get
			{
				return owner.UriStub + "/news";
			}
		}

        public override long Id
        {
            get
            {
                return newsId;
            }
        }

        public Access Access
        {
            get
            {
                if (Id == 0)
                {
                    return Owner.Access;
                }
                else
                {
                    if (newsAccess == null)
                    {
                        newsAccess = new Access(core, this);
                    }
                    return newsAccess;
                }
            }
        }

        public Primitive Owner
        {
            get
            {
                if (owner == null || (ownerKey.Id != owner.Id && ownerKey.TypeId != owner.TypeId))
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(ownerKey);
                    owner = core.PrimitiveCache[ownerKey];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

        public List<AccessControlPermission> AclPermissions
        {
            get
            {
                return AccessControlLists.GetPermissions(core, this);
            }
        }

        public bool IsItemGroupMember(User viewer, ItemKey key)
        {
            return false;
        }

        public IPermissibleItem PermissiveParent
        {
            get
            {
                return Owner;
            }
        }

        public string DisplayTitle
        {
            get
            {
                return "News: " + Title + "[" + Owner.TitleName + "]";
            }
        }

        public bool GetDefaultCan(string permission)
        {
            return false;
        }
    }

    public class InvalidNewsException : Exception
    {
    }
}
