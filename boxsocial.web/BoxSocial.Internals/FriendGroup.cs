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
using System.Configuration;
using System.Data;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("friend_groups")]
    public class FriendGroup : NumberedItem
    {
        [DataField("friend_group_id", DataFieldKeys.Primary)]
        private long groupId;
        [DataField("friend_group_title", 32)]
        private string groupTitle;
        [DataField("friend_group_friends")]
        private long friendsCount;

        private User owner;

        public FriendGroup(Core core, long groupId)
            : base(core)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            ItemLoad += new ItemLoadHandler(FriendGroup_ItemLoad);

            try
            {
                LoadItem(groupId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidFriendGroupException();
            }
        }

        public FriendGroup(Core core, User owner, DataRow friendGroupRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(FriendGroup_ItemLoad);
            this.owner = owner;

            loadItemInfo(friendGroupRow);
        }

        void FriendGroup_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return groupId;
            }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class InvalidFriendGroupException : Exception
    {
    }
}
