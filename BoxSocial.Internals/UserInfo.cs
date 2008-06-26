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
using System.Data;
using System.Text;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("user_info")]
    public sealed class UserInfo : Item
    {
        [DataField("user_id", DataFieldKeys.Unique)]
        private long userId;
        [DataField("user_name", 64)]
        private string userName;
        [DataField("user_reg_ip", 50)]
        private string registrationIp;
        [DataField("user_password", 128)]
        private string userPassword;
        [DataField("user_alternate_email", 255)]
        private string primaryEmail;
        [DataField("user_time_zone")]
        private ushort timeZoneCode;
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
        [DataField("user_icon")]
        private long displayPictureId;
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
        [DataField("user_reg_date_ut")]
        private long registrationDateRaw;
        [DataField("user_last_visit_ut")]
        private long lastVisitDateRaw;
        [DataField("user_status_messages")]
        private long userStatusMessages;

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
                if (userDisplayName == "")
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

        /// <summary>
        /// Gets the user's Display name with ownership
        /// </summary>
        public string DisplayNameOwnership
        {
            get
            {
                if (userNameOwnership == null)
                {
                    userNameOwnership = (userDisplayName != "") ? userDisplayName : userName;

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
                SetProperty("displayPictureId", value);
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
        /// Gets the user's number of blog subscribers
        /// </summary>
        public long BlogSubscriptions
        {
            get
            {
                return blogSubscriptions;
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

        internal UserInfo(Core core, DataRow memberRow)
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
            timeZone = new UnixTime(timeZoneCode);
        }

        public override long Id
        {
            get
            {
                return userId;
            }
        }

        public override string Namespace
        {
            get
            {
                return "USER";
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
