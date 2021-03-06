﻿/*
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
        private ItemKey ownerKey;
        [DataField("posts")]
        private long forumPosts;
        [DataField("rank")]
        private long forumRank;
        [DataField("signature", 255)]
        private string forumSignature;

        private Access access;
        private Primitive owner;

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
                SetPropertyByRef(new { forumSignature }, value);
			}
		}

        public override Access Access
        {
            get
            {
                if (access == null)
                {
                    access = new Access(core, this);
                }

                return access;
            }
        }

        public ForumMember(Core core, Primitive owner, User user)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ForumMember_ItemLoad);

            // load the info into a the new object being created
            this.userInfo = user.UserInfo;
            this.userProfile = user.Profile;
            this.userStyle = user.Style;
            this.userId = user.UserId;
            this.userName = user.UserName;
            this.domain = user.UserDomain;
            this.emailAddresses = user.EmailAddresses;
            this.owner = owner;

            SelectQuery sQuery = GetSelectQueryStub(core, UserLoadOptions.All);
            sQuery.AddCondition("user_keys.user_id", user.Id);
			sQuery.AddCondition("item_id", owner.Id);
			sQuery.AddCondition("item_type_id", owner.TypeId);

            try
            {
                System.Data.Common.DbDataReader memberReader = core.Db.ReaderQuery(sQuery);

                if (memberReader.HasRows)
                {
                    memberReader.Read();

                    loadItemInfo(memberReader);

                    memberReader.Close();
                    memberReader.Dispose();
                }
                else
                {
                    memberReader.Close();
                    memberReader.Dispose();

                    throw new InvalidForumMemberException();
                }
            }
            catch (InvalidItemException)
            {
                throw new InvalidForumMemberException();
            }
        }

        public ForumMember(Core core, DataRow memberRow, UserLoadOptions loadOptions)
            : base(core, memberRow, loadOptions)
        {
            ItemLoad += new ItemLoadHandler(ForumMember_ItemLoad);

            loadItemInfo(memberRow);
        }

        public ForumMember(Core core, System.Data.Common.DbDataReader memberRow, UserLoadOptions loadOptions)
            : base(core, memberRow, loadOptions)
        {
            ItemLoad += new ItemLoadHandler(ForumMember_ItemLoad);

            loadItemInfo(memberRow);
        }

        public ForumMember(Core core, Primitive owner, long userId, UserLoadOptions loadOptions)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ForumMember_ItemLoad);

            SelectQuery query = GetSelectQueryStub(core, UserLoadOptions.All);
            query.AddCondition("user_keys.user_id", userId);
            query.AddCondition("item_id", owner.Id);
            query.AddCondition("item_type_id", owner.TypeId);
			
            DataTable memberTable = db.Query(query);

            if (memberTable.Rows.Count == 1)
            {
                DataRow userRow = memberTable.Rows[0];

                loadItemInfo(userRow);
                loadItemInfo(typeof(User), userRow);

                if ((loadOptions & UserLoadOptions.Info) == UserLoadOptions.Info)
                {
                    userInfo = new UserInfo(core, userRow);
                }

                if ((loadOptions & UserLoadOptions.Profile) == UserLoadOptions.Profile)
                {
                    userProfile = new UserProfile(core, this, userRow, loadOptions);
                }

                /*if ((loadOptions & UserLoadOptions.Icon) == UserLoadOptions.Icon)
                {
                    loadUserIcon(userRow);
                }*/
            }
            else
            {
                throw new InvalidUserException();
            }
        }

        protected override void loadItemInfo(DataRow memberRow)
        {
            try
            {
                loadValue(memberRow, "user_id", out userId);
                loadValue(memberRow, "item", out ownerKey);
                loadValue(memberRow, "posts", out forumPosts);
                loadValue(memberRow, "rank", out forumRank);
                loadValue(memberRow, "signature", out forumSignature);

                itemLoaded(memberRow);
                core.ItemCache.RegisterItem((NumberedItem)this);
            }
            catch
            {
                throw new InvalidItemException();
            }
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader memberRow)
        {
            try
            {
                loadValue(memberRow, "user_id", out userId);
                loadValue(memberRow, "item", out ownerKey);
                loadValue(memberRow, "posts", out forumPosts);
                loadValue(memberRow, "rank", out forumRank);
                loadValue(memberRow, "signature", out forumSignature);

                itemLoaded(memberRow);
                core.ItemCache.RegisterItem((NumberedItem)this);
            }
            catch
            {
                throw new InvalidItemException();
            }
        }

        void ForumMember_ItemLoad()
        {
            base.userId = this.userId;
        }
		
		public static ForumMember Create(Core core, Primitive owner, User user, bool firstPost)
		{
            if (core == null)
            {
                throw new NullCoreException();
            }

			InsertQuery iQuery = new InsertQuery(GetTable(typeof(ForumMember)));
			iQuery.AddField("user_id", user.Id);
			iQuery.AddField("item_id", owner.Id);
			iQuery.AddField("item_type_id", owner.TypeId);
			iQuery.AddField("posts", (firstPost) ? 1 : 0);
			iQuery.AddField("rank", 0);
			iQuery.AddField("signature", "");
			
			core.Db.Query(iQuery);

            UpdateQuery uQuery = new UpdateQuery(typeof(ForumSettings));
            uQuery.AddCondition("forum_item_id", owner.Id);
            uQuery.AddCondition("forum_item_type_id", owner.TypeId);
            uQuery.AddField("forum_members", new QueryOperation("forum_members", QueryOperations.Addition, 1));

            core.Db.Query(uQuery);
			
			return new ForumMember(core, owner, user);
		}

        public new Primitive Owner
        {
            get
            {
                if (owner == null || ownerKey.Id != owner.Id || ownerKey.TypeId != owner.TypeId)
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(ownerKey);
                    owner = core.PrimitiveCache[ownerKey];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

        public override bool CanEditItem()
        {
            if (userId == core.LoggedInMemberId)
            {
                return true;
            }
            if (Owner != null && Owner.CanEditItem())
            {
                return true;
            }

            return false;
        }

        // cannot use this method for conversion because we do not have public
        // access to the core token
        /*public static explicit operator ForumMember(User u)
        {
            return new ForumMember(u);
        }*/
		
		public static new SelectQuery GetSelectQueryStub(Core core, UserLoadOptions loadOptions)
        {
            SelectQuery query = GetSelectQueryStub(core, typeof(ForumMember));
            query.AddFields(User.GetFieldsPrefixed(core, typeof(User)));
            query.AddFields(ItemInfo.GetFieldsPrefixed(core, typeof(ItemInfo)));
            query.AddJoin(JoinTypes.Inner, User.GetTable(typeof(User)), "user_id", "user_id");

            TableJoin join = query.AddJoin(JoinTypes.Left, new DataField(typeof(ForumMember), "user_id"), new DataField(typeof(ItemInfo), "info_item_id"));
            join.AddCondition(new DataField(typeof(ItemInfo), "info_item_type_id"), ItemKey.GetTypeId(core, typeof(User)));

            if ((loadOptions & UserLoadOptions.Info) == UserLoadOptions.Info)
            {
                query.AddFields(UserInfo.GetFieldsPrefixed(core, typeof(UserInfo)));
                query.AddJoin(JoinTypes.Inner, UserInfo.GetTable(typeof(UserInfo)), "user_id", "user_id");
            }
            if ((loadOptions & UserLoadOptions.Profile) == UserLoadOptions.Profile)
            {
                query.AddFields(UserProfile.GetFieldsPrefixed(core, typeof(UserProfile)));
                query.AddJoin(JoinTypes.Inner, UserProfile.GetTable(typeof(UserProfile)), "user_id", "user_id");
                query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_country"), new DataField("countries", "country_iso"));
                query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_religion"), new DataField("religions", "religion_id"));
            }
            /*if ((loadOptions & UserLoadOptions.Icon) == UserLoadOptions.Icon)
            {
                query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
                query.AddJoin(JoinTypes.Left, new DataField("user_info", "user_icon"), new DataField("gallery_items", "gallery_item_id"));
            }*/

            return query;
        }
		
		public static Dictionary<long, ForumMember> GetMembers(Core core, Primitive forumOwner, List<long> userIds)
		{
            if (userIds == null || userIds.Count == 0)
            {
                return new Dictionary<long, ForumMember>();
            }

			Dictionary<long, ForumMember> forumMembers = new Dictionary<long, ForumMember>();
            SelectQuery sQuery = ForumMember.GetSelectQueryStub(core, UserLoadOptions.All);
			sQuery.AddCondition("user_keys.user_id", ConditionEquality.In, userIds);
			sQuery.AddCondition("item_id", forumOwner.Id);
			sQuery.AddCondition("item_type_id", forumOwner.TypeId);

            System.Data.Common.DbDataReader membersReader = core.Db.ReaderQuery(sQuery);
			
			while (membersReader.Read())
			{
                ForumMember fm = new ForumMember(core, membersReader, UserLoadOptions.All);
				forumMembers.Add(fm.Id, fm);
			}

            membersReader.Close();
            membersReader.Dispose();
			
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
            SelectQuery sQuery = ForumMember.GetSelectQueryStub(core, UserLoadOptions.All);
			sQuery.AddCondition("item_id", forumOwner.Id);
			sQuery.AddCondition("item_type_id", forumOwner.TypeId);
            if (!string.IsNullOrEmpty(filter))
            {
                sQuery.AddCondition("user_keys.user_name_first", filter);
            }
            sQuery.LimitCount = perPage;
            sQuery.LimitStart = (page - 1) * perPage;

            System.Data.Common.DbDataReader membersReader = core.Db.ReaderQuery(sQuery);

            while (membersReader.Read())
            {
                ForumMember fm = new ForumMember(core, membersReader, UserLoadOptions.All);
                forumMembers.Add(fm.Id, fm);
            }

            membersReader.Close();
            membersReader.Dispose();

            return forumMembers;
        }

        public static string GenerateMemberlistUri(Core core, Primitive primitive)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}forum/memberlist",
                primitive.UriStub));
        }

        public static string GenerateMemberlistUri(Core core, Primitive primitive, string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                return GenerateMemberlistUri(core, primitive);
            }
            else
            {
                return core.Hyperlink.AppendSid(string.Format("{0}forum/memberlist?filter={1}",
                    primitive.UriStub, filter));
            }
        }

        public static void ShowUCP(object sender, ShowPPageEventArgs e)
        {
            e.Template.SetTemplate("Forum", "ucp");
            ForumSettings.ShowForumHeader(e.Core, e.Page);

            e.Template.Parse("PAGE_TITLE", e.Core.Prose.GetString("USER_CONTROL_PANEL"));

            if (e.Core.Session.IsLoggedIn && e.Core.Session.LoggedInMember != null)
            {
                e.Template.Parse("S_POST", e.Core.Hyperlink.AppendSid(string.Format("{0}forum/ucp",
                    e.Page.Owner.UriStub), true));
				
				try
				{
                    ForumMember member = new ForumMember(e.Core, e.Page.Owner, e.Core.Session.LoggedInMember);

                	e.Template.Parse("S_SIGNATURE", member.forumSignature);
				}
				catch (InvalidForumMemberException)
				{
					// create on submit
				}
            }
            else
            {
                e.Core.Functions.Generate403();
                return;
            }

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "forum", e.Core.Prose.GetString("FORUM") });
            breadCrumbParts.Add(new string[] { "ucp", e.Core.Prose.GetString("USER_CONTROL_PANEL") });

            e.Page.Owner.ParseBreadCrumbs(breadCrumbParts);

            if (!string.IsNullOrEmpty(e.Core.Http.Form["submit"]))
            {
                Save(e.Core, e.Page);
            }
        }

        public static void ShowMemberlist(Core core, GPage page)
        {
            core.Template.SetTemplate("Forum", "memberlist");
            ForumSettings.ShowForumHeader(core, page);
            ForumSettings settings = new ForumSettings(core, page.Owner);

            core.Template.Parse("PAGE_TITLE", core.Prose.GetString("MEMBERLIST"));

            core.Template.Parse("U_FILTER_ALL", GenerateMemberlistUri(core, page.Group));
            core.Template.Parse("U_FILTER_BEGINS_A", GenerateMemberlistUri(core, page.Owner, "a"));
            core.Template.Parse("U_FILTER_BEGINS_B", GenerateMemberlistUri(core, page.Owner, "b"));
            core.Template.Parse("U_FILTER_BEGINS_C", GenerateMemberlistUri(core, page.Owner, "c"));
            core.Template.Parse("U_FILTER_BEGINS_D", GenerateMemberlistUri(core, page.Owner, "d"));
            core.Template.Parse("U_FILTER_BEGINS_E", GenerateMemberlistUri(core, page.Owner, "e"));
            core.Template.Parse("U_FILTER_BEGINS_F", GenerateMemberlistUri(core, page.Owner, "f"));
            core.Template.Parse("U_FILTER_BEGINS_G", GenerateMemberlistUri(core, page.Owner, "g"));
            core.Template.Parse("U_FILTER_BEGINS_H", GenerateMemberlistUri(core, page.Owner, "h"));
            core.Template.Parse("U_FILTER_BEGINS_I", GenerateMemberlistUri(core, page.Owner, "i"));
            core.Template.Parse("U_FILTER_BEGINS_J", GenerateMemberlistUri(core, page.Owner, "j"));
            core.Template.Parse("U_FILTER_BEGINS_K", GenerateMemberlistUri(core, page.Owner, "k"));
            core.Template.Parse("U_FILTER_BEGINS_L", GenerateMemberlistUri(core, page.Owner, "l"));
            core.Template.Parse("U_FILTER_BEGINS_M", GenerateMemberlistUri(core, page.Owner, "m"));
            core.Template.Parse("U_FILTER_BEGINS_N", GenerateMemberlistUri(core, page.Owner, "n"));
            core.Template.Parse("U_FILTER_BEGINS_O", GenerateMemberlistUri(core, page.Owner, "o"));
            core.Template.Parse("U_FILTER_BEGINS_P", GenerateMemberlistUri(core, page.Owner, "p"));
            core.Template.Parse("U_FILTER_BEGINS_Q", GenerateMemberlistUri(core, page.Owner, "q"));
            core.Template.Parse("U_FILTER_BEGINS_R", GenerateMemberlistUri(core, page.Owner, "r"));
            core.Template.Parse("U_FILTER_BEGINS_S", GenerateMemberlistUri(core, page.Owner, "s"));
            core.Template.Parse("U_FILTER_BEGINS_T", GenerateMemberlistUri(core, page.Owner, "t"));
            core.Template.Parse("U_FILTER_BEGINS_U", GenerateMemberlistUri(core, page.Owner, "u"));
            core.Template.Parse("U_FILTER_BEGINS_V", GenerateMemberlistUri(core, page.Owner, "v"));
            core.Template.Parse("U_FILTER_BEGINS_W", GenerateMemberlistUri(core, page.Owner, "w"));
            core.Template.Parse("U_FILTER_BEGINS_X", GenerateMemberlistUri(core, page.Owner, "x"));
            core.Template.Parse("U_FILTER_BEGINS_Y", GenerateMemberlistUri(core, page.Owner, "y"));
            core.Template.Parse("U_FILTER_BEGINS_Z", GenerateMemberlistUri(core, page.Owner, "z"));

            Dictionary<long, ForumMember> members = ForumMember.GetMembers(core, page.Owner, core.Functions.GetFilter(), page.TopLevelPageNumber, 20);

            foreach (ForumMember member in members.Values)
            {
                VariableCollection memberVariableCollection = core.Template.CreateChild("member_list");

                memberVariableCollection.Parse("USER_DISPLAY_NAME", member.DisplayName);
                //memberVariableCollection.Parse("JOIN_DATE", page.tz.DateTimeToString(member.GetGroupMemberJoinDate(page.tz)));
                memberVariableCollection.Parse("USER_COUNTRY", member.Profile.Country);

                memberVariableCollection.Parse("U_PROFILE", member.Uri);

                memberVariableCollection.Parse("POSTS", member.ForumPosts.ToString());

                memberVariableCollection.Parse("ICON", member.Icon);
                memberVariableCollection.Parse("TILE", member.Tile);
                memberVariableCollection.Parse("MOBILE_COVER", member.MobileCoverPhoto);

                memberVariableCollection.Parse("ID", member.Id);
                memberVariableCollection.Parse("TYPE", member.TypeId);
                memberVariableCollection.Parse("LOCATION", member.Profile.Country);
                memberVariableCollection.Parse("ABSTRACT", page.Core.Bbcode.Parse(member.Profile.Autobiography));
                memberVariableCollection.Parse("SUBSCRIBERS", member.Info.Subscribers);

                if (Subscription.IsSubscribed(page.Core, member.ItemKey))
                {
                    memberVariableCollection.Parse("SUBSCRIBERD", "TRUE");
                    memberVariableCollection.Parse("U_SUBSCRIBE", page.Core.Hyperlink.BuildUnsubscribeUri(member.ItemKey));
                }
                else
                {
                    memberVariableCollection.Parse("U_SUBSCRIBE", page.Core.Hyperlink.BuildSubscribeUri(member.ItemKey));
                }

                if (page.Core.Session.SignedIn && member.Id == page.Core.LoggedInMemberId)
                {
                    memberVariableCollection.Parse("ME", "TRUE");
                }
            }

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "forum", core.Prose.GetString("FORUM") });
            breadCrumbParts.Add(new string[] { "memberlist", core.Prose.GetString("MEMBERLIST") });

            page.Owner.ParseBreadCrumbs(breadCrumbParts);

            core.Display.ParsePagination(ForumMember.GenerateMemberlistUri(core, page.Owner, core.Functions.GetFilter()), 20, settings.Members);
        } 

        private static void Save(Core core, PPage page)
        {
            AccountSubModule.AuthoriseRequestSid(core);

            if (core.Session.IsLoggedIn && core.Session.LoggedInMember != null)
            {
                ForumMember member = null;
				
				try
				{
                    member = new ForumMember(core, page.Owner, core.Session.LoggedInMember);
				}
				catch (InvalidForumMemberException)
				{
                    member = ForumMember.Create(core, page.Owner, core.Session.LoggedInMember, false);
				}
                member.ForumSignature = core.Http.Form["signature"];

                member.Update(typeof(ForumMember));

                core.Display.ShowMessage("Profile Updated", "Your forum profile has been saved in the database.");

                core.Template.Parse("REDIRECT_URI", core.Hyperlink.AppendSid(string.Format("{0}forum/ucp",
                    page.Owner.UriStub)));
            }
        }
    }

    public class InvalidForumMemberException : Exception
    {
    }
}
