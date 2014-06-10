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
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public enum ActionableItemType
    {
        Text,
        Photo,
        Audio,
        Video,
    }

    public interface IActionableItem
    {
        ItemKey ItemKey
        {
            get;
        }

        Primitive Owner
        {
            get;
        }

        string Action
        {
            get;
        }

        ActionableItemType PostType
        {
            get;
        }

        byte[] Data
        {
            get;
        }

        string DataContentType
        {
            get;
        }

        string Caption
        {
            get;
        }

        string GetActionBody(List<ItemKey> subItems);

        /// <summary>
        /// Gets the URI for the commentable item.
        /// </summary>
        string Uri
        {
            get;
        }

        ItemInfo Info
        {
            get;
        }
    }

    public sealed class ActionableItem
    {
        public static void CleanUp(Core core, IActionableItem item)
        {
            if (item.Owner is User)
            {
                User owner = (User)item.Owner;
                if (owner.UserInfo.TwitterSyndicate && owner.UserInfo.TwitterAuthenticated)
                {
                    if (item.Info.TweetId > 0)
                    {
                        Twitter t = new Twitter(core.Settings.TwitterApiKey, core.Settings.TwitterApiSecret);
                        t.DeleteStatus(new TwitterAccessToken(owner.UserInfo.TwitterToken, owner.UserInfo.TwitterTokenSecret), item.Info.TweetId);
                    }
                }

                if (owner.UserInfo.TumblrSyndicate && owner.UserInfo.TumblrAuthenticated)
                {
                    if (item.Info.TumblrPostId > 0)
                    {
                        Tumblr t = new Tumblr(core.Settings.TumblrApiKey, core.Settings.TumblrApiSecret);
                        t.DeleteStatus(new TumblrAccessToken(owner.UserInfo.TumblrToken, owner.UserInfo.TumblrTokenSecret), owner.UserInfo.TumblrHostname, item.Info.TumblrPostId);
                    }
                }

                if (owner.UserInfo.FacebookSyndicate && owner.UserInfo.FacebookAuthenticated)
                {
                    if (!string.IsNullOrEmpty(item.Info.FacebookPostId))
                    {
                        Facebook fb = new Facebook(core.Settings.FacebookApiAppid, core.Settings.FacebookApiSecret);
                        FacebookAccessToken token = fb.OAuthAppAccessToken(core, owner.UserInfo.FacebookUserId);
                        fb.DeleteStatus(token, item.Info.FacebookPostId);
                    }
                }
            }
        }
    }
}
