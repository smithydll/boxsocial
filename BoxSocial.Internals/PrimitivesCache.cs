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
    public sealed class PrimitivesCache
    {
        private Core core;
        private Mysql db;

        public PrimitivesCache(Core core)
        {
            this.core = core;
            this.db = core.db;
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
        /// A list of primitive Ids for batched loading
        /// </summary>
        private List<PrimitiveId> batchedPrimitivesIds = new List<PrimitiveId>();

        public User this[long key]
        {
            get
            {
                loadBatchedIds("USER", key);
                return (User)primitivesCached[new PrimitiveId("USER", key)];
            }
        }

        public Primitive this[string type, long key]
        {
            get
            {
                loadBatchedIds(type, key);
                return primitivesCached[new PrimitiveId(type, key)];
            }
        }

        public void LoadUserProfiles(List<long> userIds)
        {
            foreach (long userId in userIds)
            {
                batchedPrimitivesIds.Add(new PrimitiveId("USER", userId));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="primitiveIds"></param>
        private void loadPrimitiveProfiles(List<PrimitiveId> primitiveIds)
        {
            Dictionary<string, List<long>> idList = new Dictionary<string, List<long>>();
            foreach (PrimitiveId id in primitiveIds)
            {
                if (!primitivesCached.ContainsKey(id))
                {
                    if (!idList.ContainsKey(id.Type))
                    {
                        idList.Add(id.Type, new List<long>());
                    }
                    idList[id.Type].Add(id.Id);
                }
            }

            foreach (String type in idList.Keys)
            {
                Type t = core.GetPrimitiveType(type);
                if (t != null)
                {
                    string keysTable = Primitive.GetTable(t);
                    string idField = core.GetPrimitiveAttributes(type).IdField;

                    SelectQuery query = Primitive.GetSelectQueryStub(t);
                    query.AddCondition(string.Format("`{0}`.`{1}`", keysTable, idField), ConditionEquality.In, idList[type]);

                    DataTable primitivesTable = db.Query(query);

                    foreach (DataRow primitiveRow in primitivesTable.Rows)
                    {
                        Primitive newPrimitive = System.Activator.CreateInstance(t, new object[] { core, primitiveRow, core.GetPrimitiveAttributes(type).DefaultLoadOptions }) as Primitive;
                        primitivesCached.Add(new PrimitiveId(type, newPrimitive.Id), newPrimitive);
                        primitivesKeysCached.Add(new PrimitiveKey(type, newPrimitive.Key), new PrimitiveId(type, newPrimitive.Id));
                    }
                }
            }
        }

        public void LoadUserProfile(long userId)
        {
            PrimitiveId key = new PrimitiveId("USER", userId);
            if ((!primitivesCached.ContainsKey(key)) && (!batchedPrimitivesIds.Contains(key)))
            {
                batchedPrimitivesIds.Add(key);
            }
        }

        public void LoadPrimitiveProfile(string type, long id)
        {
            if (!primitivesCached.ContainsKey(new PrimitiveId(type, id)))
            {
                batchedPrimitivesIds.Add(new PrimitiveId(type, id));
            }
        }

        public List<long> LoadUserProfiles(List<string> usernames)
        {
            List<string> usernameList = new List<string>();
            List<long> userIds = new List<long>();
            foreach (string username in usernames)
            {
                PrimitiveKey key = new PrimitiveKey("USER", username);
                if (!primitivesKeysCached.ContainsKey(key))
                {
                    usernameList.Add(username);
                }
            }

            if (usernameList.Count > 0)
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
                query.AddCondition("`user_keys`.`user_name`", ConditionEquality.In, usernameList);

                DataTable usersTable = db.Query(query);

                foreach (DataRow userRow in usersTable.Rows)
                {
                    User newUser = new User(core, userRow, UserLoadOptions.All);
                    primitivesCached.Add(new PrimitiveId("USER", newUser.Id), newUser);
                    primitivesKeysCached.Add(new PrimitiveKey("USER", newUser.UserName), new PrimitiveId("USER", newUser.Id));
                    userIds.Add(newUser.Id);
                }
            }

            return userIds;
        }

        public long LoadUserProfile(string username)
        {
            PrimitiveKey key = new PrimitiveKey("USER", username);
            if (primitivesKeysCached.ContainsKey(key))
            {
                return primitivesKeysCached[key].Id;
            }

            User newUser = new User(core, username, UserLoadOptions.All);
            PrimitiveId id = new PrimitiveId("USER", newUser.Id);
            if (!primitivesCached.ContainsKey(id))
            {
                primitivesCached.Add(id, newUser);
            }

            return newUser.Id;
        }

        private void loadBatchedIds(string type, long requestedId)
        {
            if (batchedPrimitivesIds.Contains(new PrimitiveId(type, requestedId)))
            {
                loadPrimitiveProfiles(batchedPrimitivesIds);
            }
        }
    }
}
