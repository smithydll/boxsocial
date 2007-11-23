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
    public class Event
    {
        public const string EVENT_INFO_FIELDS = "ev.event_id, ev.event_subject, ev.event_description, ev.event_views, ev.event_attendies, ev.event_access, ev.event_comments, ev.event_item_id, ev.event_item_type, ev.user_id, ev.event_time_start_ut, ev.event_time_end_ut, ev.event_all_day, ev.event_invitees, ev.event_category, ev.event_location";

        private Mysql db;

        private long eventId;
        private string subject;
        private string description;
        private long views;
        private long attendies;
        private ushort permissions;
        private Access eventAccess;
        private long comments;
        private long ownerId;
        private Primitive owner;
        private int userId; // creator
        private long startTimeRaw;
        private long endTimeRaw;
        private bool allDay;
        private long invitees;
        private ushort category;
        private string location;

        public long EventId
        {
            get
            {
                return eventId;
            }
        }

        public string Subject
        {
            get
            {
                return subject;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
        }

        public long Views
        {
            get
            {
                return views;
            }
        }

        public long Attendies
        {
            get
            {
                return attendies;
            }
        }

        public ushort Permissions
        {
            get
            {
                return permissions;
            }
        }

        public Access EventAccess
        {
            get
            {
                return eventAccess;
            }
        }

        public long Comments
        {
            get
            {
                return comments;
            }
        }

        public int UserId
        {
            get
            {
                return userId;
            }
        }

        public bool AllDay
        {
            get
            {
                return allDay;
            }
        }

        public long Invitees
        {
            get
            {
                return invitees;
            }
        }

        public string Location
        {
            get
            {
                return location;
            }
        }

        public long StartTimeRaw
        {
            get
            {
                return startTimeRaw;
            }
        }

        public long EndTimeRaw
        {
            get
            {
                return endTimeRaw;
            }
        }

        public DateTime GetStartTime(Internals.TimeZone tz)
        {
            return tz.DateTimeFromMysql(startTimeRaw);
        }

        public DateTime GetEndTime(Internals.TimeZone tz)
        {
            return tz.DateTimeFromMysql(endTimeRaw);
        }

        public Event(Mysql db, Primitive owner, long eventId)
        {
            this.db = db;
            this.owner = owner;

            DataTable eventsTable = db.SelectQuery(string.Format("SELECT {0} FROM events ev WHERE ev.user_id = {1} AND ev.event_id = {2};",
                Event.EVENT_INFO_FIELDS, owner.Id, eventId));

            if (eventsTable.Rows.Count == 1)
            {
                loadEventInfo(eventsTable.Rows[0]);
            }
            else
            {
                throw new Exception("Invalid Event Exception");
            }
        }

        public Event(Mysql db, Primitive owner, DataRow eventRow)
        {
            this.db = db;
            this.owner = owner;

            loadEventInfo(eventRow);
        }

        private void loadEventInfo(DataRow eventRow)
        {
            eventId = (long)eventRow["event_id"];
            subject = (string)eventRow["event_subject"];
            if (!(eventRow["event_description"] is DBNull))
            {
                description = (string)eventRow["event_description"];
            }
            views = (long)eventRow["event_views"];
            attendies = (long)eventRow["event_attendies"];
            permissions = (ushort)eventRow["event_access"];
            comments = (long)eventRow["event_comments"];
            // ownerId
            userId = (int)eventRow["user_id"];
            startTimeRaw = (long)eventRow["event_time_start_ut"];
            endTimeRaw = (long)eventRow["event_time_end_ut"];
            // allDay
            invitees = (long)eventRow["event_invitees"];
            // category
            location = (string)eventRow["event_location"];

            eventAccess = new Access(db, permissions, owner);
        }

        public static Event Create(Mysql db, Member creator, Primitive owner, string subject, string location, string description, long startTimestamp, long endTimestamp, ushort permissions)
        {
            long eventId = db.UpdateQuery(string.Format("INSERT INTO events (user_id, event_item_id, event_item_type, event_subject, event_location, event_description, event_time_start_ut, event_time_end_ut, event_access) VALUES ({0}, {1}, '{2}', '{3}', '{4}', '{5}', {6}, {7}, {8})",
                creator.UserId, owner.Id, Mysql.Escape(owner.Type),Mysql.Escape(subject), Mysql.Escape(location), Mysql.Escape(description), startTimestamp, endTimestamp, permissions));

            return new Event(db, owner, eventId);
        }

        public static string BuildEventUri(Event calendarEvent)
        {
            return ZzUri.AppendSid(string.Format("{0}/calendar/event/{1}",
                calendarEvent.owner.Uri, calendarEvent.EventId));
        }

        public static void Show(Core core, Primitive owner, long eventId)
        {
            core.template.SetTemplate("viewcalendarevent.html");

            try
            {
                Event calendarEvent = new Event(core.db, owner, eventId);

                calendarEvent.EventAccess.SetSessionViewer(core.session);

                if (!calendarEvent.EventAccess.CanRead)
                {
                    Functions.Generate404(core);
                    return;
                }

                core.template.ParseVariables("SUBJECT", HttpUtility.HtmlEncode(calendarEvent.Subject));
                core.template.ParseVariables("LOCATION", HttpUtility.HtmlEncode(calendarEvent.Location));
                core.template.ParseVariables("DESCRIPTION", HttpUtility.HtmlEncode(calendarEvent.Description));
                core.template.ParseVariables("START_TIME", HttpUtility.HtmlEncode(calendarEvent.GetStartTime(core.tz).ToString()));
                core.template.ParseVariables("END_TIME", HttpUtility.HtmlEncode(calendarEvent.GetEndTime(core.tz).ToString()));
            }
            catch
            {
                Display.ShowMessage(core, "Invalid submission", "You have made an invalid form submission.");
            }
        }
    }
}
