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

namespace BoxSocial.Groups
{
    [DataTable("group_invites")]
    public class GroupInvite : Item
    {
        [DataField("group_id", typeof(UserGroup))]
        private long groupId;
        [DataField("user_id")]
        private long userId;
        [DataField("inviter_id")]
        private long inviterId; // User
        [DataField("invite_date_ut")]
        private long inviteTimeRaw;

        public long GroupId
        {
            get
            {
                return groupId;
            }
        }

        public long InviteeId
        {
            get
            {
                return userId;
            }
        }

        public long InviterId
        {
            get
            {
                return inviterId;
            }
        }

        public long InvitedTimeRaw
        {
            get
            {
                return inviteTimeRaw;
            }
        }

        public DateTime GetInvitedTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(inviteTimeRaw);
        }

        public GroupInvite(Core core, DataRow inviteRow)
            : base(core)
        {
            this.ItemLoad += new ItemLoadHandler(GroupInvite_ItemLoad);

            try
            {
                loadItemInfo(inviteRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidEventInviteException();
            }
        }

        void GroupInvite_ItemLoad()
        {
        }

        public GroupInvite Create(Core core, UserGroup group, User invitee)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            core.Db.BeginTransaction();

            InsertQuery iQuery = new InsertQuery(Table);
            iQuery.AddField("group_id", group.Id);
            iQuery.AddField("user_id", invitee.Id);
            iQuery.AddField("inviter_id", core.LoggedInMemberId);
            iQuery.AddField("invite_date_ut", UnixTime.UnixTimeStamp());

            SelectQuery query = GetSelectQueryStub();
            query.AddCondition("group_id", group.Id);
            query.AddCondition("user_id", invitee.Id);

            DataTable table = Query(query);

            if (table.Rows.Count == 1)
            {
                return new GroupInvite(core, table.Rows[0]);
            }
            else
            {
                core.Db.RollBackTransaction();
                throw new Exception("Cannot create a new GroupInvite");
            }
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }

    public class InvalidEventInviteException : Exception
    {
    }
}
