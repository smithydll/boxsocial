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
    [DataTable("news_article")]
    public class Article : NumberedItem
    {
        [DataField("article_id", DataFieldKeys.Primary)]
        private long articleId;

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
    }

    public class InvalidArticleException : Exception
    {
    }
}
