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
using System.Data;
using System.Configuration;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    /// <summary>
    /// Summary description for Access
    /// </summary>
    public class Access
    {
        public const ushort ALL_CAN_READ = 0x1111;
        public const ushort FRIENDS_CAN_READ = 0x1000;

        public static bool AllCanRead(ushort permissions)
        {
            if ((permissions & Access.ALL_CAN_READ) == Access.ALL_CAN_READ)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool FriendsCanRead(ushort permissions)
        {
            if ((permissions & Access.FRIENDS_CAN_READ) == Access.FRIENDS_CAN_READ)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private Core core;
        private Mysql db;
        private bool canRead = false;
        private bool canComment = false;
        private bool canCreate = false;
        private bool canChange = false;

        private Primitive owner;
        private User viewer;

        private ushort accessBits;

        public Access(Core core, ushort accessBits, Primitive owner)
        {
            this.core = core;
            this.db = core.db;
            this.owner = owner;

            this.accessBits = accessBits;
        }

        public long SetViewer(User viewer)
        {
            this.viewer = viewer;

            long loggedIdUid = 0;
            if (viewer != null)
            {
                loggedIdUid = viewer.UserId;
            }

            owner.GetCan(accessBits, viewer, out canRead, out canComment, out canCreate, out canChange);

            return loggedIdUid;
        }

        public long SetSessionViewer(SessionState session)
        {
            long loggedIdUid = User.GetMemberId(session.LoggedInMember);

            SetViewer(session.LoggedInMember);

            return loggedIdUid;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool CanRead
        {
            get
            {
                return canRead;
            }
        }

        public bool CanComment
        {
            get
            {
                return canComment;
            }
        }
    }
}