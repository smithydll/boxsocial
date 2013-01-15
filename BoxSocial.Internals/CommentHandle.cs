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
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class CommentHandle : IComparable
    {
        private long itemTypeId;

        /// <summary>
        /// The function that gets executed if a match
        /// </summary>
        private Core.CommentHandler canPostComment;
        private Core.CommentHandler canDeleteComment;
        private Core.CommentCountHandler adjustCommentCount;
        private Core.CommentPostedHandler commentPosted;
        private Core.CommentPostedHandler commentDeleted;

        public CommentHandle(long itemTypeId, Core.CommentHandler canPostComment, Core.CommentHandler canDeleteComment, Core.CommentCountHandler adjustCommentCount, Core.CommentPostedHandler commentPosted, Core.CommentPostedHandler commentDeleted)
        {
            this.itemTypeId = itemTypeId;
            this.canPostComment = canPostComment;
            this.canDeleteComment = canDeleteComment;
            this.adjustCommentCount = adjustCommentCount;
            this.commentPosted = commentPosted;
            this.commentDeleted = commentDeleted;
        }

        public bool CanPostComment(ItemKey itemKey, User viewer)
        {
            return canPostComment(itemKey, viewer);
        }

        public bool CanDeleteComment(ItemKey itemKey, User viewer)
        {
            return canDeleteComment(itemKey, viewer);
        }

        public void AdjustCommentCount(ItemKey itemKey, int adjustment)
        {
            adjustCommentCount(itemKey, adjustment);
        }

        public void CommentPosted(Comment comment, User poster, ItemKey itemKey)
        {
            commentPosted(new CommentPostedEventArgs(comment, poster, itemKey));
        }

        public void CommentDeleted(Comment comment, User poster, ItemKey itemKey)
        {
            if (commentDeleted != null)
            {
                commentDeleted(new CommentPostedEventArgs(comment, poster, itemKey));
            }
        }

        public int CompareTo(object obj)
        {
            if (!(obj is CommentHandle)) return -1;
            return itemTypeId.CompareTo(((CommentHandle)obj).itemTypeId);
        }
    }
}
