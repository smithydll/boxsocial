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
        [DataField("user_info", DataFieldKeys.Unique)]
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
        private ushort timeZone;
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
        private bool sendNotifications;
        [DataField("user_bytes")]
        private long bytes;
        [DataField("user_reg_date_ut")]
        private long registrationDateRaw;
        [DataField("user_last_visit_ut")]
        private long lastVisitDateRaw;
        [DataField("user_status_messages")]
        private long userStatusMessages;

        private string userNameOwnership;

        public long UserId
        {
            get
            {
                return userId;
            }
        }

        public string UserName
        {
            get
            {
                return userName;
            }
        }

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
        }

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

        public long BlogSubscriptions
        {
            get
            {
                return blogSubscriptions;
            }
        }

        public DateTime GetRegistrationDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(registrationDateRaw);
        }

        public DateTime GetLastOnlineDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(lastVisitDateRaw);
        }

        internal UserInfo(Core core, DataRow memberRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(MemberInfo_ItemLoad);

            try
            {
                loadItemInfo(memberRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidUserException();
            }
        }

        void MemberInfo_ItemLoad()
        {
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
