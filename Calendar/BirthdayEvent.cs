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

namespace BoxSocial.Applications.Calendar
{
    public class BirthdayEvent : Event
    {

        private User user;

        public User User
        {
            get
            {
                return user;
            }
        }

        public BirthdayEvent(Core core, User owner, User user, int year)
            : base(core)
        {
            this.owner = owner;
            this.user = user;

            if (!user.IsFriend(owner))
            {
                throw new InvalidEventException();
            }

            UnixTime tz = new UnixTime(core, user.TimeZoneCode);

            this.eventId = ~user.Id;
            this.subject = user.TitleNameOwnership + " birthday";
            this.description = string.Empty;
            this.views = 0;
            this.attendees = 0;
            this.permissions = 0;
            this.comments = 0;
            this.ownerKey = new ItemKey(owner.Id, owner.TypeId);
            this.userId = user.Id;
            this.startTimeRaw =  tz.GetUnixTimeStamp(new DateTime(year, user.DateOfBirth.Month, user.DateOfBirth.Day, 0, 0, 0));
            this.endTimeRaw = tz.GetUnixTimeStamp(new DateTime(year, user.DateOfBirth.Month, user.DateOfBirth.Day, 23, 59, 59));
            this.allDay = true;
            this.invitees = 0;
            this.category = 0;
            this.location = string.Empty;
        }

        public override string Uri
        {
            get
            {
                return user.Uri;
            }
        }
    }
}
