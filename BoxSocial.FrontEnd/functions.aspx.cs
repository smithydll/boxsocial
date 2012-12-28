﻿/*
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
using System.Reflection;
using System.Text;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.FrontEnd
{
    public partial class functions : TPage
    {
        public functions()
            : base("")
        {
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string function = core.Http["fun"];

            switch (function)
            {
                case "date":
                    string date = core.Functions.InterpretDate(core.Http.Form["date"]);
                    core.Ajax.SendRawText("date", date);
                    return;
                case "time":
                    string time = core.Functions.InterpretTime(core.Http.Form["time"]);
                    core.Ajax.SendRawText("time", time);
                    return;
                case "friend-list":
                    ReturnFriendList();
                    return;
                case "permission-groups-list":
                    ReturnPermissionGroupList();
                    return;
            }
        }

        private void ReturnFriendList()
        {
            string namePart = core.Http.Form["name-field"];

            if (core.Session.IsLoggedIn)
            {
                List<Friend> friends = core.Session.LoggedInMember.GetFriends(namePart);

                Dictionary<long, string[]> friendNames = new Dictionary<long, string[]>();

                foreach (Friend friend in friends)
                {
                    friendNames.Add(friend.Id, new string[] { friend.DisplayName, friend.UserTile });
                }

                core.Ajax.SendUserDictionary("friendSelect", friendNames);
            }
        }

        private void ReturnPermissionGroupList()
        {
            string namePart = core.Http.Form["name-field"];
            long itemId = core.Functions.FormLong("item", 0);
            long itemTypeId = core.Functions.FormLong("type", 0);

            if (itemId > 0 && itemTypeId > 0)
            {
                ItemKey ik = new ItemKey(itemId, itemTypeId);

                List<PrimitivePermissionGroup> groups = null;
                NumberedItem ni = NumberedItem.Reflect(core, ik);
                Primitive primitive = null;
                Dictionary<ItemKey, string[]> permissiveNames = new Dictionary<ItemKey, string[]>();

                if (ni.GetType().IsSubclassOf(typeof(Primitive)))
                {
                    primitive = (Primitive)ni;
                }
                else
                {
                    primitive = ((IPermissibleItem)ni).Owner;
                }

                groups = new List<PrimitivePermissionGroup>();
                int itemGroups = 0;

                Type type = ni.GetType();
                if (type.GetMethod(type.Name + "_GetItemGroups", new Type[] { typeof(Core) }) != null)
                {
                    groups.AddRange((List<PrimitivePermissionGroup>)type.InvokeMember(type.Name + "_GetItemGroups", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, new object[] { core }));
                    itemGroups = groups.Count;
                }

                groups.AddRange(core.GetPrimitivePermissionGroups(primitive));

                foreach (PrimitivePermissionGroup group in groups)
                {
                    if (!string.IsNullOrEmpty(group.LanguageKey))
                    {
                        permissiveNames.Add(group.ItemKey, new string[] { core.Prose.GetString(group.LanguageKey), string.Empty });
                    }
                    else
                    {
                        permissiveNames.Add(group.ItemKey, new string[] { group.DisplayName, string.Empty });
                    }
                }

                List<User> friends = primitive.GetPermissionUsers(namePart);

                foreach (User friend in friends)
                {
                    permissiveNames.Add(friend.ItemKey, new string[] { friend.DisplayName, friend.UserTile });
                }

                core.Ajax.SendPermissionGroupDictionary("permissionSelect", permissiveNames);

                /*if (core.Session.IsLoggedIn)
                {
                    List<Friend> friends = core.Session.LoggedInMember.GetFriends(namePart);

                    Dictionary<long, string[]> friendNames = new Dictionary<long, string[]>();

                    foreach (Friend friend in friends)
                    {
                        friendNames.Add(friend.Id, new string[] { friend.DisplayName, friend.UserTile });
                    }

                    core.Ajax.SendUserDictionary("friendSelect", friendNames);
                }*/
            }
        }
    }
}
