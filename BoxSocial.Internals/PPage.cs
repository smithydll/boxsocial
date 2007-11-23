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
using System.Configuration;
using System.Collections.Generic;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Web;
using System.Web.Security;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public abstract partial class PPage : TPage
    {
        protected string profileUserName;
        protected Member profileOwner;

        public PPage()
            : base()
        {
            page = 1;

            try
            {
                page = int.Parse(Request.QueryString["p"]);
            }
            catch
            {
            }

        }

        public PPage(string templateFile)
            : base(templateFile)
        {
            page = 1;

            try
            {
                page = int.Parse(Request.QueryString["p"]);
            }
            catch
            {
            }
        }

        public Member ProfileOwner
        {
            get
            {
                return profileOwner;
            }
        }

        protected void BeginProfile()
        {
            profileUserName = HttpContext.Current.Request["un"];

            try
            {

                profileOwner = new Member(db, profileUserName);
            }
            catch
            {
                Functions.Generate404(Core);
                return;
            }

            BoxSocial.Internals.Application.LoadApplications(BoxSocial.Internals.Application.GetApplications(Core, profileOwner));
            BoxSocial.Internals.Application.InitialiseApplications(Core, AppPrimitives.Member);

            PageTitle = profileOwner.DisplayName;

            if (loggedInMember != null)
            {
                if (loggedInMember.ShowCustomStyles)
                {
                    template.ParseVariables("USER_STYLE_SHEET", HttpUtility.HtmlEncode(string.Format("{0}.css", profileOwner.UserName)));
                }
            }
            else
            {
                template.ParseVariables("USER_STYLE_SHEET", HttpUtility.HtmlEncode(string.Format("{0}.css", profileOwner.UserName)));
            }
            template.ParseVariables("USER_NAME", HttpUtility.HtmlEncode(profileOwner.UserName));
            template.ParseVariables("USER_DISPLAY_NAME", HttpUtility.HtmlEncode(profileOwner.DisplayName));
            template.ParseVariables("USER_DISPLAY_NAME_OWNERSHIP", HttpUtility.HtmlEncode(profileOwner.UserNameOwnership));

            if (loggedInMember != null)
            {
                if (loggedInMember.UserId == profileOwner.UserId)
                {
                    template.ParseVariables("OWNER", "TRUE");
                    template.ParseVariables("SELF", "TRUE");
                }
                else
                {
                    template.ParseVariables("OWNER", "FALSE");
                    template.ParseVariables("SELF", "FALSE");
                }
            }
            else
            {
                template.ParseVariables("OWNER", "FALSE");
                template.ParseVariables("SELF", "FALSE");
            }
        }

        protected void ShowProfile()
        {
            bool hasProfileInfo = false;

            profileOwner.ProfileAccess.SetViewer(loggedInMember);

            if (!profileOwner.ProfileAccess.CanRead)
            {
                Functions.Generate403(Core);
                return;
            }

            if (session.IsLoggedIn)
            {
                if (profileOwner.ProfileAccess.CanComment)
                {
                    template.ParseVariables("CAN_COMMENT", "TRUE");
                }
            }

            string age;
            int ageInt = profileOwner.Age;
            if (ageInt == 0)
            {
                age = "FALSE";
            }
            else
            {
                age = ageInt.ToString() + " years old";
            }

            template.ParseVariables("USER_SEXUALITY", HttpUtility.HtmlEncode(profileOwner.Sexuality));
            template.ParseVariables("USER_GENDER", HttpUtility.HtmlEncode(profileOwner.Gender));
            template.ParseVariables("USER_AUTOBIOGRAPHY", Bbcode.Parse(HttpUtility.HtmlEncode(profileOwner.Autobiography), loggedInMember));
            template.ParseVariables("USER_MARITIAL_STATUS", HttpUtility.HtmlEncode(profileOwner.MaritialStatus));
            template.ParseVariables("USER_AGE", HttpUtility.HtmlEncode(age));
            template.ParseVariables("USER_JOINED", HttpUtility.HtmlEncode(tz.DateTimeToString(profileOwner.RegistrationDate)));
            template.ParseVariables("USER_LAST_SEEN", HttpUtility.HtmlEncode(tz.DateTimeToString(profileOwner.LastOnlineTime, true)));
            template.ParseVariables("USER_PROFILE_VIEWS", HttpUtility.HtmlEncode(Functions.LargeIntegerToString(profileOwner.ProfileViews)));
            template.ParseVariables("USER_SUBSCRIPTIONS", HttpUtility.HtmlEncode(Functions.LargeIntegerToString(profileOwner.BlogSubscriptions)));
            template.ParseVariables("USER_COUNTRY", HttpUtility.HtmlEncode(profileOwner.Country));
            template.ParseVariables("USER_ICON", HttpUtility.HtmlEncode(profileOwner.UserThumbnail));

            template.ParseVariables("U_PROFILE", HttpUtility.HtmlEncode(ZzUri.BuildProfileUri(profileOwner)));
            template.ParseVariables("U_BLOG", HttpUtility.HtmlEncode((ZzUri.BuildBlogUri(profileOwner))));
            template.ParseVariables("U_GALLERY", HttpUtility.HtmlEncode((ZzUri.BuildGalleryUri(profileOwner))));
            template.ParseVariables("U_FRIENDS", HttpUtility.HtmlEncode((ZzUri.BuildFriendsUri(profileOwner))));

            template.ParseVariables("IS_PROFILE", "TRUE");

            if (profileOwner.MaritialStatusRaw != "UNDEF")
            {
                hasProfileInfo = true;
            }
            if (profileOwner.GenderRaw != "UNDEF")
            {
                hasProfileInfo = true;
            }
            if (profileOwner.SexualityRaw != "UNDEF")
            {
                hasProfileInfo = true;
            }

            if (hasProfileInfo)
            {
                template.ParseVariables("HAS_PROFILE_INFO", "TRUE");
            }

            template.ParseVariables("U_ADD_FRIEND", HttpUtility.HtmlEncode(ZzUri.BuildAddFriendUri(profileOwner.UserId)));
            template.ParseVariables("U_BLOCK_USER", HttpUtility.HtmlEncode(ZzUri.BuildBlockUserUri(profileOwner.UserId)));

            string langFriends = (profileOwner.Friends != 1) ? "friends" : "friend";

            template.ParseVariables("FRIENDS", HttpUtility.HtmlEncode(profileOwner.Friends.ToString()));
            template.ParseVariables("L_FRIENDS", HttpUtility.HtmlEncode(langFriends));

            List<Member> friends = profileOwner.GetFriends(1, 8);
            foreach (Member friend in friends)
            {
                VariableCollection friendVariableCollection = template.CreateChild("friend_list");

                friendVariableCollection.ParseVariables("USER_DISPLAY_NAME", HttpUtility.HtmlEncode(friend.DisplayName));
                friendVariableCollection.ParseVariables("U_PROFILE", HttpUtility.HtmlEncode(ZzUri.BuildProfileUri(friend)));
                friendVariableCollection.ParseVariables("ICON", HttpUtility.HtmlEncode(friend.UserIcon));
            }

            ushort readAccessLevel = profileOwner.GetAccessLevel(loggedInMember);
            long loggedIdUid = Member.GetMemberId(loggedInMember);

            /* Show a list of lists */
            DataTable listTable = db.SelectQuery(string.Format("SELECT ul.list_path, ul.list_title FROM user_keys uk INNER JOIN user_lists ul ON ul.user_id = uk.user_id WHERE uk.user_id = {0} AND (list_access & {2:0} OR ul.user_id = {1})",
                profileOwner.UserId, loggedIdUid, readAccessLevel));

            for (int i = 0; i < listTable.Rows.Count; i++)
            {
                VariableCollection listVariableCollection = template.CreateChild("list_list");

                listVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode((string)listTable.Rows[i]["list_title"]));
                listVariableCollection.ParseVariables("URI", HttpUtility.HtmlEncode("/" + profileOwner.UserName + "/lists/" + ZzUri.AppendSid((string)listTable.Rows[i]["list_path"])));
            }

            template.ParseVariables("LISTS", listTable.Rows.Count.ToString());

            /* pages */
            template.ParseVariables("PAGE_LIST", Display.GeneratePageList(db, profileOwner, loggedInMember, true));

            Core.InvokeHooks(this);

            EndResponse();
        }

    }
}
