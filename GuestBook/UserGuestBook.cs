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
using System.Collections.Generic;
using System.Reflection;
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

        public event CommentHandler OnCommentPosted;

        public UserGuestBook(Core core, User owner) : base(core)
        {
            this.db = core.Db;

            this.owner = owner;

            OnCommentPosted += new CommentHandler(UserGuestBook_CommentPosted);
        }

        bool UserGuestBook_CommentPosted(CommentPostedEventArgs e)
        {
            User userProfile = new User(core, e.ItemId);

            Template notificationTemplate = new Template(Assembly.GetExecutingAssembly(), "user_guestbook_notification");
            notificationTemplate.Parse("U_PROFILE", e.Comment.BuildUri(this));
            notificationTemplate.Parse("POSTER", e.Poster.DisplayName);
            notificationTemplate.Parse("COMMENT", e.Comment.Body);

            ApplicationEntry ae = core.GetApplication("GuestBook");
            ae.SendNotification(core, owner, e.Comment.ItemKey, string.Format("[user]{0}[/user] commented on your guest book.", e.Poster.Id), notificationTemplate.ToString());

            return true;
        }

        public void CommentPosted(CommentPostedEventArgs e)
        {
            if (OnCommentPosted != null)
            {
                OnCommentPosted(e);
            }
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
            return core.Hyperlink.AppendSid(string.Format("{0}/{1}",
                    core.Hyperlink.StripSid(Uri), user.UserName));
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

        public string Noun
        {
            get
            {
                return "guest book";
            }
        }
    }
}
