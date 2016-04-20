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
using System.Data;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("user_info", "USER")]
    [JsonObject("user_info")]
    public sealed class UserInfo : NumberedItem
    {
        [DataField("user_id", DataFieldKeys.Unique)]
        private long userId;
        [DataField("user_name", DataFieldKeys.Unique, 64)]
        private string userName;
        [DataField("user_reg_ip", 50)]
        private string registrationIp;
        [DataField("user_password", 128)]
        private string userPassword;
        [DataField("user_alternate_email", 255)]
        private string primaryEmail;
        [DataField("user_time_zone")]
        private ushort timeZoneCode;
        [DataField("user_language", 8)]
        private string language;
        [DataField("user_active")]
        private bool userActivated;
        [DataField("user_activate_code", 64)]
        private string activationCode;
        [DataField("user_name_display", 64)]
        private string userDisplayName;
        [DataField("user_live_messenger", 255)]
        private string contactLiveMessenger;
        [DataField("user_yahoo_messenger", 255)]
        private string contactYahooMessenger;
        [DataField("user_jabber_address", 255)]
        private string contactXmpp;
        [DataField("user_home_page", MYSQL_TEXT)]
        private string profileHomepage;
        [DataField("user_blog_subscriptions")]
        private long blogSubscriptions;
        [DataField("user_icon", DataFieldKeys.Index)]
        private long displayPictureId;
        [DataField("user_cover")]
        private long coverPhotoId;
        [DataField("user_auto_login_id", 128)]
        private string autoLoginId;
        [DataField("user_friends")]
        private long friends;
        [DataField("user_family")]
        private long family;
        [DataField("user_block")]
        private long blocked;
        [DataField("user_show_bbcode")]
        private byte showBbcode;
        [DataField("user_users_blocked")]
        private long usersBlocked;
        [DataField("user_gallery_items")]
        private long galleryItems;
        [DataField("user_show_custom_styles")]
        private bool showCustomStyles;
        [DataField("user_email_notifications")]
        private bool emailNotifications;
        [DataField("user_bytes")]
        private ulong bytes;
        [DataField("user_bytes_month")]
        private ulong bytesMonth;
        [DataField("user_subscription_expirary")]
        private long subscriptionEndDate;
        [DataField("user_month_start")]
        private long bytesMonthStartDate;
        [DataField("user_reg_date_ut")]
        private long registrationDateRaw;
        [DataField("user_last_visit_ut")]
        private long lastVisitDateRaw;
        [DataField("user_status_messages")]
        private long userStatusMessages;
        [DataField("user_unread_notifications")]
        private long userUnreadNotifications;
        [DataField("user_unseen_mail")]
        private long userUnseenMail;
        [DataField("user_new_password", 63)]
        private string userNewPassword;
        [DataField("user_subscription_level")]
        private byte userSubscriptionLevel;
        [DataField("user_subscriptions")]
        private long userSubscriptions;
        [DataField("user_analytics_code", 15)]
        protected string analyticsCode;
        [DataField("user_invites")]
        private long userInvites;
        [DataField("user_twitter_user_name", 18)]
        private string userTwitterUserName;
        [DataField("user_twitter_token", 63)]
        private string userTwitterToken;
        [DataField("user_twitter_token_secret", 63)]
        private string userTwitterTokenSecret;
        [DataField("user_twitter_syndicate")]
        private bool userTwitterSyndicate;
        [DataField("user_twitter_authenticated")]
        private bool userTwitterAthenticated;
        [DataField("user_facebook_user_id", 63)]
        private string userFacebookUserId;
        [DataField("user_facebook_code", MYSQL_TEXT)]
        private string userFacebookCode;
        [DataField("user_facebook_access_token", MYSQL_TEXT)]
        private string userFacebookAccessToken;
        [DataField("user_facebook_expires_ut")]
        private long userFacebookExpires;
        [DataField("user_facebook_syndicate")]
        private bool userFacebookSyndicate;
        [DataField("user_facebook_authenticated")]
        private bool userFacebookAthenticated;
        [DataField("user_facebook_share_permissions")]
        private string userFacebookSharePermissions;
        [DataField("user_tumblr_user_name", 31)]
        private string userTumblrUserName;
        [DataField("user_tumblr_hostname", 63)]
        private string userTumblrHostname;
        [DataField("user_tumblr_token", 63)]
        private string userTumblrToken;
        [DataField("user_tumblr_token_secret", 63)]
        private string userTumblrTokenSecret;
        [DataField("user_tumblr_syndicate")]
        private bool userTumblrSyndicate;
        [DataField("user_tumblr_authenticated")]
        private bool userTumblrAthenticated;
        [DataField("user_two_factor_auth_key", 16)]
        private string twoFactorAuthKey;
        [DataField("user_two_factor_auth_verified")]
        private bool twoFactorAuthVerified;
        [DataField("user_allow_monetisation")]
        private bool allowMonetisation;
        [DataField("user_adsense_code", 12)]
        private string adsenseCode;

        // If a user has a compatible device with the app installed they can get push notifications
        // or if they use a compatible browser they can get push notifications
        [DataField("user_windows_authentication_enabled")]
        private bool windowsNotificationEnabled;
        [DataField("user_android_authentication_enabled")]
        private bool androidNotificationEnabled;
        [DataField("user_ios_authentication_enabled")]
        private bool iosNotificationEnabled;   

        private User user;
        private string userNameOwnership;
        private UnixTime timeZone;

        /// <summary>
        /// Gets the user Id
        /// </summary>
        [JsonProperty("id")]
        public long UserId
        {
            get
            {
                return userId;
            }
        }

        /// <summary>
        /// Gets the user name
        /// </summary>
        [JsonProperty("username")]
        public string UserName
        {
            get
            {
                return userName;
            }
        }

        /// <summary>
        /// Gets the user's Display name
        /// </summary>
        [JsonIgnore]
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(userDisplayName))
                {
                    return userName;
                }
                else
                {
                    return userDisplayName;
                }
            }
            set
            {
                SetProperty("userDisplayName", value);
            }
        }

        [JsonIgnore]
        public string TwoFactorAuthKey
        {
            get
            {
                return twoFactorAuthKey;
            }
            internal set
            {
                SetPropertyByRef(new { twoFactorAuthKey }, value);
            }
        }

        [JsonIgnore]
        public bool TwoFactorAuthVerified
        {
            get
            {
                return twoFactorAuthVerified;
            }
            internal set
            {
                SetPropertyByRef(new { twoFactorAuthVerified }, value);
            }
        }

        [JsonIgnore]
        public string TwitterUserName
        {
            get
            {
                return userTwitterUserName;
            }
            internal set
            {
                SetPropertyByRef(new { userTwitterUserName }, value);
            }
        }

        [JsonIgnore]
        public bool TwitterSyndicate
        {
            get
            {
                return userTwitterSyndicate;
            }
            internal set
            {
                SetPropertyByRef(new { userTwitterSyndicate }, value);
            }
        }

        [JsonIgnore]
        public bool TwitterAuthenticated
        {
            get
            {
                return userTwitterAthenticated;
            }
            internal set
            {
                SetPropertyByRef(new { userTwitterAthenticated }, value);
            }
        }

        [JsonIgnore]
        internal string TwitterToken
        {
            get
            {
                return userTwitterToken;
            }
            set
            {
                SetPropertyByRef(new { userTwitterToken }, value);
            }
        }

        [JsonIgnore]
        internal string TwitterTokenSecret
        {
            get
            {
                return userTwitterTokenSecret;
            }
            set
            {
                SetPropertyByRef(new { userTwitterTokenSecret }, value);
            }
        }

        [JsonIgnore]
        public string FacebookUserId
        {
            get
            {
                return userFacebookUserId;
            }
            internal set
            {
                SetPropertyByRef(new { userFacebookUserId }, value);
            }
        }

        [JsonIgnore]
        public bool FacebookSyndicate
        {
            get
            {
                return userFacebookSyndicate;
            }
            internal set
            {
                SetPropertyByRef(new { userFacebookSyndicate }, value);
            }
        }

        [JsonIgnore]
        public bool FacebookAuthenticated
        {
            get
            {
                return userFacebookAthenticated;
            }
            internal set
            {
                SetPropertyByRef(new { userFacebookAthenticated }, value);
            }
        }

        [JsonIgnore]
        internal string FacebookCode
        {
            get
            {
                return userFacebookCode;
            }
            set
            {
                SetPropertyByRef(new { userFacebookCode }, value);
            }
        }

        [JsonIgnore]
        internal string FacebookAccessToken
        {
            get
            {
                return userFacebookAccessToken;
            }
            set
            {
                SetPropertyByRef(new { userFacebookAccessToken }, value);
            }
        }

        [JsonIgnore]
        internal long FacebookExpires
        {
            get
            {
                return userFacebookExpires;
            }
            set
            {
                SetPropertyByRef(new { userFacebookExpires }, value);
            }
        }

        [JsonIgnore]
        internal string FacebookSharePermissions
        {
            get
            {
                return userFacebookSharePermissions;
            }
            set
            {
                SetPropertyByRef(new { userFacebookSharePermissions }, value);
            }
        }

        [JsonProperty("tumblr_username")]
        public string TumblrUserName
        {
            get
            {
                return userTumblrUserName;
            }
            internal set
            {
                SetPropertyByRef(new { userTumblrUserName }, value);
            }
        }

        [JsonIgnore]
        public string TumblrHostname
        {
            get
            {
                return userTumblrHostname;
            }
            internal set
            {
                SetPropertyByRef(new { userTumblrHostname }, value);
            }
        }

        [JsonIgnore]
        public bool TumblrSyndicate
        {
            get
            {
                return userTumblrSyndicate;
            }
            internal set
            {
                SetPropertyByRef(new { userTumblrSyndicate }, value);
            }
        }

        [JsonIgnore]
        public bool TumblrAuthenticated
        {
            get
            {
                return userTumblrAthenticated;
            }
            internal set
            {
                SetPropertyByRef(new { userTumblrAthenticated }, value);
            }
        }

        [JsonIgnore]
        internal string TumblrToken
        {
            get
            {
                return userTumblrToken;
            }
            set
            {
                SetPropertyByRef(new { userTumblrToken }, value);
            }
        }

        [JsonIgnore]
        internal string TumblrTokenSecret
        {
            get
            {
                return userTumblrTokenSecret;
            }
            set
            {
                SetPropertyByRef(new { userTumblrTokenSecret }, value);
            }
        }

        [JsonIgnore]
        public string AnalyticsCode
        {
            get
            {
                return analyticsCode;
            }
            set
            {
                SetProperty("analyticsCode", value);
            }
        }

        /// <summary>
        /// Gets the user's Display name with ownership
        /// </summary>
        [JsonIgnore]
        public string DisplayNameOwnership
        {
            get
            {
                if (userNameOwnership == null)
                {
                    userNameOwnership = (!String.IsNullOrEmpty(userDisplayName)) ? userDisplayName : userName;

                    if (userNameOwnership.EndsWith("s", StringComparison.Ordinal))
                    {
                        userNameOwnership = userNameOwnership + "'";
                    }
                    else
                    {
                        userNameOwnership = userNameOwnership + "'s";
                    }
                }
                return userNameOwnership;
            }
        }

        [JsonIgnore]
        public SubscriberLevel SubscriptionLevel
        {
            get
            {
                return (SubscriberLevel)userSubscriptionLevel;
            }
            set
            {
                SetProperty("userSubscriptionLevel", (byte)value);
            }
        }

        [JsonIgnore]
        public long Subscriptions
        {
            get
            {
                return userSubscriptions;
            }
            set
            {
                SetProperty("userSubscriptions", value);
            }
        }

        /// <summary>
        /// Gets whether the user wants to see custom styles
        /// </summary>
        [JsonIgnore]
        public bool ShowCustomStyles
        {
            get
            {
                return showCustomStyles;
            }
            set
            {
                SetProperty("showCustomStyles", value);
            }
        }

        /// <summary>
        /// Gets whether the user wants to see images in BBcode enabled fields
        /// </summary>
        [JsonIgnore]
        public bool BbcodeShowImages
        {
            get
            {
                return ((BbcodeOptions)showBbcode & BbcodeOptions.ShowImages) == BbcodeOptions.ShowImages;
            }
        }

        /// <summary>
        /// Gets whether the user wants to see flash in BBcode enabled fields
        /// </summary>
        [JsonIgnore]
        public bool BbcodeShowFlash
        {
            get
            {
                return ((BbcodeOptions)showBbcode & BbcodeOptions.ShowFlash) == BbcodeOptions.ShowFlash;
            }
        }

        /// <summary>
        /// Gets whether the user wants to see vieos in BBcode enabled fields
        /// </summary>
        [JsonIgnore]
        public bool BbcodeShowVideos
        {
            get
            {
                return ((BbcodeOptions)showBbcode & BbcodeOptions.ShowVideo) == BbcodeOptions.ShowVideo;
            }
        }

        /// <summary>
        /// Gets the user's BBcode display options
        /// </summary>
        [JsonIgnore]
        public BbcodeOptions GetUserBbcodeOptions
        {
            get
            {
                return (BbcodeOptions)showBbcode;
            }
        }

        [JsonIgnore]
        public BbcodeOptions SetUserBbcodeOptions
        {
            set
            {
                SetProperty("showBbcode", (byte)value);
            }
        }

        /// <summary>
        /// Gets the user's homepage
        /// </summary>
        [JsonProperty("homepage")]
        public string ProfileHomepage
        {
            get
            {
                return profileHomepage;
            }
            set
            {
                SetProperty("profileHomepage", value);
            }
        }

        /// <summary>
        /// Gets the user's friend count
        /// </summary>
        [JsonIgnore]
        public long Friends
        {
            get
            {
                return friends;
            }
        }

        /// <summary>
        /// Gets the user's primary e-mail address
        /// </summary>
        [JsonIgnore]
        public string PrimaryEmail
        {
            get
            {
                return primaryEmail;
            }
        }

        /// <summary>
        /// Gets the user's e-mail notifications preference
        /// </summary>
        [JsonIgnore]
        public bool EmailNotifications
        {
            get
            {
                return emailNotifications;
            }
            set
            {
                SetProperty("emailNotifications", value);
            }
        }

        /// <summary>
        /// Gets the number of bytes the user has consumed on the filesystem
        /// </summary>
        [JsonProperty("bytes_used")]
        public ulong BytesUsed
        {
            get
            {
                return bytes;
            }
        }

        /// <summary>
        /// Gets the user's number of status messages
        /// </summary>
        [JsonIgnore]
        public long StatusMessages
        {
            get
            {
                return userStatusMessages;
            }
        }

        [JsonIgnore]
        public long UnreadNotifications
        {
            get
            {
                return userUnreadNotifications;
            }
        }

        [JsonIgnore]
        public long UnseenMail
        {
            get
            {
                return userUnseenMail;
            }
        }

        /// <summary>
        /// Gets the user's display picture Id
        /// </summary>
        [JsonIgnore]
        public long DisplayPictureId
        {
            get
            {
                return displayPictureId;
            }
            set
            {
                SetPropertyByRef(new { displayPictureId }, value);
            }
        }

        /// <summary>
        /// Gets the user's display picture Id
        /// </summary>
        [JsonIgnore]
        public long CoverPhotoId
        {
            get
            {
                return coverPhotoId;
            }
            set
            {
                SetPropertyByRef(new { coverPhotoId }, value);
            }
        }

        [JsonIgnore]
        public long LastVisitDateRaw
        {
            get
            {
                return lastVisitDateRaw;
            }
        }

        /// <summary>
        /// Gets the user's last visit date
        /// </summary>
        [JsonIgnore]
        public DateTime LastOnlineTime
        {
            get
            {
                return GetLastOnlineDate(GetTimeZone);
            }
        }

        [JsonIgnore]
        public DateTime RegistrationDate
        {
            get
            {
                return GetRegistrationDate(GetTimeZone);
            }
        }

        /// <summary>
        /// Gets the user's time zone code
        /// </summary>
        [JsonIgnore]
        public ushort TimeZoneCode
        {
            get
            {
                return timeZoneCode;
            }
            set
            {
                SetProperty("timeZoneCode", value);
            }
        }

        /// <summary>
        /// Gets the user's time zone object
        /// </summary>
        [JsonIgnore]
        public UnixTime GetTimeZone
        {
            get
            {
                return timeZone;
            }
        }

        /// <summary>
        /// Get's the user's language code
        /// </summary>
        [JsonProperty("language")]
        public string Language
        {
            get
            {
                return language;
            }
            set
            {
                SetPropertyByRef(new { language }, value);
            }
        }

        /// <summary>
        /// Gets the user's number of blog subscribers
        /// </summary>
        [JsonIgnore]
        public long BlogSubscriptions
        {
            get
            {
                return blogSubscriptions;
            }
        }

        [JsonIgnore]
        public long Invites
        {
            get
            {
                return userInvites;
            }
            set
            {
                SetPropertyByRef(new { userInvites }, value);
            }
        }

        [JsonIgnore]
        public bool AllowMonetisation
        {
            get
            {
                return allowMonetisation;
            }
            internal set
            {
                SetPropertyByRef(new { allowMonetisation }, value);
            }
        }

        [JsonIgnore]
        internal string AdsenseCode
        {
            get
            {
                return adsenseCode;
            }
            set
            {
                SetPropertyByRef(new { adsenseCode }, value);
            }
        }

        /// <summary>
        /// Gets the user's registration date
        /// </summary>
        /// <param name="tz"></param>
        /// <returns></returns>
        public DateTime GetRegistrationDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(registrationDateRaw);
        }

        /// <summary>
        /// Gets the user's last online date
        /// </summary>
        /// <param name="tz"></param>
        /// <returns></returns>
        public DateTime GetLastOnlineDate(UnixTime tz)
        {
            if (tz != null)
            {
                return tz.DateTimeFromMysql(lastVisitDateRaw);
            }
            else
            {
                return timeZone.DateTimeFromMysql(lastVisitDateRaw);
            }
        }

        internal UserInfo(Core core, long userId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserInfo_ItemLoad);

            try
            {
                LoadItem("user_id", userId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidUserException();
            }
        }

        public UserInfo(Core core, DataRow memberRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserInfo_ItemLoad);

            try
            {
                loadItemInfo(memberRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidUserException();
            }
        }

        public UserInfo(Core core, System.Data.Common.DbDataReader memberRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserInfo_ItemLoad);

            try
            {
                loadItemInfo(memberRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidUserException();
            }
        }

        protected override void loadItemInfo(DataRow userRow)
        {
            loadValue(userRow, "user_id", out userId);
            loadValue(userRow, "user_name", out userName);
            loadValue(userRow, "user_reg_ip", out registrationIp);
            loadValue(userRow, "user_password", out userPassword);
            loadValue(userRow, "user_alternate_email", out primaryEmail);
            loadValue(userRow, "user_time_zone", out timeZoneCode);
            loadValue(userRow, "user_language", out language);
            loadValue(userRow, "user_active", out userActivated);
            loadValue(userRow, "user_activate_code", out activationCode);
            loadValue(userRow, "user_name_display", out userDisplayName);
            loadValue(userRow, "user_live_messenger", out contactLiveMessenger);
            loadValue(userRow, "user_yahoo_messenger", out contactYahooMessenger);
            loadValue(userRow, "user_jabber_address", out contactXmpp);
            loadValue(userRow, "user_home_page", out profileHomepage);
            loadValue(userRow, "user_blog_subscriptions", out blogSubscriptions);
            loadValue(userRow, "user_icon", out displayPictureId);
            loadValue(userRow, "user_cover", out coverPhotoId);
            loadValue(userRow, "user_auto_login_id", out autoLoginId);
            loadValue(userRow, "user_friends", out friends);
            loadValue(userRow, "user_family", out family);
            loadValue(userRow, "user_block", out blocked);
            loadValue(userRow, "user_show_bbcode", out showBbcode);
            loadValue(userRow, "user_users_blocked", out usersBlocked);
            loadValue(userRow, "user_gallery_items", out galleryItems);
            loadValue(userRow, "user_show_custom_styles", out showCustomStyles);
            loadValue(userRow, "user_email_notifications", out emailNotifications);
            loadValue(userRow, "user_bytes", out bytes);
            loadValue(userRow, "user_bytes_month", out bytesMonth);
            loadValue(userRow, "user_subscription_expirary", out subscriptionEndDate);
            loadValue(userRow, "user_month_start", out bytesMonthStartDate);
            loadValue(userRow, "user_reg_date_ut", out registrationDateRaw);
            loadValue(userRow, "user_last_visit_ut", out lastVisitDateRaw);
            loadValue(userRow, "user_status_messages", out userStatusMessages);
            loadValue(userRow, "user_unread_notifications", out userUnreadNotifications);
            loadValue(userRow, "user_unseen_mail", out userUnseenMail);
            loadValue(userRow, "user_new_password", out userNewPassword);
            loadValue(userRow, "user_subscription_level", out userSubscriptionLevel);
            loadValue(userRow, "user_subscriptions", out userSubscriptions);
            loadValue(userRow, "user_analytics_code", out analyticsCode);
            loadValue(userRow, "user_invites", out userInvites);
            loadValue(userRow, "user_twitter_user_name", out userTwitterUserName);
            loadValue(userRow, "user_twitter_token", out userTwitterToken);
            loadValue(userRow, "user_twitter_token_secret", out userTwitterTokenSecret);
            loadValue(userRow, "user_twitter_syndicate", out userTwitterSyndicate);
            loadValue(userRow, "user_twitter_authenticated", out userTwitterAthenticated);
            loadValue(userRow, "user_facebook_user_id", out userFacebookUserId);
            loadValue(userRow, "user_facebook_code", out userFacebookCode);
            loadValue(userRow, "user_facebook_access_token", out userFacebookAccessToken);
            loadValue(userRow, "user_facebook_expires_ut", out userFacebookExpires);
            loadValue(userRow, "user_facebook_syndicate", out userFacebookSyndicate);
            loadValue(userRow, "user_facebook_authenticated", out userFacebookAthenticated);
            loadValue(userRow, "user_facebook_share_permissions", out userFacebookSharePermissions);
            loadValue(userRow, "user_tumblr_user_name", out userTumblrUserName);
            loadValue(userRow, "user_tumblr_hostname", out userTumblrHostname);
            loadValue(userRow, "user_tumblr_token", out userTumblrToken);
            loadValue(userRow, "user_tumblr_token_secret", out userTumblrTokenSecret);
            loadValue(userRow, "user_tumblr_syndicate", out userTumblrSyndicate);
            loadValue(userRow, "user_tumblr_authenticated", out userTumblrAthenticated);
            loadValue(userRow, "user_two_factor_auth_key", out twoFactorAuthKey);
            loadValue(userRow, "user_two_factor_auth_verified", out twoFactorAuthVerified);
            loadValue(userRow, "user_adsense_code", out adsenseCode);
            loadValue(userRow, "user_allow_monetisation", out allowMonetisation);

            itemLoaded(userRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader userRow)
        {
            loadValue(userRow, "user_id", out userId);
            loadValue(userRow, "user_name", out userName);
            loadValue(userRow, "user_reg_ip", out registrationIp);
            loadValue(userRow, "user_password", out userPassword);
            loadValue(userRow, "user_alternate_email", out primaryEmail);
            loadValue(userRow, "user_time_zone", out timeZoneCode);
            loadValue(userRow, "user_language", out language);
            loadValue(userRow, "user_active", out userActivated);
            loadValue(userRow, "user_activate_code", out activationCode);
            loadValue(userRow, "user_name_display", out userDisplayName);
            loadValue(userRow, "user_live_messenger", out contactLiveMessenger);
            loadValue(userRow, "user_yahoo_messenger", out contactYahooMessenger);
            loadValue(userRow, "user_jabber_address", out contactXmpp);
            loadValue(userRow, "user_home_page", out profileHomepage);
            loadValue(userRow, "user_blog_subscriptions", out blogSubscriptions);
            loadValue(userRow, "user_icon", out displayPictureId);
            loadValue(userRow, "user_cover", out coverPhotoId);
            loadValue(userRow, "user_auto_login_id", out autoLoginId);
            loadValue(userRow, "user_friends", out friends);
            loadValue(userRow, "user_family", out family);
            loadValue(userRow, "user_block", out blocked);
            loadValue(userRow, "user_show_bbcode", out showBbcode);
            loadValue(userRow, "user_users_blocked", out usersBlocked);
            loadValue(userRow, "user_gallery_items", out galleryItems);
            loadValue(userRow, "user_show_custom_styles", out showCustomStyles);
            loadValue(userRow, "user_email_notifications", out emailNotifications);
            loadValue(userRow, "user_bytes", out bytes);
            loadValue(userRow, "user_bytes_month", out bytesMonth);
            loadValue(userRow, "user_subscription_expirary", out subscriptionEndDate);
            loadValue(userRow, "user_month_start", out bytesMonthStartDate);
            loadValue(userRow, "user_reg_date_ut", out registrationDateRaw);
            loadValue(userRow, "user_last_visit_ut", out lastVisitDateRaw);
            loadValue(userRow, "user_status_messages", out userStatusMessages);
            loadValue(userRow, "user_unread_notifications", out userUnreadNotifications);
            loadValue(userRow, "user_unseen_mail", out userUnseenMail);
            loadValue(userRow, "user_new_password", out userNewPassword);
            loadValue(userRow, "user_subscription_level", out userSubscriptionLevel);
            loadValue(userRow, "user_subscriptions", out userSubscriptions);
            loadValue(userRow, "user_analytics_code", out analyticsCode);
            loadValue(userRow, "user_invites", out userInvites);
            loadValue(userRow, "user_twitter_user_name", out userTwitterUserName);
            loadValue(userRow, "user_twitter_token", out userTwitterToken);
            loadValue(userRow, "user_twitter_token_secret", out userTwitterTokenSecret);
            loadValue(userRow, "user_twitter_syndicate", out userTwitterSyndicate);
            loadValue(userRow, "user_twitter_authenticated", out userTwitterAthenticated);
            loadValue(userRow, "user_facebook_user_id", out userFacebookUserId);
            loadValue(userRow, "user_facebook_code", out userFacebookCode);
            loadValue(userRow, "user_facebook_access_token", out userFacebookAccessToken);
            loadValue(userRow, "user_facebook_expires_ut", out userFacebookExpires);
            loadValue(userRow, "user_facebook_syndicate", out userFacebookSyndicate);
            loadValue(userRow, "user_facebook_authenticated", out userFacebookAthenticated);
            loadValue(userRow, "user_facebook_share_permissions", out userFacebookSharePermissions);
            loadValue(userRow, "user_tumblr_user_name", out userTumblrUserName);
            loadValue(userRow, "user_tumblr_hostname", out userTumblrHostname);
            loadValue(userRow, "user_tumblr_token", out userTumblrToken);
            loadValue(userRow, "user_tumblr_token_secret", out userTumblrTokenSecret);
            loadValue(userRow, "user_tumblr_syndicate", out userTumblrSyndicate);
            loadValue(userRow, "user_tumblr_authenticated", out userTumblrAthenticated);
            loadValue(userRow, "user_two_factor_auth_key", out twoFactorAuthKey);
            loadValue(userRow, "user_two_factor_auth_verified", out twoFactorAuthVerified);
            loadValue(userRow, "user_adsense_code", out adsenseCode);
            loadValue(userRow, "user_allow_monetisation", out allowMonetisation);

            itemLoaded(userRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        void UserInfo_ItemLoad()
        {
            timeZone = new UnixTime(core, timeZoneCode);
        }

        [JsonIgnore]
        public override long Id
        {
            get
            {
                return userId;
            }
        }

        [JsonIgnore]
        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
