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
using System.Configuration;
using System.Data;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    /// <summary>
    /// Summary description for Access
    /// </summary>
    public class Access
    {
        private Core core;
        private Mysql db;

        private Primitive owner;
        private User viewer;

        private List<AccessControlGrant> grants;
        private NumberedItem item;

        public Access(Core core, IPermissibleItem item, Primitive owner)
        {
            this.core = core;
            this.item = (NumberedItem)item;

            grants = AccessControlGrant.GetGrants(core, this.item);
        }

        public long SetViewer(User viewer)
        {
            this.viewer = viewer;

            long loggedIdUid = 0;
            if (viewer != null)
            {
                loggedIdUid = viewer.UserId;
            }

            return loggedIdUid;
        }

        public long SetSessionViewer(SessionState session)
        {
            long loggedIdUid = User.GetMemberId(session.LoggedInMember);

            SetViewer(session.LoggedInMember);

            return loggedIdUid;
        }

        public bool Can(string permission)
        {
            bool allow = false;
            bool deny = false;

            AccessControlPermission acp = new AccessControlPermission(core, item.ItemKey.TypeId, permission);

            if (grants.Count == 0)
            {
                if (item == owner)
                {
                    if (owner == viewer)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return owner.Access.Can(permission);
                }
            }

            /*foreach (AccessControlGrant grant in grants)
            {
                if (grant.PermissionId > 0 && grant.PermissionId == acp.Id)
                {
                    core.UserProfiles.LoadPrimitiveProfile(grant.PrimitiveKey);
                }
            }*/

            foreach (AccessControlGrant grant in grants)
            {
                if (grant.PermissionId > 0 && grant.PermissionId == acp.Id)
                {
                    /*if (owner.GetIsMemberOfPrimitive(grant.PrimitiveKey))
                    {
                    }
                    switch (grant.Allow)
                    {
                        case AccessControlGrants.Allow:
                            allow = true;
                            break;
                        case AccessControlGrants.Deny:
                            deny = true;
                            break;
                        case AccessControlGrants.Inherit:
                            break;
                    }*/
                }
            }

            return (allow && (!deny));
        }
    }
}
