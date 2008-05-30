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
    public partial class viewfriends : PPage
    {
        public viewfriends()
            : base("viewfriends.html")
        {
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            BeginProfile();
            int page = Functions.RequestInt("p", 1);

            string langFriends = (profileOwner.Friends != 1) ? "friends" : "friend";

            template.ParseVariables("FRIENDS_TITLE", HttpUtility.HtmlEncode(string.Format("{0} Friends", profileOwner.DisplayNameOwnership)));

            template.ParseVariables("FRIENDS", HttpUtility.HtmlEncode(profileOwner.Friends.ToString()));
            template.ParseVariables("L_FRIENDS", HttpUtility.HtmlEncode(langFriends));

            List<User> friends = profileOwner.GetFriends(page, 18);
            foreach (User friend in friends)
            {
                VariableCollection friendVariableCollection = template.CreateChild("friend_list");

                friendVariableCollection.ParseVariables("USER_DISPLAY_NAME", HttpUtility.HtmlEncode(friend.DisplayName));
                friendVariableCollection.ParseVariables("U_PROFILE", HttpUtility.HtmlEncode(Linker.BuildProfileUri(friend)));
                friendVariableCollection.ParseVariables("ICON", HttpUtility.HtmlEncode(friend.UserIcon));
            }

            string pageUri = Linker.BuildFriendsUri(profileOwner);
            template.ParseVariables("PAGINATION", Display.GeneratePagination(pageUri, page, (int)Math.Ceiling(profileOwner.Friends / 18.0)));

            EndResponse();
        }
    }
}
