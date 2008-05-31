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
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public sealed class UsersCache
    {
        private Core core;
        private Mysql db;

        public UsersCache(Core core)
        {
            this.core = core;
            this.db = core.db;
        }

        /// <summary>
        /// A cache of user profiles including icons.
        /// </summary>
        private Dictionary<long, User> userProfileCache = new Dictionary<long, User>();

        /// <summary>
        /// A list of usernames cached
        /// </summary>
        private Dictionary<string, long> userNameCache = new Dictionary<string, long>();

        /// <summary>
        /// A list of user Ids for batched loading
        /// </summary>
        private List<long> batchedUserIds = new List<long>();

        public User this[long key]
        {
            get
            {
                loadBatchedIds(key);
                return userProfileCache[key];
            }
        }

        public void LoadUserProfiles(List<long> userIds)
        {
            batchedUserIds.AddRange(userIds);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userIds"></param>
        private void loadUserProfiles(List<long> userIds)
        {
            List<long> idList = new List<long>();
            foreach (long id in userIds)
            {
                if (!userProfileCache.ContainsKey(id))
                {
                    idList.Add(id);
                }
            }

            if (idList.Count > 0)
            {
                SelectQuery query = new SelectQuery("user_keys");
                query.AddFields(User.GetFieldsPrefixed(typeof(User)));
                query.AddFields(UserInfo.GetFieldsPrefixed(typeof(UserInfo)));
                query.AddFields(UserProfile.GetFieldsPrefixed(typeof(UserProfile)));
                query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
                query.AddJoin(JoinTypes.Inner, UserInfo.GetTable(typeof(UserInfo)), "user_id", "user_id");
                query.AddJoin(JoinTypes.Inner, UserProfile.GetTable(typeof(UserProfile)), "user_id", "user_id");
                query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_country"), new DataField("countries", "country_iso"));
                query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_religion"), new DataField("religions", "religion_id"));
                query.AddJoin(JoinTypes.Left, new DataField("user_info", "user_icon"), new DataField("gallery_items", "gallery_item_id"));
                query.AddCondition("`user_keys`.`user_id`", ConditionEquality.In, idList);

                DataTable usersTable = db.Query(query);

                foreach (DataRow userRow in usersTable.Rows)
                {
                    User newUser = new User(core, userRow, UserLoadOptions.All);
                    userProfileCache.Add(newUser.Id, newUser);
                    userNameCache.Add(newUser.UserName, newUser.Id);
                }
            }
        }

        public void LoadUserProfile(long userId)
        {
            if (!userProfileCache.ContainsKey(userId))
            {
                batchedUserIds.Add(userId);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        public void loadUserProfile(long userId)
        {
            if (!userProfileCache.ContainsKey(userId))
            {
                User newUser = new User(core, userId, UserLoadOptions.All);
                userProfileCache.Add(newUser.Id, newUser);
                userNameCache.Add(newUser.UserName, newUser.Id);
            }
        }

        public List<long> LoadUserProfiles(List<string> usernames)
        {
            List<string> usernameList = new List<string>();
            List<long> userIds = new List<long>();
            foreach (string username in usernames)
            {
                if (!userNameCache.ContainsKey(username))
                {
                    usernameList.Add(username);
                }
            }

            if (usernameList.Count > 0)
            {
                SelectQuery query = new SelectQuery("user_keys uk");
                query.AddFields(User.USER_INFO_FIELDS, User.USER_PROFILE_FIELDS, User.USER_ICON_FIELDS);
                query.AddJoin(JoinTypes.Inner, "user_info ui", "uk.user_id", "ui.user_id");
                query.AddJoin(JoinTypes.Inner, "user_profile up", "uk.user_id", "up.user_id");
                query.AddJoin(JoinTypes.Left, "countries c", "up.profile_country", "c.country_iso");
                query.AddJoin(JoinTypes.Left, "gallery_items gi", "ui.user_icon", "gi.gallery_item_id");
                query.AddCondition("uk.user_name", ConditionEquality.In, usernameList);

                DataTable usersTable = db.Query(query);

                foreach (DataRow userRow in usersTable.Rows)
                {
                    User newUser = new User(core, userRow, UserLoadOptions.All);
                    userProfileCache.Add(newUser.Id, newUser);
                    userNameCache.Add(newUser.UserName, newUser.Id);
                    userIds.Add(newUser.Id);
                }
            }

            return userIds;
        }

        public long LoadUserProfile(string username)
        {
            if (userNameCache.ContainsKey(username))
            {
                return userNameCache[username];
            }

            User newUser = new User(core, username, UserLoadOptions.All);
            if (!userProfileCache.ContainsKey(newUser.Id))
            {
                userProfileCache.Add(newUser.Id, newUser);
            }

            return newUser.Id;
        }

        private void loadBatchedIds(long requestedId)
        {
            if (batchedUserIds.Contains(requestedId))
            {
                loadUserProfiles(batchedUserIds);
            }
        }
    }
}
