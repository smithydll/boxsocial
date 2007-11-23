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
using System.Text.RegularExpressions;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class CommentHandle : IComparable
    {
        private string token;

        /// <summary>
        /// The function that gets executed if a match
        /// </summary>
        private Core.CommentHandler canPostComment;
        private Core.CommentHandler canDeleteComment;
        private Core.CommentCountHandler adjustCommentCount;

        public CommentHandle(string token, Core.CommentHandler canPostComment, Core.CommentHandler canDeleteComment, Core.CommentCountHandler adjustCommentCount)
        {
            this.token = token;
            this.canPostComment = canPostComment;
            this.canDeleteComment = canDeleteComment;
            this.adjustCommentCount = adjustCommentCount;
        }

        public bool CanPostComment(long itemId, Member viewer)
        {
            return canPostComment(itemId, viewer);
        }

        public bool CanDeleteComment(long itemId, Member viewer)
        {
            return canDeleteComment(itemId, viewer);
        }

        public void AdjustCommentCount(long itemId, int adjustment)
        {
            adjustCommentCount(itemId, adjustment);
        }

        public int CompareTo(object obj)
        {
            if (!(obj is CommentHandle)) return -1;
            return token.CompareTo(((CommentHandle)obj).token);
        }
    }
}
