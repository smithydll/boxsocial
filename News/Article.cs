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

namespace BoxSocial.Applications.News
{
    [DataTable("news_article", "ARTICLE")]
    public class Article : NumberedItem, ICommentableItem
    {
        [DataField("article_id", DataFieldKeys.Primary)]
        private long articleId;
		[DataField("user_id")]
        private long userId;
		[DataField("article_item")]
        private ItemKey ownerKey;
		//[DataField("article_item_type", NAMESPACE)]
        //private string ownerType;
		[DataField("article_time_ut")]
        private long articleTime;
		[DataField("article_subject", 127)]
        private string articleSubject;
		[DataField("article_body", MYSQL_TEXT)]
        private string articleBody;
		[DataField("article_comments")]
        private long articleComments;
		
		private Primitive owner;
		private User poster;
		
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
		
		public Primitive Owner
        {
            get
            {
                if (owner == null || ownerKey.Id != owner.Id || ownerKey.Type != owner.Type)
                {
                    core.UserProfiles.LoadPrimitiveProfile(ownerKey.Type, ownerKey.Id);
                    owner = core.UserProfiles[ownerKey.Type, ownerKey.Id];
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
                    core.UserProfiles.LoadUserProfile(userId);
                    poster = core.UserProfiles[userId];
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
                return articleComments;
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
		
		public static void Show(Core core, GPage page, long articleId)
		{
			int p = Functions.RequestInt("p", 1);
			page.template.SetTemplate("News", "viewnewsarticle");
			
			Article article = null;
			
			try
			{
				article = new Article(core, articleId);
			}
			catch (InvalidArticleException)
			{
				Functions.Generate404();
			}
			
			Display.ParseBbcode(page.template, "ARTICLE_BODY", article.ArticleBody);
            page.template.Parse("ARTICLE_TITLE", article.ArticleSubject);
			page.template.Parse("ARTICLE_U_ARTICLE", article.Uri);
			page.template.Parse("ARTICLE_U_POSTER", article.Poster.Uri);
			page.template.Parse("ARTICLE_USERNAME", article.Poster.DisplayName);
			page.template.Parse("ARTICLE_COMMENTS", article.Comments.ToString());
			page.template.Parse("ARTICLE_DATE", core.tz.DateTimeToString(article.GetCreatedDate(core.tz)));
			
			Display.ParsePagination(page.template, "PAGINATION", article.Uri, p, (int)Math.Ceiling((double)article.Comments / 10), false);
			
			List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "news", "News" });
			breadCrumbParts.Add(new string[] { articleId.ToString(), article.ArticleSubject });

            page.ThisGroup.ParseBreadCrumbs(breadCrumbParts);
			

            page.template.Parse("CAN_COMMENT", "TRUE");
            Display.DisplayComments(page.template, page.ThisGroup, article);
		}
    }

    public class InvalidArticleException : Exception
    {
    }
}
