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
using System.Web.Security;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.FrontEnd
{
    public partial class search : TPage
    {
        public search()
            : base("search.html")
        {
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string mode = Request["mode"];

            switch (mode)
            {
                case "friends":
                    showFriends();
                    break;
                case "friends-online":
                    break;
                default:
                    break;
            }

            EndResponse();
        }

        private void showFriends()
        {
            string needle = Request["name"];
            List<Member> friends;
            if (IsAjax)
            {
                friends = loggedInMember.SearchFriendNames(needle, 1, 10);

                string[] friendNames = new string[friends.Count];
                for (int i = 0; i < friends.Count; i++)
                {
                    friendNames[i] = friends[i].DisplayName;
                }

                Ajax.SendArray("friends", friendNames);
            }
            else
            {
                template.SetTemplate("searchfriendsresult.html");

                friends = loggedInMember.SearchFriendNames(needle, page, 20);

                foreach (Member friend in friends)
                {
                    VariableCollection friendVariableCollection = template.CreateChild("friend_list");

                    friendVariableCollection.ParseVariables("NAME", friend.DisplayName);
                    friendVariableCollection.ParseVariables("URI", friend.Uri);
                    friendVariableCollection.ParseVariables("DISPLAY_PIC", friend.UserIcon);
                }
            }
        }
    }
}
