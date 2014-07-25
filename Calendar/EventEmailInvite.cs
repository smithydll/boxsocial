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
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Calendar
{
    [DataTable("event_email_invites")]
    public class EventEmailInvite : Item
    {
        [DataField("event_id", typeof(Event))]
        private long eventId;
        [DataField("invite_email", 127)]
        private string inviteEmail;
        [DataField("invite_key", 32)]
        private string inviteEmailKey;
        [DataField("inviter_id")]
        private long inviterId; // User
        [DataField("invite_date_ut")]
        private long inviteTimeRaw;
        [DataField("invite_accepted")]
        private bool inviteAccepted;
        [DataField("invite_status")]
        private byte inviteStatus;

        public long EventId
        {
            get
            {
                return eventId;
            }
        }

        public string Email
        {
            get
            {
                return inviteEmail;
            }
        }

        public string EmailKey
        {
            get
            {
                return inviteEmailKey;
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

        public bool InviteAccepted
        {
            get
            {
                return inviteAccepted;
            }
        }

        public EventAttendance InviteStatus
        {
            get
            {
                return (EventAttendance)inviteStatus;
            }
            set
            {
                InviteStatusRaw = (byte)value;
            }
        }

        public byte InviteStatusRaw
        {
            get
            {
                return inviteStatus;
            }
            set
            {
                SetProperty("inviteStatus", value);
            }
        }

        public DateTime GetInvitedTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(inviteTimeRaw);
        }

        public EventEmailInvite(Core core, DataRow eventInviteDataRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(EventEmailInvite_ItemLoad);

            loadItemInfo(eventInviteDataRow);
        }

        public EventEmailInvite(Core core, System.Data.Common.DbDataReader eventInviteDataRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(EventEmailInvite_ItemLoad);

            loadItemInfo(eventInviteDataRow);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader eventRow)
        {
            loadValue(eventRow, "event_id", out eventId);
            loadValue(eventRow, "invite_email", out inviteEmail);
            loadValue(eventRow, "invite_key", out inviteEmailKey);
            loadValue(eventRow, "inviter_id", out inviterId);
            loadValue(eventRow, "invite_date_ut", out inviteTimeRaw);
            loadValue(eventRow, "invite_accepted", out inviteAccepted);
            loadValue(eventRow, "invite_status", out inviteStatus);

            itemLoaded(eventRow);
        }

        void EventEmailInvite_ItemLoad()
        {
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }
}
