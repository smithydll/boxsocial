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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public delegate bool CommentHandler(CommentPostedEventArgs e);

    public interface ICommentableItem
    {
        /// <summary>
        /// Event Handler
        /// </summary>
        event CommentHandler OnCommentPosted;

        void CommentPosted(CommentPostedEventArgs e);

        /// <summary>
        /// Gets the number of comments.
        /// </summary>
        long Comments
        {
            get;
        }

        /// <summary>
        /// Gets the sort order for the comments.
        /// </summary>
        SortOrder CommentSortOrder
        {
            get;
        }

        /// <summary>
        /// Gets the id of the commentable item.
        /// </summary>
        long Id
        {
            get;
        }

        /// <summary>
        /// Gets the class namespace of the commentable item.
        /// </summary>
        string Namespace
        {
            get;
        }
		
		ItemKey ItemKey
		{
			get;
		}

        /// <summary>
        /// Gets the URI for the commentable item.
        /// </summary>
        string Uri
        {
            get;
        }

        /// <summary>
        /// Gets the number of comments to be displayed per page.
        /// </summary>
        byte CommentsPerPage
        {
            get;
        }

        Primitive Owner
        {
            get;
        }

        string Noun
        {
            get;
        }

        [JsonProperty("access_comment")]
        bool CanComment
        {
            get;
        }
    }
}
