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
using System.Data;
using System.Net;
using System.Text;
using System.Web;
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [AccountSubModule("dashboard", "preferences")]
    public class AccountPreferences : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Preferences";
            }
        }

        public override int Order
        {
            get
            {
                return 2;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountPreferences class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountPreferences(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountPreferences_Load);
            this.Show += new EventHandler(AccountPreferences_Show);
        }

        void AccountPreferences_Load(object sender, EventArgs e)
        {
            this.AddModeHandler("unlink-twitter", new ModuleModeHandler(AccountPreferences_UnlinkTwitter));
            this.AddModeHandler("link-tumblr", new ModuleModeHandler(AccountPreferences_LinkTumblr));
            this.AddModeHandler("unlink-tumblr", new ModuleModeHandler(AccountPreferences_UnlinkTumblr));
            this.AddModeHandler("unlink-facebook", new ModuleModeHandler(AccountPreferences_UnlinkFacebook));
        }

        void AccountPreferences_Show(object sender, EventArgs e)
        {
			Save(new EventHandler(AccountPreferences_Save));
			
            //User loggedInMember = (User)loggedInMember;
            template.SetTemplate("account_preferences.html");

            TextBox customDomainTextBox = new TextBox("custom-domain");
            customDomainTextBox.Value = LoggedInMember.UserDomain;

            TextBox analyticsCodeTextBox = new TextBox("analytics-code");
            analyticsCodeTextBox.Value = LoggedInMember.UserInfo.AnalyticsCode;

            TextBox twitterUserNameTextBox = new TextBox("twitter-user-name");
            twitterUserNameTextBox.Value = LoggedInMember.UserInfo.TwitterUserName;

            CheckBox twitterSyndicateCheckBox = new CheckBox("twitter-syndicate");
            twitterSyndicateCheckBox.IsChecked = LoggedInMember.UserInfo.TwitterSyndicate;
            twitterSyndicateCheckBox.Width.Length = 0;

            CheckBox tumblrSyndicateCheckBox = new CheckBox("tumblr-syndicate");
            tumblrSyndicateCheckBox.IsChecked = LoggedInMember.UserInfo.TumblrSyndicate;
            tumblrSyndicateCheckBox.Width.Length = 0;

            CheckBox facebookSyndicateCheckBox = new CheckBox("facebook-syndicate");
            facebookSyndicateCheckBox.IsChecked = LoggedInMember.UserInfo.FacebookSyndicate;
            facebookSyndicateCheckBox.Width.Length = 0;

            SelectBox facebookSharePermissionSelectBox = new SelectBox("facebook-share-permissions");
            facebookSharePermissionSelectBox.Add(new SelectBoxItem("", core.Prose.GetString("TIMELINE_DEFAULT")));
            facebookSharePermissionSelectBox.Add(new SelectBoxItem("EVERYONE", core.Prose.GetString("PUBLIC")));
            facebookSharePermissionSelectBox.Add(new SelectBoxItem("FRIENDS_OF_FRIENDS", core.Prose.GetString("FRIENDS_OF_FACEBOOK_FRIENDS")));
            facebookSharePermissionSelectBox.Add(new SelectBoxItem("ALL_FRIENDS", core.Prose.GetString("FACEBOOK_FRIENDS")));

            SelectBox tumblrBlogsSelectBox = new SelectBox("tumblr-blogs");
            if (LoggedInMember.UserInfo.TumblrAuthenticated)
            {
                Tumblr t = new Tumblr(core.Settings.TumblrApiKey, core.Settings.TumblrApiSecret);
                List<Dictionary<string, string>> blogs = t.GetUserInfo(new TumblrAccessToken(LoggedInMember.UserInfo.TumblrToken, LoggedInMember.UserInfo.TumblrTokenSecret)).Blogs;

                foreach (Dictionary<string, string> blog in blogs)
                {
                    string hostname = (new Uri(blog["url"])).Host;
                    tumblrBlogsSelectBox.Add(new SelectBoxItem(hostname, blog["title"]));

                    if (hostname == LoggedInMember.UserInfo.TumblrHostname)
                    {
                        tumblrBlogsSelectBox.SelectedKey = LoggedInMember.UserInfo.TumblrHostname;
                    }
                }
            }

            if (LoggedInMember.UserInfo.FacebookSharePermissions != null)
            {
                facebookSharePermissionSelectBox.SelectedKey = LoggedInMember.UserInfo.FacebookSharePermissions;
            }

            string radioChecked = " checked=\"checked\"";

            if (LoggedInMember.UserInfo.EmailNotifications)
            {
                template.Parse("S_EMAIL_NOTIFICATIONS_YES", radioChecked);
            }
            else
            {
                template.Parse("S_EMAIL_NOTIFICATIONS_NO", radioChecked);
            }

            if (LoggedInMember.UserInfo.ShowCustomStyles)
            {
                template.Parse("S_SHOW_STYLES_YES", radioChecked);
            }
            else
            {
                template.Parse("S_SHOW_STYLES_NO", radioChecked);
            }

            if (LoggedInMember.UserInfo.BbcodeShowImages)
            {
                template.Parse("S_DISPLAY_IMAGES_YES", radioChecked);
            }
            else
            {
                template.Parse("S_DISPLAY_IMAGES_NO", radioChecked);
            }

            if (LoggedInMember.UserInfo.BbcodeShowFlash)
            {
                template.Parse("S_DISPLAY_FLASH_YES", radioChecked);
            }
            else
            {
                template.Parse("S_DISPLAY_FLASH_NO", radioChecked);
            }

            if (LoggedInMember.UserInfo.BbcodeShowVideos)
            {
                template.Parse("S_DISPLAY_VIDEOS_YES", radioChecked);
            }
            else
            {
                template.Parse("S_DISPLAY_VIDEOS_NO", radioChecked);
            }

            template.Parse("S_CUSTOM_DOMAIN", customDomainTextBox);
            template.Parse("S_ANALYTICS_CODE", analyticsCodeTextBox);

            if (!string.IsNullOrEmpty(core.Settings.TwitterApiKey))
            {
                template.Parse("S_TWITTER_INTEGRATION", "TRUE");
            }

            if (!string.IsNullOrEmpty(core.Settings.TumblrApiKey))
            {
                template.Parse("S_TUMBLR_INTEGRATION", "TRUE");
            }

            if (core.Settings.FacebookEnabled || ((!string.IsNullOrEmpty(core.Settings.FacebookApiAppid)) && LoggedInMember.UserInfo.FacebookAuthenticated))
            {
                template.Parse("S_FACEBOOK_INTEGRATION", "TRUE");
            }

            if (string.IsNullOrEmpty(LoggedInMember.UserInfo.TwitterUserName))
            {
                template.Parse("S_TWITTER_USER_NAME", twitterUserNameTextBox);
            }
            else
            {
                template.Parse("TWITTER_USER_NAME", LoggedInMember.UserInfo.TwitterUserName);
                template.Parse("S_SYDNDICATE_TWITTER", twitterSyndicateCheckBox);
                template.Parse("U_UNLINK_TWITTER", core.Hyperlink.AppendSid(BuildUri("preferences", "unlink-twitter"), true));
            }

            if (string.IsNullOrEmpty(LoggedInMember.UserInfo.TumblrUserName))
            {
                template.Parse("U_LINK_TUMBLR", core.Hyperlink.AppendSid(BuildUri("preferences", "link-tumblr"), true));
            }
            else
            {
                /* TODO: get list of tumblr blogs */

                template.Parse("TUMBLR_USER_NAME", LoggedInMember.UserInfo.TumblrUserName);
                template.Parse("S_TUMBLR_BLOGS", tumblrBlogsSelectBox);
                template.Parse("S_SYDNDICATE_TUMBLR", tumblrSyndicateCheckBox);
                template.Parse("U_UNLINK_TUMBLR", core.Hyperlink.AppendSid(BuildUri("preferences", "unlink-tumblr"), true));
            }

            if (string.IsNullOrEmpty(LoggedInMember.UserInfo.FacebookUserId))
            {
                string appId = core.Settings.FacebookApiAppid;
                string redirectTo = (core.Settings.UseSecureCookies ? "https://" : "http://") + Hyperlink.Domain + "/api/facebook/callback";

                template.Parse("U_LINK_FACEBOOK", string.Format("https://www.facebook.com/dialog/oauth?client_id={0}&redirect_uri={1}&scope={2}", appId, System.Web.HttpUtility.UrlEncode(redirectTo), "publish_actions"));
            }
            else
            {
                template.Parse("S_SYDNDICATE_FACEBOOK", facebookSyndicateCheckBox);
                template.Parse("S_FACEBOOK_SHARE_PERMISSIONS", facebookSharePermissionSelectBox);
                template.Parse("U_UNLINK_FACEBOOK", core.Hyperlink.AppendSid(BuildUri("preferences", "unlink-facebook"), true));
            }

            DataTable pagesTable = db.Query(string.Format("SELECT page_id, page_slug, page_parent_path FROM user_pages WHERE page_item_id = {0} AND page_item_type_id = {1} ORDER BY page_order ASC;",
                LoggedInMember.UserId, ItemKey.GetTypeId(typeof(User))));

            SelectBox pagesSelectBox = new SelectBox("homepage");

            foreach (DataRow pageRow in pagesTable.Rows)
            {
                if (string.IsNullOrEmpty((string)pageRow["page_parent_path"]))
                {
                    pagesSelectBox.Add(new SelectBoxItem("/" + (string)pageRow["page_slug"], "/" + (string)pageRow["page_slug"]));
                }
                else
                {
                    pagesSelectBox.Add(new SelectBoxItem("/" + (string)pageRow["page_parent_path"] + "/" + (string)pageRow["page_slug"], "/" + (string)pageRow["page_parent_path"] + "/" + (string)pageRow["page_slug"]));
                }
            }

            SelectBox timezoneSelectBox = UnixTime.BuildTimeZoneSelectBox("timezone");
            timezoneSelectBox.SelectedKey = LoggedInMember.UserInfo.TimeZoneCode.ToString();

            pagesSelectBox.SelectedKey = LoggedInMember.UserInfo.ProfileHomepage;
            template.Parse("S_HOMEPAGE", pagesSelectBox);
            template.Parse("S_TIMEZONE", timezoneSelectBox);
            //core.Display.ParseTimeZoneBox(template, "S_TIMEZONE", LoggedInMember.TimeZoneCode.ToString());

            if (core.Http.Query["status"] == "facebook-auth-failed")
            {
                DisplayError("Failed to link your Facebook profile");
            }

        }

        void AccountPreferences_Save(object sender, EventArgs e)
        {
            //User loggedInMember = (User)loggedInMember;
            AuthoriseRequestSid();

            bool displayImages = true;
            bool displayFlash = true;
            bool displayVideos = true;
            bool displayAudio = true;
            bool showCustomStyles = false;
            bool emailNotifications = true;
            BbcodeOptions showBbcode = BbcodeOptions.None;
            string homepage = "/profile";
            string customDomain = string.Empty;
            string analyticsCode = string.Empty;
            ushort timeZoneCode = 30;
            string twitterUserName = string.Empty;
            bool twitterSyndicate = (core.Http.Form["twitter-syndicate"] != null);
            bool tumblrSyndicate = (core.Http.Form["tumblr-syndicate"] != null);
            bool facebookSyndicate = (core.Http.Form["facebook-syndicate"] != null);
            string facebookSharePermissions = core.Http.Form["facebook-share-permissions"];

            try
            {
                displayImages = (int.Parse(core.Http.Form["display-images"]) == 1);
                displayFlash = (int.Parse(core.Http.Form["display-flash"]) == 1);
                displayVideos = (int.Parse(core.Http.Form["display-videos"]) == 1);
                // TODO: displayAudio
                showCustomStyles = (int.Parse(core.Http.Form["show-styles"]) == 1);
                emailNotifications = (int.Parse(core.Http.Form["email-notifications"]) == 1);
                homepage = core.Http.Form["homepage"];
                customDomain = core.Http.Form["custom-domain"].ToLower();
                analyticsCode = core.Http.Form["analytics-code"];
                timeZoneCode = ushort.Parse(core.Http.Form["timezone"]);
                twitterUserName = core.Http.Form["twitter-user-name"];
            }
            catch
            {
            }

            if (!string.IsNullOrEmpty(customDomain))
            {
                try
                {
                    IPHostEntry host = Dns.GetHostEntry(customDomain);
                    IPHostEntry host2 = Dns.GetHostEntry(Hyperlink.Domain);

                    if (host.HostName.ToLower() != host2.HostName.ToLower() && host.HostName.ToLower() != Hyperlink.Domain)
                    {
                        SetError("Invalid domain, you need to add a CNAME entry to " + Hyperlink.Domain + " in the DNS settings for your domain.");
                        return;
                    }
                }
                catch (System.Net.Sockets.SocketException)
                {
                    SetError("Invalid domain, you need to add a CNAME entry to " + Hyperlink.Domain + " in the DNS settings for your domain.");
                    return;
                }
            }

            if (homepage != "/profile" && homepage != "/blog")
            {
                try
                {
                    Page thisPage = new Page(core, LoggedInMember, homepage.TrimStart(new char[] { '/' }));
                }
                catch (PageNotFoundException)
                {
                    homepage = "/profile";
                }
            }

            if (displayImages)
            {
                showBbcode |= BbcodeOptions.ShowImages;
            }
            if (displayFlash)
            {
                showBbcode |= BbcodeOptions.ShowFlash;
            }
            if (displayVideos)
            {
                showBbcode |= BbcodeOptions.ShowVideo;
            }
            if (displayAudio)
            {
                showBbcode |= BbcodeOptions.ShowAudio;
            }

            LoggedInMember.UserInfo.ShowCustomStyles = showCustomStyles;
            LoggedInMember.UserInfo.EmailNotifications = emailNotifications;
            LoggedInMember.UserInfo.SetUserBbcodeOptions = showBbcode;
            LoggedInMember.UserInfo.ProfileHomepage = homepage;
            LoggedInMember.UserInfo.TimeZoneCode = timeZoneCode;
            LoggedInMember.UserInfo.AnalyticsCode = analyticsCode;

            if (!string.IsNullOrEmpty(twitterUserName))
            {
                Twitter t = new Twitter(core.Settings.TwitterApiKey, core.Settings.TwitterApiSecret);
                TwitterAuthToken auth = t.OAuthRequestToken();

                LoggedInMember.UserInfo.TwitterToken = auth.Token;
                LoggedInMember.UserInfo.TwitterTokenSecret = auth.Secret;
                LoggedInMember.UserInfo.TwitterAuthenticated = false;
                LoggedInMember.UserInfo.TwitterSyndicate = false;

                LoggedInMember.UserInfo.Update();

                core.Http.Redirect("https://api.twitter.com/oauth/authorize?oauth_token=" + auth.Token + "&screen_name=" + twitterUserName + "force_login=true");
            }

            if (LoggedInMember.UserInfo.TwitterAuthenticated)
            {
                LoggedInMember.UserInfo.TwitterSyndicate = twitterSyndicate;
            }

            if (LoggedInMember.UserInfo.TumblrAuthenticated)
            {
                LoggedInMember.UserInfo.TumblrSyndicate = tumblrSyndicate;
            }

            if (LoggedInMember.UserInfo.FacebookAuthenticated)
            {
                LoggedInMember.UserInfo.FacebookSyndicate = facebookSyndicate;

                switch (facebookSharePermissions)
                {
                    case "":
                    case "EVERYONE":
                    case "ALL_FRIENDS":
                    case "FRIENDS_OF_FRIENDS":
                        LoggedInMember.UserInfo.FacebookSharePermissions = facebookSharePermissions;
                        break;
                }
            }

            LoggedInMember.UserInfo.Update();

            LoggedInMember.UserDomain = customDomain;

            //SetRedirectUri(BuildUri());
            //Display.ShowMessage("Preferences Saved", "Your preferences have been saved in the database.");
			SetInformation("Your preferences have been saved in the database.");
        }

        void AccountPreferences_UnlinkTwitter(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            LoggedInMember.UserInfo.TwitterUserName = string.Empty;
            LoggedInMember.UserInfo.TwitterToken = string.Empty;
            LoggedInMember.UserInfo.TwitterTokenSecret = string.Empty;
            LoggedInMember.UserInfo.TwitterAuthenticated = false;
            LoggedInMember.UserInfo.TwitterSyndicate = false;

            LoggedInMember.UserInfo.Update();

            core.Http.Redirect(BuildUri());
        }

        void AccountPreferences_LinkTumblr(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            Tumblr t = new Tumblr(core.Settings.TumblrApiKey, core.Settings.TumblrApiSecret);
            TumblrAuthToken auth = t.OAuthRequestToken();

            LoggedInMember.UserInfo.TumblrToken = auth.Token;
            LoggedInMember.UserInfo.TumblrTokenSecret = auth.Secret;
            LoggedInMember.UserInfo.TumblrAuthenticated = false;
            LoggedInMember.UserInfo.TumblrSyndicate = false;

            LoggedInMember.UserInfo.Update();

            core.Http.Redirect("https://www.tumblr.com/oauth/authorize?oauth_token=" + auth.Token);
        }

        void AccountPreferences_UnlinkTumblr(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            LoggedInMember.UserInfo.TumblrUserName = string.Empty;
            LoggedInMember.UserInfo.TumblrHostname = string.Empty;
            LoggedInMember.UserInfo.TumblrToken = string.Empty;
            LoggedInMember.UserInfo.TumblrTokenSecret = string.Empty;
            LoggedInMember.UserInfo.TumblrAuthenticated = false;
            LoggedInMember.UserInfo.TumblrSyndicate = false;

            LoggedInMember.UserInfo.Update();

            core.Http.Redirect(BuildUri());
        }

        void AccountPreferences_UnlinkFacebook(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            LoggedInMember.UserInfo.FacebookUserId = string.Empty;
            LoggedInMember.UserInfo.FacebookCode = string.Empty;
            LoggedInMember.UserInfo.FacebookAccessToken = string.Empty;
            LoggedInMember.UserInfo.FacebookAuthenticated = false;
            LoggedInMember.UserInfo.FacebookSyndicate = false;

            LoggedInMember.UserInfo.Update();

            core.Http.Redirect(BuildUri());
        }
    }
}
