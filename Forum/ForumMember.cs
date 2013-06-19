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
        private new ItemKey itemKey;
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
			
			SelectQuery sQuery = ForumMember.GetSelectQueryStub(typeof(ForumMember));
			sQuery.AddCondition("user_id", user.Id);
			sQuery.AddCondition("item_id", owner.Id);
			sQuery.AddCondition("item_type_id", owner.TypeId);

            try
            {
                loadItemInfo(typeof(ForumMember), core.Db.ReaderQuery(sQuery));
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

            loadItemInfo(typeof(ForumMember), memberRow);
        }

        public ForumMember(Core core, Primitive owner, long userId, UserLoadOptions loadOptions)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ForumMember_ItemLoad);

            SelectQuery query = GetSelectQueryStub(UserLoadOptions.All);
            query.AddCondition("user_keys.user_id", userId);
            query.AddCondition("item_id", owner.Id);
            query.AddCondition("item_type_id", owner.TypeId);
			
            DataTable memberTable = db.Query(query);

            if (memberTable.Rows.Count == 1)
            {
                DataRow userRow = memberTable.Rows[0];

                loadItemInfo(typeof(ForumMember), userRow);
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
			
			return new ForumMember(core, owner, user);
		}

        public new Primitive Owner
        {
            get
            {
                if (owner == null || itemKey.Id != owner.Id || itemKey.TypeId != owner.TypeId)
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(itemKey);
                    owner = core.PrimitiveCache[itemKey];
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
			SelectQuery sQuery = ForumMember.GetSelectQueryStub(UserLoadOptions.All);
			sQuery.AddCondition("user_keys.user_id", ConditionEquality.In, userIds);
			sQuery.AddCondition("item_id", forumOwner.Id);
			sQuery.AddCondition("item_type_id", forumOwner.TypeId);
			
			DataTable membersTable = core.Db.Query(sQuery);
			
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
			sQuery.AddCondition("item_id", forumOwner.Id);
			sQuery.AddCondition("item_type_id", forumOwner.TypeId);
            if (!string.IsNullOrEmpty(filter))
            {
                sQuery.AddCondition("user_keys.user_name_first", filter);
            }
            sQuery.LimitCount = perPage;
            sQuery.LimitStart = (page - 1) * perPage;

            DataTable membersTable = core.Db.Query(sQuery);

            foreach (DataRow dr in membersTable.Rows)
            {
                ForumMember fm = new ForumMember(core, dr, UserLoadOptions.All);
                forumMembers.Add(fm.Id, fm);
            }

            return forumMembers;
        }

        public static string GenerateMemberlistUri(Core core, Primitive primitive)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}forum/memberlist",
                primitive.UriStub));
        }

        public static string GenerateMemberlistUri(Core core, Primitive primitive, string filter)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}forum/memberlist?filter={1}",
                primitive.UriStub, filter));
        }

        public static void ShowUCP(object sender, ShowPPageEventArgs e)
        {
            e.Template.SetTemplate("Forum", "ucp");
            ForumSettings.ShowForumHeader(e.Core, e.Page);

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

            if (!string.IsNullOrEmpty(e.Core.Http.Form["submit"]))
            {
                Save(e.Core, e.Page);
            }
        }

        public static void ShowMemberlist(Core core, GPage page)
        {
            page.template.SetTemplate("Forum", "memberlist");
            ForumSettings.ShowForumHeader(core, page);

            page.template.Parse("U_FILTER_ALL", GenerateMemberlistUri(core, page.Group));
            page.template.Parse("U_FILTER_BEGINS_A", GenerateMemberlistUri(core, page.Owner, "a"));
            page.template.Parse("U_FILTER_BEGINS_B", GenerateMemberlistUri(core, page.Owner, "b"));
            page.template.Parse("U_FILTER_BEGINS_C", GenerateMemberlistUri(core, page.Owner, "c"));
            page.template.Parse("U_FILTER_BEGINS_D", GenerateMemberlistUri(core, page.Owner, "d"));
            page.template.Parse("U_FILTER_BEGINS_E", GenerateMemberlistUri(core, page.Owner, "e"));
            page.template.Parse("U_FILTER_BEGINS_F", GenerateMemberlistUri(core, page.Owner, "f"));
            page.template.Parse("U_FILTER_BEGINS_G", GenerateMemberlistUri(core, page.Owner, "g"));
            page.template.Parse("U_FILTER_BEGINS_H", GenerateMemberlistUri(core, page.Owner, "h"));
            page.template.Parse("U_FILTER_BEGINS_I", GenerateMemberlistUri(core, page.Owner, "i"));
            page.template.Parse("U_FILTER_BEGINS_J", GenerateMemberlistUri(core, page.Owner, "j"));
            page.template.Parse("U_FILTER_BEGINS_K", GenerateMemberlistUri(core, page.Owner, "k"));
            page.template.Parse("U_FILTER_BEGINS_L", GenerateMemberlistUri(core, page.Owner, "l"));
            page.template.Parse("U_FILTER_BEGINS_M", GenerateMemberlistUri(core, page.Owner, "m"));
            page.template.Parse("U_FILTER_BEGINS_N", GenerateMemberlistUri(core, page.Owner, "n"));
            page.template.Parse("U_FILTER_BEGINS_O", GenerateMemberlistUri(core, page.Owner, "o"));
            page.template.Parse("U_FILTER_BEGINS_P", GenerateMemberlistUri(core, page.Owner, "p"));
            page.template.Parse("U_FILTER_BEGINS_Q", GenerateMemberlistUri(core, page.Owner, "q"));
            page.template.Parse("U_FILTER_BEGINS_R", GenerateMemberlistUri(core, page.Owner, "r"));
            page.template.Parse("U_FILTER_BEGINS_S", GenerateMemberlistUri(core, page.Owner, "s"));
            page.template.Parse("U_FILTER_BEGINS_T", GenerateMemberlistUri(core, page.Owner, "t"));
            page.template.Parse("U_FILTER_BEGINS_U", GenerateMemberlistUri(core, page.Owner, "u"));
            page.template.Parse("U_FILTER_BEGINS_V", GenerateMemberlistUri(core, page.Owner, "v"));
            page.template.Parse("U_FILTER_BEGINS_W", GenerateMemberlistUri(core, page.Owner, "w"));
            page.template.Parse("U_FILTER_BEGINS_X", GenerateMemberlistUri(core, page.Owner, "x"));
            page.template.Parse("U_FILTER_BEGINS_Y", GenerateMemberlistUri(core, page.Owner, "y"));
            page.template.Parse("U_FILTER_BEGINS_Z", GenerateMemberlistUri(core, page.Owner, "z"));

            Dictionary<long, ForumMember> members = ForumMember.GetMembers(core, page.Owner, core.Functions.GetFilter(), page.TopLevelPageNumber, 20);

            foreach (ForumMember member in members.Values)
            {
                VariableCollection memberVariableCollection = page.template.CreateChild("member_list");

                memberVariableCollection.Parse("USER_DISPLAY_NAME", member.DisplayName);
                //memberVariableCollection.Parse("JOIN_DATE", page.tz.DateTimeToString(member.GetGroupMemberJoinDate(page.tz)));
                memberVariableCollection.Parse("USER_COUNTRY", member.Profile.Country);

                memberVariableCollection.Parse("U_PROFILE", member.Uri);

                memberVariableCollection.Parse("POSTS", member.ForumPosts.ToString());
            }
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

                page.template.Parse("REDIRECT_URI", core.Hyperlink.AppendSid(string.Format("{0}forum/ucp",
                    page.Owner.UriStub)));
            }
        }
    }

    public class InvalidForumMemberException : Exception
    {
    }
}
