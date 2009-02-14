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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Calendar
{
    public enum EventAttendance : byte
    {
        Unknown = 0,
        Yes = 1,
        Maybe = 2,
        No = 3,
    }

    [DataTable("event_invites")]
    public class EventInvite : Item
    {
        [DataField("event_id", typeof(Event))]
        private long eventId;
        [DataField("item_id")]
        private long itemId;
        [DataField("item_type", NAMESPACE)]
        private string itemType;
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

        public EventInvite(Core core, DataRow inviteRow)
            : base(core)
        {
            this.ItemLoad += new ItemLoadHandler(EventInvite_ItemLoad);

            try
            {
                loadItemInfo(inviteRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidEventInviteException();
            }
        }

        void EventInvite_ItemLoad()
        {
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