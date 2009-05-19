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
        [DataField("user_id", DataFieldKeys.Unique, "u_key")]
        private new long userId;
        [DataField("item", DataFieldKeys.Unique, "u_key")]
        private ItemKey itemKey;
        [DataField("posts")]
        private long forumPosts;
        [DataField("rank")]
        private long forumRank;
        [DataField("signature", 255)]
        private string forumSignature;
		
		public long ForumRankId
		{
			get
			{
				return forumRank;
			}
            set
            {
                SetProperty("forumRank", value);
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

        public ForumMember(Core core, Primitive owner, long userId, UserLoadOptions loadOptions)
            : base(core)
        {
            SelectQuery query = GetSelectQueryStub(UserLoadOptions.All);
            query.AddCondition("user_keys.user_id", userId);
            query.AddCondition("item_id", owner.Id);
            query.AddCondition("item_type_id", owner.TypeId);

            DataTable memberTable = db.Query(query);
			
			//HttpContext.Current.Response.Write(query.ToString());

            if (memberTable.Rows.Count == 1)
            {
                loadItemInfo(typeof(ForumMember), memberTable.Rows[0]);
				loadItemInfo(typeof(User), memberTable.Rows[0]);
				loadItemInfo(typeof(UserInfo), memberTable.Rows[0]);
				loadItemInfo(typeof(UserProfile), memberTable.Rows[0]);
                /*loadUserInfo(memberTable.Rows[0]);
                loadUserIcon(memberTable.Rows[0]);*/
            }
            else
            {
                throw new InvalidUserException();
            }
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="forumOwner"></param>
        /// <param name="filter">First character based filter</param>
        /// /// <param name="page"></param>
        /// /// <param name="perPage"></param>
        /// <returns></returns>
        public static Dictionary<long, ForumMember> GetMembers(Core core, Primitive forumOwner, string filter, int page, int perPage)
        {
            Dictionary<long, ForumMember> forumMembers = new Dictionary<long, ForumMember>();
            SelectQuery sQuery = ForumMember.GetSelectQueryStub(UserLoadOptions.All);
            if (!string.IsNullOrEmpty(filter))
            {
                sQuery.AddCondition("user_keys.user_name_first", filter);
            }
            sQuery.LimitCount = perPage;
            sQuery.LimitStart = (page - 1) * perPage;

            DataTable membersTable = core.db.Query(sQuery);

            foreach (DataRow dr in membersTable.Rows)
            {
                ForumMember fm = new ForumMember(core, dr, UserLoadOptions.All);
                forumMembers.Add(fm.Id, fm);
            }

            return forumMembers;
        }

        public static string GenerateMemberlistUri(Primitive primitive)
        {
            return Linker.AppendSid(string.Format("{0}forum/memberlist",
                primitive.UriStub));
        }

        public static string GenerateMemberlistUri(Primitive primitive, string filter)
        {
            return Linker.AppendSid(string.Format("{0}forum/memberlist?filter={1}",
                primitive.UriStub, filter));
        }

        public static void ShowUCP(Core core, GPage page)
        {
            page.template.SetTemplate("Forum", "ucp");
            ForumSettings.ShowForumHeader(core, page);

            if (core.session.IsLoggedIn && core.session.LoggedInMember != null)
            {
                ForumMember member = new ForumMember(core, page.ThisGroup, core.session.LoggedInMember);

                page.template.Parse("S_POST", Linker.AppendSid(string.Format("{0}forum/ucp",
                    ((GPage)page).ThisGroup.UriStub), true));
                page.template.Parse("S_SIGNATURE", member.forumSignature);
            }
            else
            {
                Functions.Generate403();
                return;
            }

            if (!string.IsNullOrEmpty(HttpContext.Current.Request.Form["submit"]))
            {
                Save(core, page);
            }
        }

        public static void ShowMemberlist(Core core, GPage page)
        {
            page.template.SetTemplate("Forum", "memberlist");

            page.template.Parse("U_FILTER_ALL", GenerateMemberlistUri(page.ThisGroup));
            page.template.Parse("U_FILTER_BEGINS_A", GenerateMemberlistUri(page.ThisGroup, "a"));
            page.template.Parse("U_FILTER_BEGINS_B", GenerateMemberlistUri(page.ThisGroup, "b"));
            page.template.Parse("U_FILTER_BEGINS_C", GenerateMemberlistUri(page.ThisGroup, "c"));
            page.template.Parse("U_FILTER_BEGINS_D", GenerateMemberlistUri(page.ThisGroup, "d"));
            page.template.Parse("U_FILTER_BEGINS_E", GenerateMemberlistUri(page.ThisGroup, "e"));
            page.template.Parse("U_FILTER_BEGINS_F", GenerateMemberlistUri(page.ThisGroup, "f"));
            page.template.Parse("U_FILTER_BEGINS_G", GenerateMemberlistUri(page.ThisGroup, "g"));
            page.template.Parse("U_FILTER_BEGINS_H", GenerateMemberlistUri(page.ThisGroup, "h"));
            page.template.Parse("U_FILTER_BEGINS_I", GenerateMemberlistUri(page.ThisGroup, "i"));
            page.template.Parse("U_FILTER_BEGINS_J", GenerateMemberlistUri(page.ThisGroup, "j"));
            page.template.Parse("U_FILTER_BEGINS_K", GenerateMemberlistUri(page.ThisGroup, "k"));
            page.template.Parse("U_FILTER_BEGINS_L", GenerateMemberlistUri(page.ThisGroup, "l"));
            page.template.Parse("U_FILTER_BEGINS_M", GenerateMemberlistUri(page.ThisGroup, "m"));
            page.template.Parse("U_FILTER_BEGINS_N", GenerateMemberlistUri(page.ThisGroup, "n"));
            page.template.Parse("U_FILTER_BEGINS_O", GenerateMemberlistUri(page.ThisGroup, "o"));
            page.template.Parse("U_FILTER_BEGINS_P", GenerateMemberlistUri(page.ThisGroup, "p"));
            page.template.Parse("U_FILTER_BEGINS_Q", GenerateMemberlistUri(page.ThisGroup, "q"));
            page.template.Parse("U_FILTER_BEGINS_R", GenerateMemberlistUri(page.ThisGroup, "r"));
            page.template.Parse("U_FILTER_BEGINS_S", GenerateMemberlistUri(page.ThisGroup, "s"));
            page.template.Parse("U_FILTER_BEGINS_T", GenerateMemberlistUri(page.ThisGroup, "t"));
            page.template.Parse("U_FILTER_BEGINS_U", GenerateMemberlistUri(page.ThisGroup, "u"));
            page.template.Parse("U_FILTER_BEGINS_V", GenerateMemberlistUri(page.ThisGroup, "v"));
            page.template.Parse("U_FILTER_BEGINS_W", GenerateMemberlistUri(page.ThisGroup, "w"));
            page.template.Parse("U_FILTER_BEGINS_X", GenerateMemberlistUri(page.ThisGroup, "x"));
            page.template.Parse("U_FILTER_BEGINS_Y", GenerateMemberlistUri(page.ThisGroup, "y"));
            page.template.Parse("U_FILTER_BEGINS_Z", GenerateMemberlistUri(page.ThisGroup, "z"));
        }

        private static void Save(Core core, GPage page)
        {
            AccountSubModule.AuthoriseRequestSid(core);

            if (core.session.IsLoggedIn && core.session.LoggedInMember != null)
            {
                ForumMember member = new ForumMember(core, page.ThisGroup, core.session.LoggedInMember);
                member.ForumSignature = HttpContext.Current.Request.Form["signature"];

                member.Update(typeof(ForumMember));
				
				Display.ShowMessage("Profile Updated", "Your forum profile has been saved in the database.");
				
				page.template.Parse("REDIRECT_URI", Linker.AppendSid(string.Format("{0}forum/ucp",
                	page.ThisGroup.UriStub)));
            }
        }
    }

    public class InvalidForumMemberException : Exception
    {
    }
}
