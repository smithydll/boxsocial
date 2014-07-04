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
                return core.Prose.GetString("_GUEST_BOOK");
            }
        }


        public void CommentPosted(CommentPostedEventArgs e)
        {
            
        }

        public static void NotifyUserComment(Core core, Job job)
        {
            Comment comment = new Comment(core, job.ItemId);
            core.LoadUserProfile(comment.CommentedItemKey.Id);
            User ev = core.PrimitiveCache[comment.CommentedItemKey.Id];

            if (ev.Owner is User && (!comment.OwnerKey.Equals(ev.ItemKey)))
            {
                core.CallingApplication.SendNotification(core, comment.User, (User)ev.Owner, ev.ItemKey, ev.ItemKey, "_COMMENTED_GUEST_BOOK", comment.BuildUri(ev));
            }

            //core.CallingApplication.SendNotification(core, comment.OwnerKey, ev.ItemKey, string.Format("[user]{0}[/user] commented on [user]{2}[/user] [iurl=\"{1}\"]blog post[/iurl]", comment.OwnerKey.Id, comment.BuildUri(ev), ev.OwnerKey.Id), string.Empty, emailTemplate);
        }
    }
}
