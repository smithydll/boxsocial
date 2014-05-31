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
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("user_info", "USER")]
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
        [DataField("user_two_factor_auth_key", 16)]
        private string twoFactorAuthKey;
        [DataField("user_two_factor_auth_verified")]
        private bool twoFactorAuthVerified;

        private User user;
        private string userNameOwnership;
        private UnixTime timeZone;

        /// <summary>
        /// Gets the user Id
        /// </summary>
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
        public string DisplayNameOwnership
        {
            get
            {
                if (userNameOwnership == null)
                {
                    userNameOwnership = (!String.IsNullOrEmpty(userDisplayName)) ? userDisplayName : userName;

                    if (userNameOwnership.EndsWith("s"))
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
        public BbcodeOptions GetUserBbcodeOptions
        {
            get
            {
                return (BbcodeOptions)showBbcode;
            }
        }

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
        public long StatusMessages
        {
            get
            {
                return userStatusMessages;
            }
        }

        public long UnreadNotifications
        {
            get
            {
                return userUnreadNotifications;
            }
        }

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

        /// <summary>
        /// Gets the user's last visit date
        /// </summary>
        public DateTime LastOnlineTime
        {
            get
            {
                return GetLastOnlineDate(GetTimeZone);
            }
        }

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
        public long BlogSubscriptions
        {
            get
            {
                return blogSubscriptions;
            }
        }

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

        void UserInfo_ItemLoad()
        {
            timeZone = new UnixTime(core, timeZoneCode);
        }

        public override long Id
        {
            get
            {
                return userId;
            }
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
