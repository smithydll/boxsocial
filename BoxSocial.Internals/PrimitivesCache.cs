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
        /// A cache of primitives loaded.
        /// </summary>
        private Dictionary<PrimitiveId, Primitive> primitivesCached = new Dictionary<PrimitiveId, Primitive>();

        /// <summary>
        /// A cached primitive keys.
        /// </summary>
        private Dictionary<PrimitiveKey, PrimitiveId> primitivesKeysCached = new Dictionary<PrimitiveKey, PrimitiveId>();

        /// <summary>
        /// A cached user Ids.
        /// </summary>
        private Dictionary<string, long> userIdsCached = new Dictionary<string, long>(StringComparer.Ordinal);

        /// <summary>
        /// A list of primitive Ids for batched loading
        /// </summary>
        private List<PrimitiveId> batchedPrimitivesIds = new List<PrimitiveId>();

        public User this[long key]
        {
            get
            {
                /*loadBatchedIds(ItemKey.GetTypeId(typeof(User)), key);
                return (User)primitivesCached[new PrimitiveId(key, ItemKey.GetTypeId(typeof(User)))];*/
                return (User)core.ItemCache[new ItemKey(key, typeof(User))];
            }
        }

        public Primitive this[ItemKey key]
        {
            get
            {
                /*try
                {
                    loadBatchedIds(key.TypeId, key.Id);
                    return primitivesCached[new PrimitiveId(key.Id, key.TypeId)];
                }
                catch (KeyNotFoundException ex)
                {
                    throw new Exception(string.Format("Something went terribly wrong with {0}\n{1}\n\n{2}", key.ToString(), ex.ToString(), core.Db.QueryListToString()));
                }*/
                return (Primitive)core.ItemCache[key];
            }
        }

        public Primitive this[long key, long typeId]
        {
            get
            {
                /*loadBatchedIds(typeId, key);
                return primitivesCached[new PrimitiveId(key, typeId)];*/
                return (Primitive)core.ItemCache[new ItemKey(key, typeId)];
            }
        }

        public void LoadUserProfiles(List<long> userIds)
        {
            foreach (long userId in userIds)
            {
                //batchedPrimitivesIds.Add(new PrimitiveId(userId, ItemKey.GetTypeId(typeof(User))));
                core.ItemCache.RequestItem(new ItemKey(userId, typeof(User)));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="primitiveIds"></param>
        private void loadPrimitiveProfiles(List<PrimitiveId> primitiveIds)
        {
            Dictionary<long, List<long>> idList = new Dictionary<long, List<long>>();
            foreach (PrimitiveId id in primitiveIds)
            {
                if (id.Id > 0)
                {
                    if (!primitivesCached.ContainsKey(id))
                    {
                        if (!idList.ContainsKey(id.TypeId))
                        {
                            idList.Add(id.TypeId, new List<long>());
                        }
                        idList[id.TypeId].Add(id.Id);
                    }
                }
            }

            foreach (long typeId in idList.Keys)
            {
                if (idList[typeId].Count > 0)
                {
                    Type t = core.GetPrimitiveType(typeId);
                    if (t != null)
                    {
                        string keysTable = Primitive.GetTable(t);
                        PrimitiveAttribute attr = core.GetPrimitiveAttributes(typeId);
                        if (attr == null)
                        {
                            continue;
                        }
                        string idField = attr.IdField;

                        SelectQuery query = Primitive.GetSelectQueryStub(t);
                        query.AddCondition(string.Format("`{0}`.`{1}`", keysTable, idField), ConditionEquality.In, idList[typeId]);

                        DataTable primitivesTable = db.Query(query);

                        foreach (DataRow primitiveRow in primitivesTable.Rows)
                        {
                            Primitive newPrimitive = System.Activator.CreateInstance(t, new object[] { core, primitiveRow, core.GetPrimitiveAttributes(typeId).DefaultLoadOptions }) as Primitive;
                            primitivesCached.Add(new PrimitiveId(newPrimitive.Id, typeId), newPrimitive);
                            primitivesKeysCached.Add(new PrimitiveKey(newPrimitive.Key, typeId), new PrimitiveId(newPrimitive.Id, typeId));
                        }
                    }
                }
            }
        }

        public void LoadUserProfile(long userId)
        {
            /*PrimitiveId key = new PrimitiveId(userId, ItemKey.GetTypeId(typeof(User)));
            if ((!primitivesCached.ContainsKey(key)) && (!batchedPrimitivesIds.Contains(key)))
            {
                batchedPrimitivesIds.Add(key);
            }*/
            core.ItemCache.RequestItem(new ItemKey(userId, typeof(User)));
        }

        public void LoadPrimitiveProfile(ItemKey key)
        {
            /*if (!primitivesCached.ContainsKey(new PrimitiveId(key.Id, key.TypeId)))
            {
                batchedPrimitivesIds.Add(new PrimitiveId(key.Id, key.TypeId));
            }*/
            core.ItemCache.RequestItem(key);
        }

        public void LoadPrimitiveProfile(long id, long typeId)
        {
            /*if (!primitivesCached.ContainsKey(new PrimitiveId(id, typeId)))
            {
                batchedPrimitivesIds.Add(new PrimitiveId(id, typeId));
            }*/
            core.ItemCache.RequestItem(new ItemKey(id, typeId));
        }

        public void LoadPrimitiveProfiles(List<ItemKey> itemkeys)
        {
            foreach (ItemKey ik in itemkeys)
            {
                /*if (ik.InheritsPrimitive)
                {
                    if (!primitivesCached.ContainsKey(new PrimitiveId(ik.Id, ik.TypeId)))
                    {
                        batchedPrimitivesIds.Add(new PrimitiveId(ik.Id, ik.TypeId));
                    }
                }*/
                core.ItemCache.RequestItem(ik);
            }
        }

        public Dictionary<string, long> LoadUserProfiles(List<string> usernames)
        {
            long userTypeId = ItemKey.GetTypeId(typeof(User));

            List<string> usernameList = new List<string>();
            Dictionary<string, long> userIds = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (string username in usernames)
            {
                PrimitiveKey key = new PrimitiveKey(username.ToLower(), userTypeId);
                if (!primitivesKeysCached.ContainsKey(key))
                {
                    usernameList.Add(username.ToLower());
                }
                else
                {
                    if (!userIdsCached.ContainsKey(username.ToLower()))
                    {
                        usernameList.Add(username.ToLower());
                    }
                    else
                    {
                        userIds.Add(username.ToLower(), userIdsCached[username.ToLower()]);
                    }
                }
            }

            if (usernameList.Count > 0)
            {
                SelectQuery query = new SelectQuery("user_keys");
                query.AddFields(User.GetFieldsPrefixed(typeof(User)));
                query.AddFields(UserInfo.GetFieldsPrefixed(typeof(UserInfo)));
                query.AddFields(UserProfile.GetFieldsPrefixed(typeof(UserProfile)));
                query.AddJoin(JoinTypes.Inner, UserInfo.GetTable(typeof(UserInfo)), "user_id", "user_id");
                query.AddJoin(JoinTypes.Inner, UserProfile.GetTable(typeof(UserProfile)), "user_id", "user_id");
                query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_country"), new DataField("countries", "country_iso"));
                query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_religion"), new DataField("religions", "religion_id"));
                query.AddCondition("LCASE(`user_keys`.`user_name`)", ConditionEquality.In, usernameList);

                DataTable usersTable = db.Query(query);

                foreach (DataRow userRow in usersTable.Rows)
                {
                    User newUser = new User(core, userRow, UserLoadOptions.All);

                    PrimitiveId pid = new PrimitiveId(newUser.Id, userTypeId);
                    PrimitiveKey kid = new PrimitiveKey(newUser.UserName.ToLower(), userTypeId);
					
					if (!primitivesCached.ContainsKey(pid))
					{
						primitivesCached.Add(pid, newUser);
					}
					if (!primitivesKeysCached.ContainsKey(kid))
					{
                        primitivesKeysCached.Add(kid, new PrimitiveId(newUser.Id, userTypeId));
					}
					if (!userIds.ContainsValue(newUser.Id))
					{
						userIds.Add(newUser.UserName, newUser.Id);
					}
                    if (!userIdsCached.ContainsKey(newUser.UserName.ToLower()))
                    {
                        userIdsCached.Add(newUser.UserName.ToLower(), newUser.Id);
                    }
                }
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
                PrimitiveId id = new PrimitiveId(newUser.Id, ItemKey.GetTypeId(typeof(User)));
                if (!primitivesCached.ContainsKey(id))
                {
                    primitivesCached.Add(id, newUser);
                }

                return newUser.Id;
            }
            catch (InvalidUserException)
            {
                return 0;
            }
        }

        private void loadBatchedIds(long typeId, long requestedId)
        {
            if (batchedPrimitivesIds.Contains(new PrimitiveId(requestedId, typeId)))
            {
                loadPrimitiveProfiles(batchedPrimitivesIds);
            }
        }
    }
}
