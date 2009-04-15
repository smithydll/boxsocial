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
using System.ComponentModel;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;

namespace BoxSocial.Applications.Forum
{
    [PseudoPrimitive]
    [DataTable("group_members")]
    public class ForumMember : User
    {
        [DataField("user_id")]
        private new long userId;
        [DataField("item")]
        private ItemKey itemKey;
        [DataField("posts")]
        private long forumPosts;
        [DataField("rank")]
        private long forumRank;
        [DataField("signature")]
        private string forumSignature;

        public ForumMember(Core core, User user)
            : base(core)
        {
            // load the info into a the new object being created
            this.userInfo = user.Info;
            this.userProfile = user.Profile;
            this.userStyle = user.Style;
            this.userId = user.UserId;
            this.userName = user.UserName;
            this.domain = user.UserDomain;
            this.emailAddresses = user.EmailAddresses;

            try
            {
                LoadItem(user.Id);
            }
            catch (InvalidItemException)
            {
                throw new InvalidForumMemberException();
            }
        }

        public ForumMember(Core core, DataRow memberRow, UserLoadOptions loadOptions)
            : base(core, memberRow, loadOptions)
        {
            loadItemInfo(typeof(GroupMember), memberRow);
        }

        // cannot use this method for conversion because we do not have public
        // access to the core token
        /*public static explicit operator ForumMember(User u)
        {
            return new ForumMember(u);
        }*/
    }

    public class InvalidForumMemberException : Exception
    {
    }
}
