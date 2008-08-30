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
    public enum EventAttendance : byte
    {
        Unknown = 0,
        Yes = 1,
        Maybe = 2,
        No = 3,
    }

    [DataTable("events")]
    public class Event : NumberedItem, ICommentableItem, IPermissibleItem, IComparable
    {
        public const string EVENT_INFO_FIELDS = "ev.event_id, ev.event_subject, ev.event_description, ev.event_views, ev.event_attendees, ev.event_access, ev.event_comments, ev.event_item_id, ev.event_item_type, ev.user_id, ev.event_time_start_ut, ev.event_time_end_ut, ev.event_all_day, ev.event_invitees, ev.event_category, ev.event_location";

        #region Data Fields
        [DataField("event_id", DataFieldKeys.Primary)]
        protected long eventId;
        [DataField("event_subject", 127)]
        protected string subject;
        [DataField("event_description", MYSQL_TEXT)]
        protected string description;
        [DataField("event_views")]
        protected long views;
        [DataField("event_attendees")]
        protected long attendees;
        [DataField("event_access")]
        protected ushort permissions;
        [DataField("event_comments")]
        protected long comments;
        [DataField("event_item_id")]
        protected long ownerId;
        [DataField("event_item_type", 63)]
        protected string ownerType;
        [DataField("user_id")]
        protected long userId; // creator
        [DataField("event_time_start_ut")]
        protected long startTimeRaw;
        [DataField("event_time_end_ut")]
        protected long endTimeRaw;
        [DataField("event_all_day")]
        protected bool allDay;
        [DataField("event_invitees")]
        protected long invitees;
        [DataField("event_category")]
        protected ushort category;
        [DataField("event_location", 127)]
        protected string location;
        #endregion

        protected Access eventAccess;
        protected Primitive owner;

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
            set
            {
                SetProperty("subject", value);
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                SetProperty("description", value);
            }
        }

        public long Views
        {
            get
            {
                return views;
            }
        }

        public long Attendees
        {
            get
            {
                return attendees;
            }
        }

        public ushort Permissions
        {
            get
            {
                return permissions;
            }
            set
            {
                SetProperty("permissions", value);
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

        public long UserId
        {
            get
            {
                return userId;
            }
        }

        public Primitive Owner
        {
            get
            {
                return owner;
            }
        }

        public bool AllDay
        {
            get
            {
                return allDay;
            }
            set
            {
                SetProperty("allDay", value);
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
            set
            {
                SetProperty("location", value);
            }
        }

        public long StartTimeRaw
        {
            get
            {
                return startTimeRaw;
            }
            set
            {
                SetProperty("startTimeRaw", value);
            }
        }

        public long EndTimeRaw
        {
            get
            {
                return endTimeRaw;
            }
            set
            {
                SetProperty("endTimeRaw", value);
            }
        }

        public DateTime GetStartTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(startTimeRaw);
        }

        public DateTime GetEndTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(endTimeRaw);
        }

        protected Event(Core core)
            : base(core)
        {
        }

        public Event(Core core, Primitive owner, long eventId)
            : base(core)
        {
            this.owner = owner;
            ItemLoad += new ItemLoadHandler(Event_ItemLoad);

            try
            {
                LoadItem(eventId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidEventException();
            }
        }

        public Event(Core core, Primitive owner, DataRow eventRow)
            : base(core)
        {
            this.owner = owner;
            ItemLoad += new ItemLoadHandler(Event_ItemLoad);

            loadItemInfo(eventRow);
        }

        private void Event_ItemLoad()
        {
            if (owner == null || ownerId != owner.Id)
            {
                owner = new User(core, userId);
            }

            eventAccess = new Access(core, permissions, owner);
        }

        public static Event Create(Core core, User creator, Primitive owner, string subject, string location, string description, long startTimestamp, long endTimestamp, ushort permissions)
        {
            long eventId = core.db.UpdateQuery(string.Format("INSERT INTO events (user_id, event_item_id, event_item_type, event_subject, event_location, event_description, event_time_start_ut, event_time_end_ut, event_access) VALUES ({0}, {1}, '{2}', '{3}', '{4}', '{5}', {6}, {7}, {8})",
                creator.UserId, owner.Id, Mysql.Escape(owner.Type), Mysql.Escape(subject), Mysql.Escape(location), Mysql.Escape(description), startTimestamp, endTimestamp, permissions));

            Event myEvent = new Event(core, owner, eventId);

            if (Access.FriendsCanRead(myEvent.Permissions))
            {
                AppInfo.Entry.PublishToFeed(creator, "created a new event", string.Format("[iurl={0}]{1}[/iurl]",
                    Event.BuildEventUri(myEvent), myEvent.subject));
            }

            return myEvent;
        }

        public void Delete(Core core)
        {
            if (core.LoggedInMemberId == userId)
            {
                DeleteQuery dQuery = new DeleteQuery("events");
                dQuery.AddCondition("user_id", core.LoggedInMemberId);
                dQuery.AddCondition("event_id", EventId);

                db.BeginTransaction();
                db.Query(dQuery);

                dQuery = new DeleteQuery("event_invites");
                dQuery.AddCondition("event_id", EventId);

                if (db.Query(dQuery) < 0)
                {
                    throw new Exception();
                }
                else
                {
                    return;
                }
            }
            else
            {
                throw new NotLoggedInException();
            }
        }

        public void Invite(Core core, User invitee)
        {
            core.LoadUserProfile(userId);
            User user = core.UserProfiles[userId];
            // only the person who created the event can invite people to it
            if (core.LoggedInMemberId == userId)
            {
                // we can only invite people friends with us to an event
                if (invitee.IsFriend(user))
                {
                    InsertQuery iQuery = new InsertQuery("event_invites");
                    iQuery.AddField("event_id", EventId);
                    iQuery.AddField("item_id", invitee.Id);
                    iQuery.AddField("item_type", invitee.Type);
                    iQuery.AddField("inviter_id", userId);
                    iQuery.AddField("invite_date_ut", UnixTime.UnixTimeStamp());
                    iQuery.AddField("invite_accepted", false);
                    iQuery.AddField("invite_status", (byte)EventAttendance.Unknown);

                    long invitationId = db.Query(iQuery);

                    UpdateQuery uQuery = new UpdateQuery("events");
                    uQuery.AddField("event_invitees", new QueryOperation("event_invitees", QueryOperations.Addition, 1));
                    uQuery.AddCondition("event_id", EventId);

                    db.Query(uQuery);

                    RawTemplate emailTemplate = new RawTemplate(HttpContext.Current.Server.MapPath("./templates/emails/"), "event_invitation.eml");

                    emailTemplate.Parse("FROM_NAME", user.DisplayName);
                    emailTemplate.Parse("FROM_EMAIL", user.AlternateEmail);
                    emailTemplate.Parse("FROM_NAMES", user.DisplayNameOwnership);
                    emailTemplate.Parse("EVENT_SUBJECT", this.Subject);
                    emailTemplate.Parse("U_EVENT", "http://zinzam.com" + Linker.StripSid(Event.BuildEventUri(this)));
                    emailTemplate.Parse("U_ACCEPT", "http://zinzam.com" + Linker.StripSid(Event.BuildEventAcceptUri(core, this)));
                    emailTemplate.Parse("U_REJECT", "http://zinzam.com" + Linker.StripSid(Event.BuildEventRejectUri(core, this)));

                    AppInfo.Entry.SendNotification(invitee, string.Format("{0} has invited you to {1}.",
                        user.DisplayName, subject), string.Format("[iurl=\"{0}\" sid=true]Click Here[/iurl] accept the invitation.", Event.BuildEventAcceptUri(core, this)), emailTemplate);

                }
                else
                {
                    throw new Exception();
                }
            }
            else
            {
                throw new Exception();
            }
        }

        public void Invite(Core core, List<User> invitees)
        {
            core.LoadUserProfile(userId);
            User user = core.UserProfiles[userId];
            // only the person who created the event can invite people to it
            if (core.LoggedInMemberId == userId)
            {
                long friends = 0;
                foreach (User invitee in invitees)
                {
                    // we can only invite people friends with us to an event
                    if (invitee.IsFriend(user))
                    {
                        friends++;

                        InsertQuery iQuery = new InsertQuery("event_invites");
                        iQuery.AddField("event_id", EventId);
                        iQuery.AddField("item_id", invitee.Id);
                        iQuery.AddField("item_type", invitee.Type);
                        iQuery.AddField("inviter_id", userId);
                        iQuery.AddField("invite_date_ut", UnixTime.UnixTimeStamp());
                        iQuery.AddField("invite_accepted", false);
                        iQuery.AddField("invite_status", (byte)EventAttendance.Unknown);

                        long invitationId = db.Query(iQuery);

                        RawTemplate emailTemplate = new RawTemplate(HttpContext.Current.Server.MapPath("./templates/emails/"), "event_invitation.eml");

                        emailTemplate.Parse("FROM_NAME", user.DisplayName);
                        emailTemplate.Parse("FROM_EMAIL", user.AlternateEmail);
                        emailTemplate.Parse("FROM_NAMES", user.DisplayNameOwnership);
                        emailTemplate.Parse("EVENT_SUBJECT", this.Subject);
                        emailTemplate.Parse("U_EVENT", "http://zinzam.com" + Linker.StripSid(Event.BuildEventUri(this)));
                        emailTemplate.Parse("U_ACCEPT", "http://zinzam.com" + Linker.StripSid(Event.BuildEventAcceptUri(core, this)));
                        emailTemplate.Parse("U_REJECT", "http://zinzam.com" + Linker.StripSid(Event.BuildEventRejectUri(core, this)));

                        AppInfo.Entry.SendNotification(invitee, string.Format("{0} has invited you to {1}.",
                            user.DisplayName, subject), string.Format("[iurl=\"{0}\" sid=true]Click Here[/iurl] accept the invitation.", Event.BuildEventAcceptUri(core, this)), emailTemplate);

                    }
                    else
                    {
                        // ignore
                    }

                    UpdateQuery uQuery = new UpdateQuery("events");
                    uQuery.AddField("event_invitees", new QueryOperation("event_invitees", QueryOperations.Addition, friends));
                    uQuery.AddCondition("event_id", EventId);

                    db.Query(uQuery);
                }
            }
            else
            {
                throw new Exception();
            }
        }

        public List<long> GetInvitees()
        {
            List<long> ids = new List<long>();

            SelectQuery query = new SelectQuery("event_invites");
            query.AddFields("item_id", "item_type", "inviter_id", "event_id");
            query.AddCondition("event_id", EventId);

            DataTable invitees = db.Query(query);

            foreach (DataRow dr in invitees.Rows)
            {
                if ((string)dr["item_type"] == "USER")
                {
                    ids.Add((long)dr["item_id"]);
                }
            }

            return ids;
        }

        public bool IsInvitee(User member)
        {
            if (member != null)
            {
                SelectQuery query = new SelectQuery("event_invites");
                query.AddFields("item_id", "item_type", "inviter_id", "event_id");
                query.AddCondition("event_id", EventId);
                query.AddCondition("item_id", member.Id);
                query.AddCondition("item_type", member.Type);

                if (db.Query(query).Rows.Count > 0)
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
                return false;
            }
        }

        public List<long> GetAttendees()
        {
            List<long> ids = new List<long>();

            SelectQuery query = new SelectQuery("event_invites");
            query.AddFields("item_id", "item_type", "inviter_id", "event_id");
            query.AddCondition("event_id", EventId);
            query.AddCondition("invite_accepted", (byte)EventAttendance.Yes);

            DataTable invitees = db.Query(query);

            foreach (DataRow dr in invitees.Rows)
            {
                if ((string)dr["item_type"] == "USER")
                {
                    ids.Add((long)dr["item_id"]);
                }
            }

            return ids;
        }

        public static string BuildEventUri(Event calendarEvent)
        {
            return Linker.AppendSid(string.Format("{0}/calendar/event/{1}",
                calendarEvent.owner.Uri, calendarEvent.EventId));
        }

        public static string BuildEventAcceptUri(Core core, Event calendarEvent)
        {
            return Linker.BuildAccountSubModuleUri("calendar", "invite-event", "accept", calendarEvent.Id, true);
        }

        public static string BuildEventRejectUri(Core core, Event calendarEvent)
        {
            return Linker.BuildAccountSubModuleUri("calendar", "invite-event", "reject", calendarEvent.Id, true);
        }

        public static void Show(Core core, TPage page, Primitive owner, long eventId)
        {
            /*HttpContext.Current.Response.Write(BoxSocial.IO.Query.ObjectToSql(Event.GetFields(typeof(Event))));*/
            /*HttpContext.Current.Response.Write(BoxSocial.IO.Query.ObjectToSql(Event.GetTable(typeof(Event))));*/

            page.template.SetTemplate("Calendar", "viewcalendarevent");

            if (core.LoggedInMemberId == owner.Id && owner.Type == "USER")
            {
                page.template.Parse("U_NEW_EVENT", Linker.BuildAccountSubModuleUri("calendar", "new-event", true,
                    string.Format("year={0}", core.tz.Now.Year),
                    string.Format("month={0}", core.tz.Now.Month),
                    string.Format("day={0}", core.tz.Now.Day)));
                page.template.Parse("U_EDIT_EVENT", Linker.BuildAccountSubModuleUri("calendar", "new-event", "edit", eventId, true));
                page.template.Parse("U_DELETE_EVENT", Linker.BuildAccountSubModuleUri("calendar", "delete-event", eventId, true));
            }

            try
            {
                Event calendarEvent = new Event(core, owner, eventId);

                /*calendarEvent.Subject = "Hi";
                calendarEvent.Update();*/

                calendarEvent.EventAccess.SetSessionViewer(core.session);

                if (!calendarEvent.EventAccess.CanRead && !calendarEvent.IsInvitee(core.session.LoggedInMember))
                {
                    Functions.Generate403();
                    return;
                }

                page.template.Parse("SUBJECT", calendarEvent.Subject);
                page.template.Parse("LOCATION", calendarEvent.Location);
                page.template.Parse("DESCRIPTION", calendarEvent.Description);
                page.template.Parse("START_TIME", calendarEvent.GetStartTime(core.tz).ToString());
                page.template.Parse("END_TIME", calendarEvent.GetEndTime(core.tz).ToString());

                List<string[]> calendarPath = new List<string[]>();
                calendarPath.Add(new string[] { "calendar", "Calendar" });
                //calendarPath.Add(new string[] { "events", "Events" });
                calendarPath.Add(new string[] { "event/" + calendarEvent.EventId.ToString(), calendarEvent.Subject });
                //page.template.Parse("BREADCRUMBS", owner.GenerateBreadCrumbs(calendarPath));
                owner.ParseBreadCrumbs(calendarPath);

                if (calendarEvent.EventAccess.CanComment)
                {
                    page.template.Parse("CAN_COMMENT", "TRUE");
                }
                Display.DisplayComments(page.template, calendarEvent.owner, calendarEvent);

                List<long> attendees = calendarEvent.GetAttendees();
                core.LoadUserProfiles(attendees);

                page.template.Parse("ATTENDEES", calendarEvent.Attendees.ToString());
                page.template.Parse("INVITEES", calendarEvent.Invitees.ToString());
                if (attendees.Count > 1)
                {
                    page.template.Parse("L_IS_ARE", "is");
                    page.template.Parse("L_ATTENDEES", "attendees");
                }
                else
                {
                    page.template.Parse("L_IS_ARE", "are");
                    page.template.Parse("L_ATTENDEES", "attendee");
                }

                int i = 0;
                foreach (long attendeeId in attendees)
                {
                    i++;
                    if (i > 4)
                    {
                        break;
                    }
                    VariableCollection attendeesVariableCollection = page.template.CreateChild("attendee_list");
                    User attendee = core.UserProfiles[attendeeId];

                    attendeesVariableCollection.Parse("U_PROFILE", attendee.Uri);
                    attendeesVariableCollection.Parse("USER_DISPLAY_NAME", attendee.DisplayName);
                    attendeesVariableCollection.Parse("ICON", attendee.UserTile);
                    attendeesVariableCollection.Parse("ICON", attendee.UserTile);
                }
            }
            catch (Exception ex)
            {
                Display.ShowMessage("Invalid event", "The event does not exist. " + ex.ToString());
            }
        }

        public override long Id
        {
            get
            {
                return eventId;
            }
        }

        public override string Namespace
        {
            get
            {
                return this.GetType().FullName;
            }
        }

        public override string Uri
        {
            get
            {
                return Event.BuildEventUri(this);
            }
        }

        public SortOrder CommentSortOrder
        {
            get
            {
                return SortOrder.Ascending;
            }
        }

        public byte CommentsPerPage
        {
            get
            {
                return 10;
            }
        }

        public Access Access
        {
            get
            {
                return EventAccess;
            }
        }

        public List<string> PermissibleActions
        {
            get
            {
                List<string> permissions = new List<string>();
                permissions.Add("Can Read");

                return permissions;
            }
        }


        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj is Event || obj is BirthdayEvent)
            {
                return startTimeRaw.CompareTo(((Event)obj).startTimeRaw);
            }
            else
            {
                return -1;
            }
        }

        #endregion
    }

    public class InvalidEventException : Exception
    {
    }
}
