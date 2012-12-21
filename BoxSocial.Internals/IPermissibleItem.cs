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
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public interface IPermissibleItem
    {

        /// <summary>
        /// Access Object
        /// </summary>
        Access Access
        {
            get;
        }

        /// <summary>
        /// Owner of the item
        /// </summary>
        Primitive Owner
        {
            get;
        }

        /// <summary>
        /// List of actions that require elevated permissions for an item
        /// </summary>
        List<AccessControlPermission> AclPermissions
        {
            get;
        }

        bool IsItemGroupMember(User viewer, ItemKey key);

        long Id
        {
            get;
        }

        ItemKey ItemKey
        {
            get;
        }

        string Namespace
        {
            get;
        }

        string Uri
        {
            get;
        }

        IPermissibleItem PermissiveParent
        {
            get;
        }

        ItemKey PermissiveParentKey
        {
            get;
        }

        string DisplayTitle
        {
            get;
        }

        /// <summary>
        /// When all fall throughs to parent levels fail, it uses the default
        /// </summary>
        /// <returns></returns>
        bool GetDefaultCan(string permission);

        string ParentPermissionKey(Type parentType, string permission);
    }
}
