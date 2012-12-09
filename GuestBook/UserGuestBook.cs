﻿/*
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
using System.Collections.Generic;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.GuestBook
{
    public class UserGuestBook : NumberedItem, ICommentableItem
    {
        private User owner;

        public UserGuestBook(Core core, User owner) : base(core)
        {
            this.db = core.Db;

            this.owner = owner;
        }

        public override long Id
        {
            get
            {
                return owner.Id;
            }
        }

        public override string Uri
        {
            get
            {
                return GuestBook.Uri(core, owner);
            }
        }

        public long Comments
        {
            get
            {
                return owner.Comments;
            }
        }

        public string BuildConversationUri(User user)
        {
            return core.Uri.AppendSid(string.Format("{0}/{1}",
                    core.Uri.StripSid(Uri), user.UserName));
        }

        public Primitive Owner
        {
            get
            {
                return owner;
            }
        }

        #region ICommentableItem Members


        public SortOrder CommentSortOrder
        {
            get
            {
                return owner.CommentSortOrder;
            }
        }

        public byte CommentsPerPage
        {
            get
            {
                return 10;
            }
        }

        #endregion
    }
}
