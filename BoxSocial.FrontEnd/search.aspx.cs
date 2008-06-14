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
            string query = Request["q"];
            string email = null;
            string firstName = null;
            string middleName = null;
            string lastName = null;
            string userName = null;

            Match match = Regex.Match(query, @"[a-z0-9&\'\.\-_\+]+@[a-z0-9\-]+\.([a-z0-9\-]+\.)*?[a-z]+", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                email = match.Value;
            }
            else
            {
                string[] parts = query.Split(new char[] { ' ', '\t', ',', '.', ';', '+', '_', '$', '=', '&' });

                switch (parts.Length)
                {
                    case 1:
                        firstName = lastName = parts[0];
                        break;
                    case 2:
                        if (query.Contains(","))
                        {
                            firstName = parts[1];
                            lastName = parts[0];
                        }
                        else
                        {
                            firstName = parts[0];
                            lastName = parts[1];
                        }
                        break;
                    case 3:
                        if (query.Contains(","))
                        {
                            firstName = parts[1];
                            middleName = parts[2];
                            lastName = parts[0];
                        }
                        else
                        {
                            firstName = parts[0];
                            middleName = parts[1];
                            lastName = parts[2];
                        }
                        break;
                }

                parts = query.Split(new char[] { ' ', '\t', ';' });

                if (parts.Length == 1)
                {
                    userName = parts[0];
                }
            }

            SelectQuery squery = new SelectQuery("user_keys");
            squery.AddFields(Item.GetFieldsPrefixed(typeof(BoxSocial.Internals.User)));
            squery.AddFields(UserInfo.GetFieldsPrefixed(typeof(UserInfo)));
            squery.AddFields(UserProfile.GetFieldsPrefixed(typeof(UserProfile)));
            squery.AddField(new DataField("gallery_items", "gallery_item_uri"));
            squery.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
            squery.AddJoin(JoinTypes.Inner, "user_info", "user_id", "user_id");
            squery.AddJoin(JoinTypes.Inner, "user_profile", "user_id", "user_id");
            squery.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_country"), new DataField("countries", "country_iso"));
            squery.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_religion"), new DataField("religions", "religion_id"));
            squery.AddJoin(JoinTypes.Left, new DataField("user_info", "user_icon"), new DataField("gallery_items", "gallery_item_id"));

            if (!string.IsNullOrEmpty(email))
            {
                squery.AddCondition("user_alternate_email", email.ToLower());
            }

            if (!string.IsNullOrEmpty(firstName))
            {
                squery.AddCondition(ConditionRelations.Or, "profile_name_first", firstName.ToLower());
            }

            if (!string.IsNullOrEmpty(middleName))
            {
                squery.AddCondition(ConditionRelations.Or, "profile_name_middle", firstName.ToLower());
            }

            if (!string.IsNullOrEmpty(lastName))
            {
                squery.AddCondition(ConditionRelations.Or, "profile_name_suffix", firstName.ToLower());
            }

            if (!string.IsNullOrEmpty(userName))
            {
                squery.AddCondition(ConditionRelations.Or, "user_keys.user_name", userName.ToLower());
                squery.AddCondition(ConditionRelations.Or, "user_name_display", userName.ToLower());
            }

            squery.LimitCount = 10;
            squery.LimitStart = 10 * (page - 1);

            DataTable searchDataTable = db.Query(squery);

            template.SetTemplate("search_results.html");

            template.Parse("RESULTS", searchDataTable.Rows.Count.ToString());

            foreach (DataRow dr in searchDataTable.Rows)
            {
                BoxSocial.Internals.User user = new User(core, dr, UserLoadOptions.All);

                VariableCollection userVariableCollection = template.CreateChild("search_listing");

                userVariableCollection.Parse("USER_DISPLAY_NAME", user.DisplayName);
                userVariableCollection.Parse("ICON", user.UserIcon);
                userVariableCollection.Parse("U_PROFILE", user.Uri);
                userVariableCollection.Parse("JOIN_DATE", tz.DateTimeToString(user.Info.GetRegistrationDate(tz)));
                userVariableCollection.Parse("USER_AGE", user.AgeString);
                userVariableCollection.Parse("USER_COUNTRY", user.Country);
            }
            
        }

        private void showFriends()
        {
            string needle = Request["name"];
            List<User> friends;
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

                foreach (User friend in friends)
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
