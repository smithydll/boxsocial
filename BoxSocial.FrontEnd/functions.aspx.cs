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
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;

namespace BoxSocial.FrontEnd
{
    public partial class functions : TPage
    {
        public functions()
            : base("")
        {
            this.Load += new EventHandler(Page_Load);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string function = core.Http["fun"];

            switch (function)
            {
                case "date":
                    string date = core.Functions.InterpretDate(core.Http.Form["date"]);
                    core.Ajax.SendRawText("date", date);
                    return;
                case "time":
                    string time = core.Functions.InterpretTime(core.Http.Form["time"]);
                    core.Ajax.SendRawText("time", time);
                    return;
                case "friend-list":
                    ReturnFriendList();
                    return;
                case "contact-card":
                    ReturnContactCard();
                    return;
                case "permission-groups-list":
                    ReturnPermissionGroupList();
                    return;
                case "embed":
                    return;
                case "twitter":
                    Twitter t = new Twitter(core.Settings.TwitterApiKey, core.Settings.TwitterApiSecret);
                    
                    string oAuthToken = core.Http.Query["oauth_token"];
                    string oAuthVerifier = core.Http.Query["oauth_verifier"];

                    t.SaveTwitterAccess(core, oAuthToken, oAuthVerifier);

                    return;
                case "googleplus":
                    /*Google g = new Google(core.Settings.GoogleApiKey, core.Settings.GoogleApiSecret);

                    string oAuthCode = core.Http.Query["code"];

                    g.SaveGoogleAccess(core, oAuthToken, oAuthCode);*/

                    return;
            }
        }

        private void ReturnFriendList()
        {
            string namePart = core.Http.Form["name-field"];

            if (core.Session.SignedIn)
            {
                List<Friend> friends = core.Session.LoggedInMember.GetFriends(namePart);

                Dictionary<long, string[]> friendNames = new Dictionary<long, string[]>();

                foreach (Friend friend in friends)
                {
                    friendNames.Add(friend.Id, new string[] { friend.DisplayName, friend.UserTile });
                }

                core.Ajax.SendUserDictionary("friendSelect", friendNames);
            }
        }

        private void ReturnContactCard()
        {
            long uid = core.Functions.RequestLong("uid", core.Functions.FormLong("uid", 0));

            Dictionary<string, string> userInfo = new Dictionary<string, string>();

            try
            {
                User user = new Internals.User(core, uid);

                bool subscribed = Subscription.IsSubscribed(core, user.ItemKey);

                userInfo.Add("cover-photo", user.MobileCoverPhoto);
                userInfo.Add("display-name", user.DisplayName);
                userInfo.Add("display-picture", user.UserIcon);
                userInfo.Add("uri", user.Uri);
                userInfo.Add("profile", user.ProfileUri);
                userInfo.Add("abstract", core.Bbcode.Parse(user.Profile.Autobiography));
                userInfo.Add("subscribed", subscribed.ToString().ToLower());
                userInfo.Add("subscribers", core.Functions.LargeIntegerToString(user.Info.Subscribers));
                userInfo.Add("subscribe-uri", (subscribed) ? core.Hyperlink.BuildUnsubscribeUri(user.ItemKey) : core.Hyperlink.BuildSubscribeUri(user.ItemKey));
                userInfo.Add("location", user.Profile.Country);
                userInfo.Add("l-location", "Location");
                userInfo.Add("l-subscribe", (subscribed) ? "Unsubscribe" : "Subscribe");
                userInfo.Add("id", user.ItemKey.Id.ToString());
                userInfo.Add("type", user.ItemKey.TypeId.ToString());

                core.Ajax.SendDictionary("contactCard", userInfo);
            }
            catch (InvalidUserException)
            {
            }
        }

        private void ReturnPermissionGroupList()
        {
            string namePart = core.Http.Form["name-field"];
            long itemId = core.Functions.FormLong("item", 0);
            long itemTypeId = core.Functions.FormLong("type", 0);

            if (!(itemId > 0 && itemTypeId > 0))
            {
                if (core.Session.SignedIn)
                {
                    itemId = core.Session.LoggedInMember.Id;
                    itemTypeId = core.Session.LoggedInMember.TypeId;
                }
            }

            if (itemId > 0 && itemTypeId > 0)
            {
                ItemKey ik = new ItemKey(itemId, itemTypeId);

                List<PrimitivePermissionGroup> groups = null;
                NumberedItem ni = NumberedItem.Reflect(core, ik);
                Primitive primitive = null;
                Dictionary<ItemKey, string[]> permissiveNames = new Dictionary<ItemKey, string[]>();

                if (ni.GetType().IsSubclassOf(typeof(Primitive)))
                {
                    primitive = (Primitive)ni;
                }
                else
                {
                    primitive = ((IPermissibleItem)ni).Owner;
                }

                groups = new List<PrimitivePermissionGroup>();
                int itemGroups = 0;

                Type type = ni.GetType();
                if (type.GetMethod(type.Name + "_GetItemGroups", new Type[] { typeof(Core) }) != null)
                {
                    groups.AddRange((List<PrimitivePermissionGroup>)type.InvokeMember(type.Name + "_GetItemGroups", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, new object[] { core }));
                    itemGroups = groups.Count;
                }

                groups.AddRange(core.GetPrimitivePermissionGroups(primitive));

                foreach (PrimitivePermissionGroup group in groups)
                {
                    if (!string.IsNullOrEmpty(group.LanguageKey))
                    {
                        permissiveNames.Add(group.ItemKey, new string[] { core.Prose.GetString(group.LanguageKey), group.Tile });
                    }
                    else
                    {
                        permissiveNames.Add(group.ItemKey, new string[] { group.DisplayName, group.Tile });
                    }
                }

                List<User> friends = primitive.GetPermissionUsers(namePart);

                foreach (User friend in friends)
                {
                    permissiveNames.Add(friend.ItemKey, new string[] { friend.DisplayName, friend.UserTile });
                }

                core.Ajax.SendPermissionGroupDictionary("permissionSelect", permissiveNames);

                /*if (core.Session.IsLoggedIn)
                {
                    List<Friend> friends = core.Session.LoggedInMember.GetFriends(namePart);

                    Dictionary<long, string[]> friendNames = new Dictionary<long, string[]>();

                    foreach (Friend friend in friends)
                    {
                        friendNames.Add(friend.Id, new string[] { friend.DisplayName, friend.UserTile });
                    }

                    core.Ajax.SendUserDictionary("friendSelect", friendNames);
                }*/
            }
        }
    }
}
