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
        [DataField("news_item", DataFieldKeys.Unique)]
        private ItemKey ownerKey;
        [DataField("news_title", 127)]
        private string newsTitle;
        [DataField("news_items_per_page")]
        private int newsItemsPerPage;
        [DataField("news_simple_permissions")]
        private bool simplePermissions;

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

        public News(Core core, System.Data.Common.DbDataReader newsDataRow)
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

        protected override void loadItemInfo(DataRow newsRow)
        {
            loadValue(newsRow, "news_id", out newsId);
            loadValue(newsRow, "news_item", out ownerKey);
            loadValue(newsRow, "news_title", out newsTitle);
            loadValue(newsRow, "news_items_per_page", out newsItemsPerPage);
            loadValue(newsRow, "news_simple_permissions", out simplePermissions);

            itemLoaded(newsRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader newsRow)
        {
            loadValue(newsRow, "news_id", out newsId);
            loadValue(newsRow, "news_item", out ownerKey);
            loadValue(newsRow, "news_title", out newsTitle);
            loadValue(newsRow, "news_items_per_page", out newsItemsPerPage);
            loadValue(newsRow, "news_simple_permissions", out simplePermissions);

            itemLoaded(newsRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        void News_ItemLoad()
        {
        }
		
		public List<Article> GetArticles(int currentPage, int count)
		{
			List<Article> articles = new List<Article>();

            SelectQuery query = Article.GetSelectQueryStub(core, typeof(Article));
			query.AddCondition("article_item_id", Owner.Id);
			query.AddCondition("article_item_type_id", Owner.TypeId);
			query.AddSort(SortOrder.Descending, "article_time_ut");
            query.LimitStart = (currentPage - 1) * count;
            query.LimitCount = count;

            System.Data.Common.DbDataReader articlesReader = db.ReaderQuery(query);
			
			while(articlesReader.Read())
			{
                articles.Add(new Article(core, articlesReader));
			}

            articlesReader.Close();
            articlesReader.Dispose();
			
			foreach (Article article in articles)
            {
				core.LoadUserProfile(article.PosterId);
			}
			
			return articles;
		}

        public List<NewsIcon> GetIcons()
        {
            List<NewsIcon> icons = new List<NewsIcon>();

            SelectQuery query = NewsIcon.GetSelectQueryStub(core, typeof(NewsIcon));
            query.AddCondition("icon_item_id", owner.ItemKey.Id);
            query.AddCondition("icon_item_type_id", owner.ItemKey.TypeId);

            DataTable iconsDataTable = db.Query(query);

            foreach (DataRow dr in iconsDataTable.Rows)
            {
                icons.Add(new NewsIcon(core, dr));
            }

            return icons;
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

            e.Template.Parse("PAGE_TITLE", e.Core.Prose.GetString("NEWS"));

            e.Core.Display.ParsePageList(e.Page.Owner, true);

            List<Article> articles = news.GetArticles(e.Page.TopLevelPageNumber, 10);
			
			e.Template.Parse("NEWS_COUNT", articles.Count.ToString());

            foreach (Article article in articles)
            {
                VariableCollection articleVariableCollection = e.Template.CreateChild("news_list");

                e.Core.Display.ParseBbcode(articleVariableCollection, "BODY", article.ArticleBody, e.Page.Owner);
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

            e.Core.Display.ParsePagination(e.Template, news.Uri, 10, e.Page.Group.GroupInfo.NewsArticles);
			
			List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "news", e.Core.Prose.GetString("NEWS") });

            e.Page.Group.ParseBreadCrumbs(breadCrumbParts);
		}
		
		public override string Uri
		{
			get
			{
				return owner.UriStub + "news";
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

        public bool IsSimplePermissions
        {
            get
            {
                return simplePermissions;
            }
            set
            {
                SetPropertyByRef(new { simplePermissions }, value);
            }
        }

        public ItemKey OwnerKey
        {
            get
            {
                return ownerKey;
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

        public bool IsItemGroupMember(ItemKey viewer, ItemKey key)
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

        public ItemKey PermissiveParentKey
        {
            get
            {
                return ownerKey;
            }
        }

        public string DisplayTitle
        {
            get
            {
                return "News: " + Title + "[" + Owner.TitleName + "]";
            }
        }

        public string ParentPermissionKey(Type parentType, string permission)
        {
            return permission;
        }

        public bool GetDefaultCan(string permission, ItemKey viewer)
        {
            return false;
        }
    }

    public class InvalidNewsException : Exception
    {
    }
}
