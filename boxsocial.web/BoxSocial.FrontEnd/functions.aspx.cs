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
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Forms;
using BoxSocial.Groups;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

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

            string oAuthToken;
            string oAuthVerifier;

            switch (function)
            {
                case "date":
                    string date = core.Functions.InterpretDate(core.Http.Form["date"], (DisplayMedium)int.Parse(core.Http.Form["Medium"]));
                    core.Ajax.SendRawText("date", date);
                    return;
                case "time":
                    string time = core.Functions.InterpretTime(core.Http.Form["time"]);
                    core.Ajax.SendRawText("time", time);
                    return;
                case "friend-list":
                    ReturnFriendList();
                    return;
                case "tag-list":
                    ReturnTagList();
                    return;
                case "search-list":
                    return;
                case "contact-card":
                    ReturnContactCard();
                    return;
                case "feed":
                    CheckNewFeedItems();
                    return;
                case "permission-groups-list":
                    ReturnPermissionGroupList();
                    return;
                case "embed":
                    ReturnItemEmbedCode();
                    return;
                case "twitter":
                    Twitter t = new Twitter(core.Settings.TwitterApiKey, core.Settings.TwitterApiSecret);
                    
                    oAuthToken = core.Http.Query["oauth_token"];
                    oAuthVerifier = core.Http.Query["oauth_verifier"];

                    t.SaveTwitterAccess(core, oAuthToken, oAuthVerifier);

                    return;
                case "tumblr":
                    Tumblr tr = new Tumblr(core.Settings.TumblrApiKey, core.Settings.TumblrApiSecret);
                    
                    oAuthToken = core.Http.Query["oauth_token"];
                    oAuthVerifier = core.Http.Query["oauth_verifier"]; // + "#_=_";

                    tr.SaveTumblrAccess(core, oAuthToken, oAuthVerifier);
                    return;
                case "googleplus":
                    /*Google g = new Google(core.Settings.GoogleApiKey, core.Settings.GoogleApiSecret);

                    string oAuthCode = core.Http.Query["code"];

                    g.SaveGoogleAccess(core, oAuthToken, oAuthCode);*/

                    return;
                case "facebook":
                    Facebook fb = new Facebook(core.Settings.FacebookApiAppid, core.Settings.FacebookApiSecret);

                    string errorReason = core.Http.Query["error_reason"];
                    string code = core.Http.Query["code"];

                    if (!(errorReason == "user_denied"))
                    {
                        fb.SaveFacebookAccess(core, code);
                    }
                    else
                    {
                        core.Http.Redirect(core.Hyperlink.BuildAccountSubModuleUri("dashboard", "preferences") + "?status=facebook-auth-failed");
                    }

                    return;
            }
        }

        private void ReturnTagList()
        {
            string tagText = core.Http.Form["tag-text"];


            List<Tag> tags = Tag.SearchTags(core, tagText);

            Dictionary<string, string> tagsText = new Dictionary<string, string>();

            foreach (Tag tag in tags)
            {
                tagsText.Add(tag.Id.ToString(), tag.TagText);
            }

            core.Ajax.SendDictionary("tagSelect", tagsText);

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
                    friendNames.Add(friend.Id, new string[] { friend.DisplayName, friend.Tile });
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
                userInfo.Add("display-picture", user.Icon);
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

        private void ReturnItemEmbedCode()
        {
            string url = core.Hyperlink.StripScheme(core.Http.Query["url"]);
            string format = core.Http.Query["format"];
            string key = string.Empty;
            int maxWidth = core.Functions.RequestInt("maxwidth", 640);
            int maxHeight = core.Functions.RequestInt("maxheight", 640);

            core.Session.SetBot();

            string shareUrlStub = core.Hyperlink.StripScheme(core.Hyperlink.StripSid(core.Hyperlink.AppendAbsoluteSid("/s/")));
            if ((!string.IsNullOrEmpty(url)) && url.StartsWith(shareUrlStub))
            {
                key = url.Substring(shareUrlStub.Length).Trim(new char[] { '/' });
            }
            else
            {
                core.Functions.Generate404();
            }

            ItemInfo info = null;
            IEmbeddableItem item = null;

            try
            {
                info = new ItemInfo(core, key);
            }
            catch (InvalidIteminfoException)
            {
                core.Functions.Generate404();
            }

            if (info.InfoKey.ImplementsEmbeddable)
            {
                core.ItemCache.RequestItem(info.InfoKey);
                try
                {
                    item = (IEmbeddableItem)core.ItemCache[info.InfoKey];
                }
                catch
                {
                    try
                    {
                        item = (IEmbeddableItem)NumberedItem.Reflect(core, info.InfoKey);
                    }
                    catch
                    {
                        core.Functions.Generate404();
                    }
                }

                Embed embed = item.GetEmbedObject(maxWidth, maxHeight);

                if (embed != null)
                {
                    embed.ProviderName = core.Settings.SiteTitle;
                    embed.ProviderUrl = core.Hyperlink.StripSid(core.Hyperlink.Uri);
                    embed.CacheAge = (365 * 24 * 60 * 60).ToString(); // recommend to cache for a year

                    switch (format)
                    {
                        case "xml":
                            XmlSerializer xs;
                            StringWriter stw;

                            xs = new XmlSerializer(typeof(Embed));
                            stw = new StringWriter();

                            core.Http.WriteXml(xs, embed);

                            if (core.Db != null)
                            {
                                core.Db.CloseConnection();
                            }

                            core.Http.End();
                            break;
                        case "json":
                        default:
                            JsonSerializer js;
                            StringWriter jstw;
                            JsonWriter jtw;

                            js = new JsonSerializer();
                            jstw = new StringWriter();
                            jtw = new JsonTextWriter(jstw);

                            core.Http.WriteJson(js, embed);

                            if (core.Db != null)
                            {
                                core.Db.CloseConnection();
                            }

                            core.Http.End();
                            break;
                    }
                }
                else
                {
                    core.Functions.Generate404();
                }
            }
            else
            {
                core.Functions.Generate404();
            }
        }

        private void CheckNewFeedItems()
        {
            string mode = core.Http["mode"];
            long newestId = core.Functions.RequestLong("newest-id", 0);
            long newerId = 0;

            if (!core.Session.IsLoggedIn)
            {
                Dictionary<string, string> returnValues = new Dictionary<string, string>();
                core.Ajax.SendDictionary("noNewContent", returnValues);
            }

            if (mode == "query")
            {
                int count = Feed.GetNewerItemCount(core, core.Session.LoggedInMember, newestId); ;

                Dictionary<string, string> returnValues = new Dictionary<string, string>();

                returnValues.Add("notifications", session.LoggedInMember.UserInfo.UnreadNotifications.ToString());
                returnValues.Add("mail", session.LoggedInMember.UserInfo.UnseenMail.ToString());
                
                if (count > 0)
                {
                    returnValues.Add("feed-count", count.ToString());

                    core.Ajax.SendDictionary("newContent", returnValues);
                }
                else
                {
                    core.Ajax.SendDictionary("noNewContent", returnValues);
                }
            }
            else if (mode == "fetch")
            {
                List<BoxSocial.Internals.Action> feedActions = Feed.GetNewerItems(core, core.Session.LoggedInMember, newestId);

                Template template = new Template("pane.feeditem.html");
                template.Medium = core.Template.Medium;
                template.SetProse(core.Prose);

                foreach (BoxSocial.Internals.Action feedAction in feedActions)
                {
                    VariableCollection feedItemVariableCollection = template.CreateChild("feed_days_list.feed_item");

                    if (feedAction.Id > newerId)
                    {
                        newerId = feedAction.Id;
                    }

                    core.Display.ParseBbcode(feedItemVariableCollection, "TITLE", feedAction.Title);
                    core.Display.ParseBbcode(feedItemVariableCollection, "TEXT", feedAction.Body, core.PrimitiveCache[feedAction.OwnerId], true, string.Empty, string.Empty);

                    feedItemVariableCollection.Parse("USER_DISPLAY_NAME", feedAction.Owner.DisplayName);

                    feedItemVariableCollection.Parse("ID", feedAction.ActionItemKey.Id);
                    feedItemVariableCollection.Parse("TYPE_ID", feedAction.ActionItemKey.TypeId);

                    if (feedAction.ActionItemKey.ImplementsLikeable)
                    {
                        feedItemVariableCollection.Parse("LIKEABLE", "TRUE");

                        if (feedAction.Info.Likes > 0)
                        {
                            feedItemVariableCollection.Parse("LIKES", string.Format(" {0:d}", feedAction.Info.Likes));
                            feedItemVariableCollection.Parse("DISLIKES", string.Format(" {0:d}", feedAction.Info.Dislikes));
                        }
                    }

                    if (feedAction.ActionItemKey.ImplementsCommentable)
                    {
                        feedItemVariableCollection.Parse("COMMENTABLE", "TRUE");

                        if (feedAction.Info.Comments > 0)
                        {
                            feedItemVariableCollection.Parse("COMMENTS", string.Format(" ({0:d})", feedAction.Info.Comments));
                        }
                    }

                    //Access access = new Access(core, feedAction.ActionItemKey, true);
                    if (feedAction.PermissiveParent.Access.IsPublic())
                    {
                        feedItemVariableCollection.Parse("IS_PUBLIC", "TRUE");
                        if (feedAction.ActionItemKey.ImplementsShareable)
                        {
                            feedItemVariableCollection.Parse("SHAREABLE", "TRUE");
                            //feedItemVariableCollection.Parse("U_SHARE", feedAction.ShareUri);

                            if (feedAction.Info.SharedTimes > 0)
                            {
                                feedItemVariableCollection.Parse("SHARES", string.Format(" {0:d}", feedAction.Info.SharedTimes));
                            }
                        }
                    }

                    if (feedAction.Owner is User)
                    {
                        feedItemVariableCollection.Parse("USER_TILE", ((User)feedAction.Owner).Tile);
                        feedItemVariableCollection.Parse("USER_ICON", ((User)feedAction.Owner).Icon);
                    }
                }


                // Check for new messages and upload
                Dictionary<string, string> returnValues = new Dictionary<string, string>();

                returnValues.Add("update", "true");
                returnValues.Add("template", template.ToString());
                returnValues.Add("newest-id", newerId.ToString());

                returnValues.Add("notifications", session.LoggedInMember.UserInfo.UnreadNotifications.ToString());
                returnValues.Add("mail", session.LoggedInMember.UserInfo.UnseenMail.ToString());

                core.Ajax.SendDictionary("newFeedItems", returnValues);
            }
            else
            {
                Dictionary<string, string> returnValues = new Dictionary<string, string>();

                returnValues.Add("notifications", session.LoggedInMember.UserInfo.UnreadNotifications.ToString());
                returnValues.Add("mail", session.LoggedInMember.UserInfo.UnseenMail.ToString());

                core.Ajax.SendDictionary("unreadItems", returnValues);
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
                    permissiveNames.Add(friend.ItemKey, new string[] { friend.DisplayName, friend.Tile });
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
