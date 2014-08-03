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
using System.Text;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public sealed class PrimitivesCache
    {
        private Core core;
        private Mysql db;

        public PrimitivesCache(Core core)
        {
            this.core = core;
            this.db = core.Db;
        }

        /// <summary>
        /// A cached primitive keys.
        /// </summary>
        private Dictionary<PrimitiveKey, PrimitiveId> primitivesKeysCached = new Dictionary<PrimitiveKey, PrimitiveId>();

        public void RegisterItem(Primitive item)
        {
            PrimitiveKey kid = new PrimitiveKey(item.Key.ToLower(), item.TypeId);

            if (!primitivesKeysCached.ContainsKey(kid))
            {
                primitivesKeysCached.Add(kid, new PrimitiveId(item.Id, item.TypeId));
            }
        }

        public User this[long key]
        {
            get
            {
                return (User)core.ItemCache[new ItemKey(key, typeof(User))];
            }
        }

        public Primitive this[ItemKey key]
        {
            get
            {
                return (Primitive)core.ItemCache[key];
            }
        }

        public Primitive this[long key, long typeId]
        {
            get
            {
                return (Primitive)core.ItemCache[new ItemKey(key, typeId)];
            }
        }

        public void LoadUserProfiles(List<long> userIds)
        {
            foreach (long userId in userIds)
            {
                core.ItemCache.RequestItem(new ItemKey(userId, typeof(User)));
            }
        }

        public void LoadUserProfile(long userId)
        {
            core.ItemCache.RequestItem(new ItemKey(userId, typeof(User)));
        }

        public void LoadPrimitiveProfile(ItemKey key)
        {
            core.ItemCache.RequestItem(key);
        }

        public void LoadPrimitiveProfile(long id, long typeId)
        {
            core.ItemCache.RequestItem(new ItemKey(id, typeId));
        }

        public void LoadPrimitiveProfiles(List<ItemKey> itemkeys)
        {
            foreach (ItemKey ik in itemkeys)
            {
                core.ItemCache.RequestItem(ik);
            }
        }

        public Dictionary<string, long> LoadUserProfiles(List<string> usernames)
        {
            long userTypeId = ItemKey.GetTypeId(typeof(User));

            List<string> usernameList = new List<string>();
            Dictionary<string, long> userIds = new Dictionary<string, long>(8, StringComparer.Ordinal);
            foreach (string username in usernames)
            {
                PrimitiveKey key = new PrimitiveKey(username.ToLower(), userTypeId);
                if (!primitivesKeysCached.ContainsKey(key))
                {
                    usernameList.Add(username.ToLower());
                }
            }

            if (usernameList.Count > 0)
            {
                SelectQuery query = new SelectQuery("user_keys");
                query.AddFields(User.GetFieldsPrefixed(core, typeof(User)));
                query.AddFields(UserInfo.GetFieldsPrefixed(core, typeof(UserInfo)));
                query.AddFields(UserProfile.GetFieldsPrefixed(core, typeof(UserProfile)));
                query.AddJoin(JoinTypes.Inner, UserInfo.GetTable(typeof(UserInfo)), "user_id", "user_id");
                query.AddJoin(JoinTypes.Inner, UserProfile.GetTable(typeof(UserProfile)), "user_id", "user_id");
                query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_country"), new DataField("countries", "country_iso"));
                query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_religion"), new DataField("religions", "religion_id"));
                query.AddCondition(new DataField("user_keys", "user_name_lower"), ConditionEquality.In, usernameList);

                System.Data.Common.DbDataReader usersReader = db.ReaderQuery(query);

                while(usersReader.Read())
                {
                    User newUser = new User(core, usersReader, UserLoadOptions.All);
                    // This will automatically cache itself when loadUser is called

                    PrimitiveId pid = new PrimitiveId(newUser.Id, userTypeId);
                    PrimitiveKey kid = new PrimitiveKey(newUser.UserName.ToLower(), userTypeId);
					
					if (!userIds.ContainsValue(newUser.Id))
					{
						userIds.Add(newUser.UserName, newUser.Id);
					}
                }

                usersReader.Close();
                usersReader.Dispose();
            }

            return userIds;
        }

        public long LoadUserProfile(string username)
        {
            PrimitiveKey key = new PrimitiveKey(username, ItemKey.GetTypeId(typeof(User)));
            PrimitiveId pid = null;
            if (primitivesKeysCached.TryGetValue(key, out pid))
            {
                return pid.Id;
            }

            try
            {
                User newUser = new User(core, username, UserLoadOptions.All);
                // This will automatically cache itself when loadUser is called

                return newUser.Id;
            }
            catch (InvalidUserException)
            {
                return 0;
            }
        }
    }
}
