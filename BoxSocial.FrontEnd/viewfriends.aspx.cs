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
using System.Configuration;
using System.Data;
using System.Web;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.FrontEnd
{
    public partial class viewfriends : UPage
    {
        public viewfriends()
            : base("viewfriends.html")
        {
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            BeginProfile();
            int page = core.Functions.RequestInt("p", 1);

            string langFriends = (User.UserInfo.Friends != 1) ? "friends" : "friend";

            template.Parse("FRIENDS_TITLE", string.Format("{0} Friends", User.DisplayNameOwnership));

            template.Parse("FRIENDS", User.UserInfo.Friends.ToString());
            template.Parse("L_FRIENDS", langFriends);

            List<Friend> friends = User.GetFriends(page, 18);
            foreach (UserRelation friend in friends)
            {
                VariableCollection friendVariableCollection = template.CreateChild("friend_list");

                friendVariableCollection.Parse("USER_DISPLAY_NAME", friend.DisplayName);
                friendVariableCollection.Parse("U_PROFILE", friend.Uri);
                friendVariableCollection.Parse("ICON", friend.UserIcon);
            }

            string pageUri = core.Uri.BuildFriendsUri(User);
            core.Display.ParsePagination(pageUri, page, (int)Math.Ceiling(User.UserInfo.Friends / 18.0));

            /* pages */
            core.Display.ParsePageList(User, true);

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "friends", "Friends" });

            User.ParseBreadCrumbs(breadCrumbParts);

            EndResponse();
        }
    }
}
