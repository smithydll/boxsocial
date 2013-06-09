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
using System.Configuration;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
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
                    showSearchBox();
                    break;
            }


            EndResponse();
        }

        private void showSearchBox()
        {
            if (Request["q"] != null)
            {
                showSearchResults();
            }
        }

        private void showSearchResults()
        {
            template.SetTemplate("search_results.html");

            string query = Request["q"];

            SearchResult results = core.Search.DoSearch(query, TopLevelPageNumber, null, null);

            int resultsRemoved = 0;
            foreach (ISearchableItem result in results.Items)
            {
                bool canView = true;
                if (result is IPermissibleItem)
                {
                    IPermissibleItem item = (IPermissibleItem)result;

                    if (!item.Access.Can("VIEW"))
                    {
                        canView = false;
                        resultsRemoved++;
                    }
                }

                if (result is IPermissibleSubItem)
                {
                    IPermissibleItem item = ((IPermissibleSubItem)result).PermissiveParent;

                    if (!item.Access.Can("VIEW"))
                    {
                        canView = false;
                        resultsRemoved++;
                    }
                }

                if (canView)
                {
                    VariableCollection listingVariableCollection = template.CreateChild("search_listing");

                    listingVariableCollection.Parse("BODY", result.RenderPreview());
                }
            }

            template.Parse("RESULTS", (results.Results - resultsRemoved).ToString());

            core.Display.ParsePagination(core.Hyperlink.BuildSearchUri(query), 10, results.Results - resultsRemoved);
        }

        private void showFriends()
        {
            string needle = Request["name"];
            List<UserRelation> friends;
            if (IsAjax)
            {
                friends = loggedInMember.SearchFriendNames(needle, 1, 10);

                Dictionary<string, string> friendNames = new Dictionary<string, string>();
                for (int i = 0; i < friends.Count; i++)
                {
                    friendNames.Add(friends[i].Id.ToString(), friends[i].DisplayName);
                }

                core.Ajax.SendDictionary("friends", friendNames);
            }
            else
            {
                template.SetTemplate("searchfriendsresult.html");

                friends = loggedInMember.SearchFriendNames(needle, TopLevelPageNumber, 20);

                foreach (UserRelation friend in friends)
                {
                    VariableCollection friendVariableCollection = template.CreateChild("friend_list");

                    friendVariableCollection.Parse("NAME", friend.DisplayName);
                    friendVariableCollection.Parse("URI", friend.Uri);
                    friendVariableCollection.Parse("DISPLAY_PIC", friend.UserIcon);
                }
            }
        }
    }
}
