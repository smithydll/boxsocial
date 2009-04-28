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
    [DataTable("forum_members")]
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
		
		public long ForumRankId
		{
			get
			{
				return forumRank;
			}
		}
		
		public long ForumPosts
		{
			get
			{
				return forumPosts;
			}
			set
			{
				SetProperty("forumPosts", value);
			}
		}
		
		public string ForumSignature
		{
			get
			{
				return forumSignature;
			}
			set
			{
				SetProperty("forumSignature", value);
			}
		}

        public ForumMember(Core core, Primitive owner, User user)
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
			
			SelectQuery sQuery = ForumMember.GetSelectQueryStub(typeof(ForumMember));
			sQuery.AddCondition("user_id", user.Id);
			sQuery.AddCondition("item_id", owner.Id);
			sQuery.AddCondition("item_type_id", owner.TypeId);

            try
            {
                loadItemInfo(typeof(ForumMember), core.db.ReaderQuery(sQuery));
            }
            catch (InvalidItemException)
            {
                throw new InvalidForumMemberException();
            }
        }

        public ForumMember(Core core, DataRow memberRow, UserLoadOptions loadOptions)
            : base(core, memberRow, loadOptions)
        {
            loadItemInfo(typeof(ForumMember), memberRow);
        }
		
		public static ForumMember Create(Core core, Primitive owner, User user, bool firstPost)
		{
			InsertQuery iQuery = new InsertQuery(GetTable(typeof(ForumMember)));
			iQuery.AddField("user_id", user.Id);
			iQuery.AddField("item_id", owner.Id);
			iQuery.AddField("item_type_id", owner.TypeId);
			iQuery.AddField("posts", (firstPost) ? 1 : 0);
			iQuery.AddField("rank", 0);
			iQuery.AddField("signature", "");
			
			
			core.db.Query(iQuery);
			
			return new ForumMember(core, owner, user);
		}

        // cannot use this method for conversion because we do not have public
        // access to the core token
        /*public static explicit operator ForumMember(User u)
        {
            return new ForumMember(u);
        }*/
		
		public static new SelectQuery GetSelectQueryStub(UserLoadOptions loadOptions)
        {
            SelectQuery query = GetSelectQueryStub(typeof(ForumMember));
            query.AddFields(User.GetFieldsPrefixed(typeof(User)));
            query.AddJoin(JoinTypes.Inner, User.GetTable(typeof(User)), "user_id", "user_id");
            if ((loadOptions & UserLoadOptions.Info) == UserLoadOptions.Info)
            {
                query.AddFields(UserInfo.GetFieldsPrefixed(typeof(UserInfo)));
                query.AddJoin(JoinTypes.Inner, UserInfo.GetTable(typeof(UserInfo)), "user_id", "user_id");
            }
            if ((loadOptions & UserLoadOptions.Profile) == UserLoadOptions.Profile)
            {
                query.AddFields(UserProfile.GetFieldsPrefixed(typeof(UserProfile)));
                query.AddJoin(JoinTypes.Inner, UserProfile.GetTable(typeof(UserProfile)), "user_id", "user_id");
                query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_country"), new DataField("countries", "country_iso"));
                query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_religion"), new DataField("religions", "religion_id"));
            }
            if ((loadOptions & UserLoadOptions.Icon) == UserLoadOptions.Icon)
            {
                query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
                query.AddJoin(JoinTypes.Left, new DataField("user_info", "user_icon"), new DataField("gallery_items", "gallery_item_id"));
            }

            return query;
        }
		
		public static Dictionary<long, ForumMember> GetMembers(Core core, Primitive forumOwner, List<long> userIds)
		{
			Dictionary<long, ForumMember> forumMembers = new Dictionary<long, ForumMember>();
			SelectQuery sQuery = ForumMember.GetSelectQueryStub(UserLoadOptions.All);
			sQuery.AddCondition("user_keys.user_id", ConditionEquality.In, userIds);
			
			DataTable membersTable = core.db.Query(sQuery);
			
			foreach (DataRow dr in membersTable.Rows)
			{
				ForumMember fm = new ForumMember(core, dr, UserLoadOptions.All);
				forumMembers.Add(fm.Id, fm);
			}
			
			return forumMembers;
		}
    }

    public class InvalidForumMemberException : Exception
    {
    }
}
