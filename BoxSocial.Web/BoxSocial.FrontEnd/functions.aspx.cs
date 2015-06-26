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
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using TwoStepsAuthenticator;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Forms;
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

            string oAuthToken;
            string oAuthVerifier;

            switch (function)
            {
                case "date":
                    string date = core.Functions.InterpretDate(core.Http.Form["date"], (DisplayMedium)int.Parse(core.Http.Form["Medium"]));
                    core.Response.SendRawText("date", date);
                    return;
                case "time":
                    string time = core.Functions.InterpretTime(core.Http.Form["time"]);
                    core.Response.SendRawText("time", time);
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
                    core.Functions.ReturnPermissionGroupList(ResponseFormats.Xml);
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
                case "boxsocial":
                    NameValueCollection response = new NameValueCollection();

                    core.Http.WriteAndEndResponse(response);
                    break;
                case "oauth":
                    string method = core.Http.Query["method"];
                    switch (method)
                    {
                        case "authorize":
                            OAuthAuthorize(false);
                            return;
                        case "approve":
                            OAuthApprove();
                            return;
                        case "authenticate":
                            return;
                    }
                    break;
                case "sms":
                    break;
            }
        }

        private void OAuthApprove()
        {
            string oauthToken = core.Http.Form["oauth_token"];
            bool success = false;

            try
            {
                OAuthToken token = new OAuthToken(core, oauthToken);
                ApplicationEntry ae = token.Application;
                OAuthApplication oae = new OAuthApplication(core, ae);

                if (core.Http.Form["mode"] == "verify")
                {
                    Authenticator authenticator = new Authenticator();

                    if (authenticator.CheckCode(core.Session.CandidateMember.UserInfo.TwoFactorAuthKey, core.Http.Form["verify"]))
                    {
                        success = true;
                    }
                    else
                    {
                        showVerificationForm(ae, oauthToken, core.Session.SessionId);

                        return;
                    }
                }
                else
                {

                    bool authenticated = false;

                    string userName = Request.Form["username"];
                    string password = BoxSocial.Internals.User.HashPassword(Request.Form["password"]);

                    DataTable userTable = db.Query(string.Format("SELECT uk.user_name, uk.user_id, ui.user_password, ui.user_two_factor_auth_key, ui.user_two_factor_auth_verified FROM user_keys uk INNER JOIN user_info ui ON uk.user_id = ui.user_id WHERE uk.user_name = '{0}';",
                       userName));

                    if (userTable.Rows.Count == 1)
                    {
                        DataRow userRow = userTable.Rows[0];
                        string dbPassword = (string)userRow["user_password"];

                        if (dbPassword == password)
                        {
                            authenticated = true;
                        }

                        if (authenticated)
                        {
                            if ((byte)userRow["user_two_factor_auth_verified"] > 0)
                            {
                                string sessionId = session.SessionBegin((long)userRow["user_id"], false, false, false);

                                showVerificationForm(ae, oauthToken, sessionId);

                                return;
                            }
                            else
                            {
                                string sessionId = session.SessionBegin((long)userRow["user_id"], false, false);

                                success = true;
                            }
                        }
                        else
                        {
                            OAuthAuthorize(true);
                            return;
                        }
                    }
                }

                if (success)
                {
                    OAuthVerifier verifier = OAuthVerifier.Create(core, token, core.Session.CandidateMember);
                    token.UseToken();

                    db.CommitTransaction();

                    if (!string.IsNullOrEmpty(oae.CallbackUrl))
                    {
                        Response.Redirect(string.Format("{0}?oauth_token={1}&oauth_verifier={2}", oae.CallbackUrl, Uri.EscapeDataString(token.Token), Uri.EscapeDataString(verifier.Verifier)));
                    }
                    else
                    {
                        core.Response.SendRawText("", string.Format("oauth_token={0}&oauth_verifier={1}", Uri.EscapeDataString(token.Token), Uri.EscapeDataString(verifier.Verifier)));
                    }
                }
                else
                {
                    // Incorrect password
                    OAuthAuthorize(true);
                    return;
                }
            }
            catch (InvalidOAuthTokenException)
            {
                core.Functions.Generate403();
            }


            EndResponse();
        }

        private void showVerificationForm(ApplicationEntry ae, string oauthToken, string sessionId)
        {

            TextBox verifyTextBox = new TextBox("verify");

            HiddenField oauthTokenHiddenField = new HiddenField("oauth_token");
            oauthTokenHiddenField.Value = oauthToken;

            HiddenField modeHiddenField = new HiddenField("mode");
            modeHiddenField.Value = "verify";

            SubmitButton submitButton = new SubmitButton("submit", core.Prose.GetString("AUTHORISE"));
            Button cancelButton = new Button("cancel", core.Prose.GetString("CANCEL"), "cancel");

            template.SetTemplate("oauth_authorize.html");
            template.Parse("U_POST", core.Hyperlink.AppendSid("/oauth/approve", true, sessionId));
            template.Parse("VERIFY", "TRUE");
            template.Parse("AUTHORISE_APPLICATION", string.Format(core.Prose.GetString("AUTHORISE_APPLICATION"), ae.Title));
            template.Parse("APPLICATION_ICON", ae.Icon);
            template.Parse("S_VERIFY", verifyTextBox);
            template.Parse("S_OAUTH_TOKEN", oauthTokenHiddenField);
            template.Parse("S_MODE", modeHiddenField);
            template.Parse("S_SUBMIT", submitButton);
            template.Parse("S_CANCEL", cancelButton);

            EndResponse();
        }

        private void OAuthAuthorize(bool fail)
        {
            bool forceLogin = (core.Http.Query["force_login"] == "true");
            string oauthToken = core.Http["oauth_token"];

            try
            {
                OAuthToken token = new OAuthToken(core, oauthToken);
                ApplicationEntry ae = token.Application;

                TextBox usernameTextBox = new TextBox("username");
                TextBox passwordTextBox = new TextBox("password", InputType.Password);
                
                HiddenField oauthTokenHiddenField = new HiddenField("oauth_token");
                oauthTokenHiddenField.Value = oauthToken;

                SubmitButton submitButton = new SubmitButton("submit", core.Prose.GetString("AUTHORISE"));
                Button cancelButton = new Button("cancel", core.Prose.GetString("CANCEL"), "cancel");

                if (token.TokenExpired)
                {
                    core.Functions.Generate403();
                    EndResponse();
                    return;
                }

                template.SetTemplate("oauth_authorize.html");

                template.Parse("U_POST", core.Hyperlink.AppendSid("/oauth/approve", true));
                template.Parse("REQUIRE_LOGIN", ((forceLogin || (!core.Session.SignedIn)) ? "TRUE" : "FALSE"));
                template.Parse("AUTHORISE_APPLICATION", string.Format(core.Prose.GetString("AUTHORISE_APPLICATION"), ae.Title));
                template.Parse("APPLICATION_ICON", ae.Icon);
                template.Parse("S_USERNAME", usernameTextBox);
                template.Parse("S_PASSWORD", passwordTextBox);
                template.Parse("S_OAUTH_TOKEN", oauthTokenHiddenField);
                template.Parse("S_SUBMIT", submitButton);
                template.Parse("S_CANCEL", cancelButton);
            }
            catch (InvalidOAuthTokenException)
            {
                core.Functions.Generate403();
            }
            catch (InvalidApplicationException)
            {
                core.Functions.Generate403();
            }

            EndResponse();
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

            core.Response.SendDictionary("tagSelect", tagsText);

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

                core.Response.SendUserDictionary("friendSelect", friendNames);
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

                core.Response.SendDictionary("contactCard", userInfo);
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
            if ((!string.IsNullOrEmpty(url)) && url.StartsWith(shareUrlStub, StringComparison.Ordinal))
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

            if (info.InfoKey.GetType(core).Embeddable)
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
                            JsonTextWriter jtw;

                            js = new JsonSerializer();
                            jstw = new StringWriter();
                            jtw = new JsonTextWriter(jstw);

                            js.NullValueHandling = NullValueHandling.Ignore;

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
                core.Response.SendDictionary("noNewContent", returnValues);
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

                    core.Response.SendDictionary("newContent", returnValues);
                }
                else
                {
                    core.Response.SendDictionary("noNewContent", returnValues);
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

                    core.Display.ParseBbcode(feedItemVariableCollection, "TITLE", feedAction.FormattedTitle);
                    core.Display.ParseBbcode(feedItemVariableCollection, "TEXT", feedAction.Body, core.PrimitiveCache[feedAction.OwnerId], true, string.Empty, string.Empty);

                    feedItemVariableCollection.Parse("USER_DISPLAY_NAME", feedAction.Owner.DisplayName);

                    feedItemVariableCollection.Parse("ID", feedAction.ActionItemKey.Id);
                    feedItemVariableCollection.Parse("TYPE_ID", feedAction.ActionItemKey.TypeId);

                    if (feedAction.ActionItemKey.GetType(core).Likeable)
                    {
                        feedItemVariableCollection.Parse("LIKEABLE", "TRUE");

                        if (feedAction.Info.Likes > 0)
                        {
                            feedItemVariableCollection.Parse("LIKES", string.Format(" {0:d}", feedAction.Info.Likes));
                            feedItemVariableCollection.Parse("DISLIKES", string.Format(" {0:d}", feedAction.Info.Dislikes));
                        }
                    }

                    if (feedAction.ActionItemKey.GetType(core).Commentable)
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
                        if (feedAction.ActionItemKey.GetType(core).Shareable)
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

                core.Response.SendDictionary("newFeedItems", returnValues);
            }
            else
            {
                Dictionary<string, string> returnValues = new Dictionary<string, string>();

                returnValues.Add("notifications", session.LoggedInMember.UserInfo.UnreadNotifications.ToString());
                returnValues.Add("mail", session.LoggedInMember.UserInfo.UnseenMail.ToString());

                core.Response.SendDictionary("unreadItems", returnValues);
            }
        }
    }
}
