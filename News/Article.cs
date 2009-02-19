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
        private long articleBody;
		[DataField("article_comments")]
        private long articleComments;
		
		public long ArticleId
		{
			get
			{
				return articleId;
			}
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
                throw new NotImplementedException();
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
    }

    public class InvalidArticleException : Exception
    {
    }
}
