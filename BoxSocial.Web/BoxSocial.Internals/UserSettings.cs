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
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [JsonObject("user_settings")]
    public class UserSettings
    {
        private User user;

        public UserSettings(User user)
        {
            this.user = user;
        }

        [JsonProperty("id")]
        public long UserId
        {
            get
            {
                return user.UserId;
            }
        }

        [JsonProperty("twitter_username")]
        public string TwitterUserName
        {
            get
            {
                return user.UserInfo.TwitterUserName;
            }
            internal set
            {
                user.UserInfo.TwitterUserName = value;
            }
        }

        [JsonProperty("twitter_syndicate")]
        public bool TwitterSyndicate
        {
            get
            {
                return user.UserInfo.TwitterSyndicate;
            }
            internal set
            {
                user.UserInfo.TwitterSyndicate = value;
            }
        }

        [JsonProperty("twitter_authenticated")]
        public bool TwitterAuthenticated
        {
            get
            {
                return user.UserInfo.TwitterAuthenticated;
            }
            internal set
            {
                user.UserInfo.TwitterAuthenticated = value;
            }
        }

        [JsonProperty("facebook_syndicate")]
        public bool FacebookSyndicate
        {
            get
            {
                return user.UserInfo.FacebookSyndicate;
            }
            internal set
            {
                user.UserInfo.FacebookSyndicate = value;
            }
        }

        [JsonProperty("facebook_authenticated")]
        public bool FacebookAuthenticated
        {
            get
            {
                return user.UserInfo.FacebookAuthenticated;
            }
            internal set
            {
                user.UserInfo.FacebookAuthenticated = value;
            }
        }

        [JsonProperty("tumblr_username")]
        public string TumblrUserName
        {
            get
            {
                return user.UserInfo.TumblrUserName;
            }
            internal set
            {
                user.UserInfo.TumblrUserName = value;
            }
        }

        [JsonProperty("tumblr_hostname")]
        public string TumblrHostname
        {
            get
            {
                return user.UserInfo.TumblrHostname;
            }
            internal set
            {
                user.UserInfo.TumblrHostname = value;
            }
        }

        [JsonProperty("tumblr_syndicate")]
        public bool TumblrSyndicate
        {
            get
            {
                return user.UserInfo.TumblrSyndicate;
            }
            internal set
            {
                user.UserInfo.TumblrSyndicate = value;
            }
        }

        [JsonProperty("tumblr_authenticated")]
        public bool TumblrAuthenticated
        {
            get
            {
                return user.UserInfo.TumblrAuthenticated;
            }
            internal set
            {
                user.UserInfo.TumblrAuthenticated = value;
            }
        }

        public void Update()
        {
            user.UserInfo.Update();
            user.Profile.Update();
        }
    }
}
