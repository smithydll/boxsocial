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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;

namespace BoxSocial.Applications.News
{
    [DataTable("news_article", "ARTICLE")]
    public class Article : NumberedItem, ICommentableItem, ITagableItem
    {
        [DataField("article_id", DataFieldKeys.Primary)]
        private long articleId;
		[DataField("user_id")]
        private long userId;
		[DataField("article_item", DataFieldKeys.Index)]
        private ItemKey ownerKey;
		//[DataField("article_item_type", NAMESPACE)]
        //private string ownerType;
		[DataField("article_time_ut")]
        private long articleTime;
		[DataField("article_subject", 127)]
        private string articleSubject;
		[DataField("article_body", MYSQL_TEXT)]
        private string articleBody;
        [DataField("article_icon_item_id", typeof(NewsIcon))]
        private long newsIconId;
		
		private Primitive owner;
		private User poster;
        private NewsIcon icon;
		
		public long ArticleId
		{
			get
			{
				return articleId;
			}
		}
		
		public long PosterId
		{
			get
			{
				return userId;
			}
		}

        public string ArticleSubject
        {
            get
            {
                return articleSubject;
            }
        }

        public string ArticleBody
        {
            get
            {
                return articleBody;
            }
        }

        public NewsIcon Icon
        {
            get
            {
                if (icon == null || icon.Id != newsIconId)
                {
                    icon = new NewsIcon(core, newsIconId);
                    return icon;
                }
                else
                {
                    return icon;
                }
            }
        }
		
		public Primitive Owner
        {
            get
            {
                if (owner == null || ownerKey.Id != owner.Id || ownerKey.TypeId != owner.TypeId)
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
		
		public User Poster
        {
            get
            {
                if (poster == null || userId != poster.Id)
                {
                    core.PrimitiveCache.LoadUserProfile(userId);
                    poster = core.PrimitiveCache[userId];
                    return poster;
                }
                else
                {
                    return poster;
                }
            }
        }
		
		public DateTime GetCreatedDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(articleTime);
        }

        public Article(Core core, long articleId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Article_ItemLoad);

            try
            {
                LoadItem(articleId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidArticleException();
            }
        }
		
		public Article(Core core, DataRow articleDataRow)
			: base(core)
		{
			ItemLoad += new ItemLoadHandler(Article_ItemLoad);
			
			try
			{
				loadItemInfo(articleDataRow);
			}
		    catch (InvalidItemException)
			{
				throw new InvalidArticleException();
			}
		}

        void Article_ItemLoad()
		{
		}

        public override long Id
        {
            get
            {
                return articleId;
            }
        }

        public override string Uri
        {
            get
            {
                return Owner.UriStub + "news/" + articleId.ToString();
            }
        }
		
        public long Comments
        {
            get
            {
                return Info.Comments;
            }
        }

        public SortOrder CommentSortOrder
        {
            get
            {
                return SortOrder.Ascending;
            }
        }

        public byte CommentsPerPage
        {
            get
            {
                return 10;
            }
        }

        public static Article Create(Core core, News news, string subject, string body)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (news == null)
            {
                throw new InvalidNewsException();
            }

            // TODO: fix this
            Item item = Item.Create(core, typeof(Article), new FieldValuePair("article_item_id", news.Owner.Id),
                new FieldValuePair("article_item_type_id", news.Owner.TypeId),
                new FieldValuePair("article_time_ut", UnixTime.UnixTimeStamp()),
                new FieldValuePair("article_subject", subject),
                new FieldValuePair("article_body", body),
                new FieldValuePair("user_id", core.LoggedInMemberId));

            return (Article)item;
        }

        public static void Show(object sender, ShowGPageEventArgs e)
		{
			e.Template.SetTemplate("News", "viewnewsarticle");
			
			Article article = null;
			
			try
			{
				article = new Article(e.Core, e.ItemId);
			}
			catch (InvalidArticleException)
			{
                e.Core.Functions.Generate404();
			}

            e.Core.Display.ParseBbcode(e.Template, "ARTICLE_BODY", article.ArticleBody);
            e.Template.Parse("ARTICLE_TITLE", article.ArticleSubject);
            e.Template.Parse("ARTICLE_U_ARTICLE", article.Uri);
            e.Template.Parse("ARTICLE_U_POSTER", article.Poster.Uri);
            e.Template.Parse("ARTICLE_USERNAME", article.Poster.DisplayName);
            e.Template.Parse("ARTICLE_COMMENTS", article.Comments.ToString());
            e.Template.Parse("ARTICLE_DATE", e.Core.Tz.DateTimeToString(article.GetCreatedDate(e.Core.Tz)));

            // PaginationOptions.Blog
            e.Core.Display.ParsePagination(e.Template, article.Uri, 10, article.Comments);
			
			List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "news", "News" });
			breadCrumbParts.Add(new string[] { e.ItemId.ToString(), article.ArticleSubject });

            e.Page.Group.ParseBreadCrumbs(breadCrumbParts);


            e.Template.Parse("CAN_COMMENT", "TRUE");
            e.Core.Display.DisplayComments(e.Template, e.Page.Group, article);
		}

        public List<ItemTag> GetTags()
        {
            throw new NotImplementedException();
        }

        public ItemTag Add(string tag)
        {
            throw new NotImplementedException();
        }
    }

    public class InvalidArticleException : Exception
    {
    }
}
