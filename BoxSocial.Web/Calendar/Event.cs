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
    [DataTable("events", "EVENT")]
    [Permission("VIEW", "Can view the event", PermissionTypes.View)]
    [Permission("COMMENT", "Can leave comments on the event", PermissionTypes.Interact)]
    [Permission("INVITE", "Can invite people to the event", PermissionTypes.CreateAndEdit)]
    public class Event : NumberedItem, ICommentableItem, IPermissibleItem, IComparable, INotifiableItem
    {
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
        protected long attendeeCount;
        [DataField("event_maybes")]
        protected long maybeCount;
        [DataField("event_item", DataFieldKeys.Index)]
        protected ItemKey ownerKey;
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
        [DataField("event_simple_permissions")]
        private bool simplePermissions;
        #endregion

        protected Access access;
        protected Primitive owner;
        protected Calendar calendar;

        public event CommentHandler OnCommentPosted;

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
                return attendeeCount;
            }
        }

        public long Comments
        {
            get
            {
                return Info.Comments;
            }
        }

        public long UserId
        {
            get
            {
                return userId;
            }
        }

        public ItemKey OwnerKey
        {
            get
            {
                return ownerKey;
            }
        }

        public Primitive Owner
        {
            get
            {
                if (owner == null || ownerKey.Id != owner.Id || ownerKey.TypeId != owner.TypeId)
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(ownerKey);
                    owner = core.PrimitiveCache[ownerKey];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

        public Calendar Calendar
        {
            get
            {
                if (calendar == null)
                {
                    calendar = new Calendar(core, Owner);
                    return calendar;
                }
                else
                {
                    return calendar;
                }
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

        public Event(Core core, long eventId)
            : base(core)
        {
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

        public Event(Core core, DataRow eventRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Event_ItemLoad);

            loadItemInfo(eventRow);
        }

        public Event(Core core, System.Data.Common.DbDataReader eventRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Event_ItemLoad);

            loadItemInfo(eventRow);
        }

        protected override void loadItemInfo(DataRow eventRow)
        {
            loadValue(eventRow, "event_id", out eventId);
            loadValue(eventRow, "event_subject", out subject);
            loadValue(eventRow, "event_description", out description);
            loadValue(eventRow, "event_views", out views);
            loadValue(eventRow, "event_attendees", out attendeeCount);
            loadValue(eventRow, "event_maybes", out maybeCount);
            loadValue(eventRow, "event_item", out ownerKey);
            loadValue(eventRow, "user_id", out userId);
            loadValue(eventRow, "event_time_start_ut", out startTimeRaw);
            loadValue(eventRow, "event_time_end_ut", out endTimeRaw);
            loadValue(eventRow, "event_all_day", out allDay);
            loadValue(eventRow, "event_invitees", out invitees);
            loadValue(eventRow, "event_category", out category);
            loadValue(eventRow, "event_location", out location);
            loadValue(eventRow, "event_simple_permissions", out simplePermissions);

            itemLoaded(eventRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader eventRow)
        {
            loadValue(eventRow, "event_id", out eventId);
            loadValue(eventRow, "event_subject", out subject);
            loadValue(eventRow, "event_description", out description);
            loadValue(eventRow, "event_views", out views);
            loadValue(eventRow, "event_attendees", out attendeeCount);
            loadValue(eventRow, "event_maybes", out maybeCount);
            loadValue(eventRow, "event_item", out ownerKey);
            loadValue(eventRow, "user_id", out userId);
            loadValue(eventRow, "event_time_start_ut", out startTimeRaw);
            loadValue(eventRow, "event_time_end_ut", out endTimeRaw);
            loadValue(eventRow, "event_all_day", out allDay);
            loadValue(eventRow, "event_invitees", out invitees);
            loadValue(eventRow, "event_category", out category);
            loadValue(eventRow, "event_location", out location);
            loadValue(eventRow, "event_simple_permissions", out simplePermissions);

            itemLoaded(eventRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        private void Event_ItemLoad()
        {
            OnCommentPosted += new CommentHandler(Event_CommentPosted);
        }

        bool Event_CommentPosted(CommentPostedEventArgs e)
        {
            if (Owner is User)
            {
                core.CallingApplication.QueueNotifications(core, e.Comment.ItemKey, "notifyEventComment");
            }

            return true;
        }

        public void CommentPosted(CommentPostedEventArgs e)
        {
            if (OnCommentPosted != null)
            {
                OnCommentPosted(e);
            }
        }

        public static void NotifyEventComment(Core core, Job job)
        {
            Comment comment = new Comment(core, job.ItemId);
            Event ev = new Event(core, comment.CommentedItemKey.Id);

            if (ev.Owner is User && (!comment.OwnerKey.Equals(ev.OwnerKey)))
            {
                core.CallingApplication.SendNotification(core, comment.User, (User)ev.Owner, ev.OwnerKey, ev.ItemKey, "_COMMENTED_EVENT", comment.BuildUri(ev));
            }

            core.CallingApplication.SendNotification(core, comment.OwnerKey, comment.User, ev.OwnerKey, ev.ItemKey, "_COMMENTED_EVENT", comment.BuildUri(ev));
        }

        public static Event Create(Core core, Primitive owner, string subject, string location, string description, long startTimestamp, long endTimestamp)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            Item item = Item.Create(core, typeof(Event), new FieldValuePair("user_id", core.Session.LoggedInMember.Id),
                new FieldValuePair("event_item_id", owner.Id),
                new FieldValuePair("event_item_type_id", owner.TypeId),
                new FieldValuePair("event_subject", subject),
                new FieldValuePair("event_location", location),
                new FieldValuePair("event_description", description),
                new FieldValuePair("event_time_start_ut", startTimestamp),
                new FieldValuePair("event_time_end_ut", endTimestamp));

            Event newEvent =  (Event)item;

            /*if (Access.FriendsCanRead(myEvent.Permissions))
            {
                core.CallingApplication.PublishToFeed(creator, "created a new event", string.Format("[iurl={0}]{1}[/iurl]",
                    Event.BuildEventUri(core, myEvent), myEvent.subject));
            }*/

            Access.CreateAllGrantsForOwner(core, newEvent);
            Access.CreateGrantForPrimitive(core, newEvent, EventInvite.GetInviteesGroupKey(core), "VIEW", "COMMENT");

            newEvent.IsSimplePermissions = true;
            newEvent.Update();

            return newEvent;
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
            User user = core.PrimitiveCache[userId];
            // only the person who created the event can invite people to it
            if (core.LoggedInMemberId == userId)
            {
                // we can only invite people friends with us to an event
                if (invitee.IsFriend(user.ItemKey))
                {
                    InsertQuery iQuery = new InsertQuery("event_invites");
                    iQuery.AddField("event_id", EventId);
                    iQuery.AddField("item_id", invitee.Id);
                    iQuery.AddField("item_type_id", invitee.TypeId);
                    iQuery.AddField("inviter_id", userId);
                    iQuery.AddField("invite_date_ut", UnixTime.UnixTimeStamp());
                    iQuery.AddField("invite_accepted", false);
                    iQuery.AddField("invite_status", (byte)EventAttendance.Unknown);

                    long invitationId = db.Query(iQuery);

                    UpdateQuery uQuery = new UpdateQuery("events");
                    uQuery.AddField("event_invitees", new QueryOperation("event_invitees", QueryOperations.Addition, 1));
                    uQuery.AddCondition("event_id", EventId);

                    db.Query(uQuery);

                    core.CallingApplication.SendNotification(core, user, invitee, OwnerKey, ItemKey, "_INVITED_EVENT", Uri, "invite");

                }
                else
                {
                    throw new CouldNotInviteEventException();
                }
            }
            else
            {
                throw new CouldNotInviteEventException();
            }
        }

        public void Invite(Core core, List<User> invitees)
        {
            core.LoadUserProfile(userId);
            User user = core.PrimitiveCache[userId];
            // only the person who created the event can invite people to it
            if (core.LoggedInMemberId == userId)
            {
                long friends = 0;
                foreach (User invitee in invitees)
                {
                    // we can only invite people friends with us to an event
                    if (invitee.IsFriend(user.ItemKey))
                    {
                        friends++;

                        InsertQuery iQuery = new InsertQuery("event_invites");
                        iQuery.AddField("event_id", EventId);
                        iQuery.AddField("item_id", invitee.Id);
                        iQuery.AddField("item_typeId", invitee.TypeId);
                        iQuery.AddField("inviter_id", userId);
                        iQuery.AddField("invite_date_ut", UnixTime.UnixTimeStamp());
                        iQuery.AddField("invite_accepted", false);
                        iQuery.AddField("invite_status", (byte)EventAttendance.Unknown);

                        long invitationId = db.Query(iQuery);

                        core.CallingApplication.SendNotification(core, user, invitee, OwnerKey, ItemKey, "_INVITED_EVENT", Uri, "invite");

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
                throw new CouldNotInviteEventException();
            }
        }

        public List<long> GetInviteeIds()
        {
            List<long> ids = new List<long>();

            SelectQuery query = new SelectQuery("event_invites");
            query.AddFields("item_id", "item_type_id", "inviter_id", "event_id");
            query.AddCondition("event_id", EventId);

            DataTable invitees = db.Query(query);

            foreach (DataRow dr in invitees.Rows)
            {
                if ((long)dr["item_type_id"] == ItemKey.GetTypeId(core, typeof(User)))
                {
                    ids.Add((long)dr["item_id"]);
                }
            }

            return ids;
        }

        public List<EventInvite> GetInvites()
        {
            List<EventInvite> invites = new List<EventInvite>();

            SelectQuery query = EventInvite.GetSelectQueryStub(core, typeof(EventInvite));
            query.AddCondition("event_id", EventId);

            DataTable invitesDataTable = db.Query(query);

            foreach (DataRow dr in invitesDataTable.Rows)
            {
                invites.Add(new EventInvite(core, dr));
            }

            return invites;
        }

        public bool IsInvitee(ItemKey member)
        {
            if (member != null)
            {
                SelectQuery query = EventInvite.GetSelectQueryStub(core, typeof(EventInvite));
                query.AddCondition("event_id", EventId);
                query.AddCondition("item_id", member.Id);
                query.AddCondition("item_type_id", member.TypeId);

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

        public bool IsAttending(ItemKey member, EventAttendance attendance)
        {
            if (member != null)
            {
                SelectQuery query = EventInvite.GetSelectQueryStub(core, typeof(EventInvite));
                query.AddCondition("event_id", EventId);
                query.AddCondition("item_id", member.Id);
                query.AddCondition("item_type_id", member.TypeId);
                query.AddCondition("item_type_id", (byte)attendance);

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
            query.AddFields("item_id", "item_type_id", "inviter_id", "event_id");
            query.AddCondition("event_id", EventId);
            query.AddCondition("invite_accepted", (byte)EventAttendance.Yes);

            DataTable invitees = db.Query(query);

            foreach (DataRow dr in invitees.Rows)
            {
                if ((long)dr["item_type_id"] == ItemKey.GetTypeId(core, typeof(User)))
                {
                    ids.Add((long)dr["item_id"]);
                }
            }

            return ids;
        }

        public static string BuildEventUri(Core core, Event calendarEvent)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}calendar/event/{1}",
                calendarEvent.Owner.Uri, calendarEvent.EventId));
        }

        public static string BuildEventAcceptUri(Core core, Event calendarEvent)
        {
            return core.Hyperlink.BuildAccountSubModuleUri("calendar", "invite-event", "accept", calendarEvent.Id, true);
        }

        public static string BuildEventMaybeUri(Core core, Event calendarEvent)
        {
            return core.Hyperlink.BuildAccountSubModuleUri("calendar", "invite-event", "maybe", calendarEvent.Id, true);
        }

        public static string BuildEventRejectUri(Core core, Event calendarEvent)
        {
            return core.Hyperlink.BuildAccountSubModuleUri("calendar", "invite-event", "reject", calendarEvent.Id, true);
        }

        public static void ShowAll(object sender, ShowPPageEventArgs e)
        {
            e.Template.SetTemplate("Calendar", "viewcalendarevents");

            /* pages */
            e.Core.Display.ParsePageList(e.Page.Owner, true);

            e.Template.Parse("PAGE_TITLE", e.Core.Prose.GetString("EVENTS"));

            long startTime = e.Core.Tz.GetUnixTimeStamp(new DateTime(e.Core.Tz.Now.Year, e.Core.Tz.Now.Month, e.Core.Tz.Now.Day, 0, 0, 0));
            long endTime = startTime + 60 * 60 * 24 * 30; // skip ahead one month into the future

            Calendar cal = null;
            try
            {
                cal = new Calendar(e.Core, e.Page.Owner);
            }
            catch (InvalidCalendarException)
            {
                cal = Calendar.Create(e.Core, e.Page.Owner);
            }

            List<Event> events = cal.GetEvents(e.Core, e.Page.Owner, startTime, endTime);

            VariableCollection appointmentDaysVariableCollection = null;
            DateTime lastDay = e.Core.Tz.Now;

            e.Template.Parse("U_NEW_EVENT", e.Core.Hyperlink.AppendSid(string.Format("{0}calendar/new-event",
                e.Page.Owner.AccountUriStub)));

            if (events.Count > 0)
            {
                e.Template.Parse("HAS_EVENTS", "TRUE");
            }

            foreach (Event calendarEvent in events)
            {
                DateTime eventDay = calendarEvent.GetStartTime(e.Core.Tz);
                DateTime eventEnd = calendarEvent.GetEndTime(e.Core.Tz);

                if (appointmentDaysVariableCollection == null || lastDay.Day != eventDay.Day)
                {
                    lastDay = eventDay;
                    appointmentDaysVariableCollection = e.Template.CreateChild("appointment_days_list");

                    appointmentDaysVariableCollection.Parse("DAY", e.Core.Tz.DateTimeToDateString(eventDay, true));
                }

                VariableCollection appointmentVariableCollection = appointmentDaysVariableCollection.CreateChild("appointments_list");

                appointmentVariableCollection.Parse("TIME", eventDay.ToShortTimeString() + " - " + eventEnd.ToShortTimeString());
                appointmentVariableCollection.Parse("SUBJECT", calendarEvent.Subject);
                appointmentVariableCollection.Parse("LOCATION", calendarEvent.Location);
                appointmentVariableCollection.Parse("URI", Event.BuildEventUri(e.Core, calendarEvent));
            }
        }

        public static void Show(object sender, ShowPPageEventArgs e)
        {
            e.Template.SetTemplate("Calendar", "viewcalendarevent");

            /* pages */
            e.Core.Display.ParsePageList(e.Page.Owner, true);

            Event calendarEvent = null;

            if (e.ItemId <= 0)
            {
                User pageUser = null;
                if (e.Page.Owner is User)
                {
                    pageUser = (User)e.Page.Owner;
                    e.Core.LoadUserProfile(~e.ItemId);
                    User birthdayUser = e.Core.PrimitiveCache[~e.ItemId];
                    calendarEvent = new BirthdayEvent(e.Core, pageUser, birthdayUser, e.Core.Tz.Now.Year);
                }
                else
                {
                    e.Core.Functions.Generate404();
                    return;
                }
            }
            else
            {
                try
                {
                    calendarEvent = new Event(e.Core, e.ItemId);
                }
                catch (InvalidEventException)
                {
                    e.Core.Functions.Generate404();
                    return;
                }
            }

            if (e.Core.LoggedInMemberId == e.Page.Owner.Id && e.Page.Owner.GetType() == typeof(User))
            {
                e.Template.Parse("U_NEW_EVENT", e.Core.Hyperlink.BuildAccountSubModuleUri("calendar", "new-event", true,
                    string.Format("year={0}", e.Core.Tz.Now.Year),
                    string.Format("month={0}", e.Core.Tz.Now.Month),
                    string.Format("day={0}", e.Core.Tz.Now.Day)));
                if (!(calendarEvent is BirthdayEvent))
                {
                    e.Template.Parse("U_EDIT_EVENT", e.Core.Hyperlink.BuildAccountSubModuleUri("calendar", "new-event", "edit", e.ItemId, true));
                    e.Template.Parse("U_DELETE_EVENT", e.Core.Hyperlink.BuildAccountSubModuleUri("calendar", "delete-event", e.ItemId, true));
                    e.Template.Parse("U_EDIT_PERMISSIONS", Access.BuildAclUri(e.Core, calendarEvent));
                }
            }

            if (calendarEvent is BirthdayEvent)
            {
            }
            else
            {
                if ((!calendarEvent.Access.Can("VIEW")) && ((!e.Core.Session.IsLoggedIn) || (!calendarEvent.IsInvitee(e.Core.Session.LoggedInMember.ItemKey))))
                {
                    e.Core.Functions.Generate403();
                    return;
                }
            }

            e.Template.Parse("PAGE_TITLE", calendarEvent.Subject);
            e.Template.Parse("SUBJECT", calendarEvent.Subject);
            e.Template.Parse("LOCATION", calendarEvent.Location);
            e.Template.Parse("DESCRIPTION", calendarEvent.Description);
            e.Template.Parse("START_TIME", calendarEvent.GetStartTime(e.Core.Tz).ToString());
            e.Template.Parse("END_TIME", calendarEvent.GetEndTime(e.Core.Tz).ToString());

            List<string[]> calendarPath = new List<string[]>();
            calendarPath.Add(new string[] { "calendar", e.Core.Prose.GetString("CALENDAR") });
            //calendarPath.Add(new string[] { "events", "Events" });
            calendarPath.Add(new string[] { "event/" + calendarEvent.EventId.ToString(), calendarEvent.Subject });
            e.Page.Owner.ParseBreadCrumbs(calendarPath);

            if (!(calendarEvent is BirthdayEvent))
            {
                if (calendarEvent.Access.Can("COMMENT") || (e.Core.Session.IsLoggedIn && calendarEvent.IsInvitee(e.Core.Session.LoggedInMember.ItemKey)))
                {
                    e.Template.Parse("CAN_COMMENT", "TRUE");
                }
                e.Core.Display.DisplayComments(e.Template, calendarEvent.owner, calendarEvent);

                List<EventInvite> invitees = calendarEvent.GetInvites();
                List<long> invitedIds = new List<long>(); /* Users to be displayed on the page */

                long attendingCount = 0;
                long invitedCount = 0;
                long notAttendingCount = 0;
                long mightAttendCount = 0;
                long notRespondedCount = 0;

                int i = 0, j = 0, k = 0, l = 0;
                foreach (EventInvite ei in invitees)
                {
                    if (ei.Invited.TypeId == ItemType.GetTypeId(e.Core, typeof(User)))
                    {
                        invitedCount++;

                        switch (ei.InviteStatus)
                        {
                            case EventAttendance.Yes:
                                if (i < 4)
                                {
                                    invitedIds.Add(ei.Invited.Id);
                                    i++;
                                }
                                attendingCount++;
                                break;
                            case EventAttendance.Maybe:
                                if (j < 4)
                                {
                                    invitedIds.Add(ei.Invited.Id);
                                    j++;
                                }
                                mightAttendCount++;
                                break;
                            case EventAttendance.No:
                                if (k < 4)
                                {
                                    invitedIds.Add(ei.Invited.Id);
                                    k++;
                                }
                                notAttendingCount++;
                                break;
                            case EventAttendance.Unknown:
                                if (l < 4)
                                {
                                    invitedIds.Add(ei.Invited.Id);
                                    l++;
                                }
                                notRespondedCount++;
                                break;
                        }
                    }
                }

                e.Core.LoadUserProfiles(invitedIds);

                e.Template.Parse("ATTENDEES", attendingCount.ToString());
                e.Template.Parse("INVITEES", invitedCount.ToString());
                e.Template.Parse("NOT_ATTENDING", notAttendingCount.ToString());
                e.Template.Parse("MAYBE_ATTENDING", mightAttendCount.ToString());
                e.Template.Parse("NOT_RESPONDED", notRespondedCount.ToString());

                if (attendingCount > 1)
                {
                    e.Template.Parse("L_IS_ARE", "is");
                    e.Template.Parse("L_ATTENDEES", "attendees");
                }
                else
                {
                    e.Template.Parse("L_IS_ARE", "are");
                    e.Template.Parse("L_ATTENDEES", "attendee");
                }

                i = j = k = l = 0;
                foreach (EventInvite ei in invitees)
                {
                    if (ei.Invited.TypeId == ItemType.GetTypeId(e.Core, typeof(User)))
                    {
                        VariableCollection listVariableCollection = null;

                        switch (ei.InviteStatus)
                        {
                            case EventAttendance.Yes:
                                i++;
                                if (i > 4)
                                {
                                    continue;
                                }
                                listVariableCollection = e.Template.CreateChild("attendee_list");
                                break;
                            case EventAttendance.No:
                                j++;
                                if (j > 4)
                                {
                                    continue;
                                }
                                listVariableCollection = e.Template.CreateChild("not_attending_list");
                                break;
                            case EventAttendance.Maybe:
                                k++;
                                if (k > 4)
                                {
                                    continue;
                                }
                                listVariableCollection = e.Template.CreateChild("maybe_attending_list");
                                break;
                            case EventAttendance.Unknown:
                                l++;
                                if (l > 4)
                                {
                                    continue;
                                }
                                listVariableCollection = e.Template.CreateChild("unresponded_list");
                                break;
                        }

                        User user = e.Core.PrimitiveCache[ei.Invited.Id];

                        listVariableCollection.Parse("U_PROFILE", user.Uri);
                        listVariableCollection.Parse("USER_DISPLAY_NAME", user.DisplayName);
                        listVariableCollection.Parse("ICON", user.Icon);
                        listVariableCollection.Parse("TILE", user.Tile);
                    }
                }
            }
        }

        public override long Id
        {
            get
            {
                return eventId;
            }
        }

        public override string Uri
        {
            get
            {
                return Event.BuildEventUri(core, this);
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
                if (access == null)
                {
                    access = new Access(core, this);
                }
                return access;
            }
        }

        public bool IsSimplePermissions
        {
            get
            {
                return simplePermissions;
            }
            set
            {
                SetPropertyByRef(new { simplePermissions }, value);
            }
        }

        public List<AccessControlPermission> AclPermissions
        {
            get
            {
                return AccessControlLists.GetPermissions(core, this);
            }
        }
        
        public IPermissibleItem PermissiveParent
        {
            get
            {
                return Calendar;
            }
        }

        public ItemKey PermissiveParentKey
        {
            get
            {
                return Calendar.ItemKey;
            }
        }

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

        public bool GetDefaultCan(string permission, ItemKey viewer)
        {
            return false;
        }

        public string DisplayTitle
        {
            get
            {
                return "Event: " + Subject + " (" + core.Tz.DateTimeToString(GetStartTime(core.Tz),false) + ")";
            }
        }

        public string ParentPermissionKey(Type parentType, string permission)
        {
            switch (permission)
            {
                case "INVITE":
                    return "INVITE_EVENTS";
                case "EDIT":
                    return "EDIT_EVENTS";
            }
            return permission;
        }

        public static List<PrimitivePermissionGroup> Event_GetItemGroups(Core core)
        {
            List<PrimitivePermissionGroup> itemGroups = new List<PrimitivePermissionGroup>();

            itemGroups.Add(new PrimitivePermissionGroup(EventInvite.GetInviteesGroupKey(core), "Event Invitees", string.Empty));
            itemGroups.Add(new PrimitivePermissionGroup(EventInvite.GetAttendingGroupKey(core), "Event Attending", string.Empty));
            itemGroups.Add(new PrimitivePermissionGroup(EventInvite.GetMaybeAttendingGroupKey(core), "Event Maybe Attending", string.Empty));
            itemGroups.Add(new PrimitivePermissionGroup(EventInvite.GetNotAttendingGroupKey(core), "Event Not Attending", string.Empty));

            return itemGroups;
        }

        public bool IsItemGroupMember(ItemKey viewer, ItemKey key)
        {
            if (key == EventInvite.GetInviteesGroupKey(core))
            {
                if (IsInvitee(viewer))
                {
                    return true;
                }
            }
            if (key == EventInvite.GetAttendingGroupKey(core))
            {
                if (IsAttending(viewer, EventAttendance.Yes))
                {
                    return true;
                }
            }

            return false;
        }

        public string Noun
        {
            get
            {
                return "event";
            }
        }


        public Dictionary<string, string> GetNotificationActions(string verb)
        {
            Dictionary<string, string> actions = new Dictionary<string, string>();

            switch (verb)
            {
                case "invite":
                    actions.Add("accept", core.Prose.GetString("ACCEPT"));
                    actions.Add("maybe", core.Prose.GetString("MAYBE"));
                    actions.Add("reject", core.Prose.GetString("REJECT"));
                    break;
            }

            return actions;
        }

        public string GetNotificationActionUrl(string action)
        {
            switch (action)
            {
                case "accept":
                    return BuildEventAcceptUri(core, this);
                case "maybe":
                    return BuildEventMaybeUri(core, this);
                case "reject":
                    return BuildEventRejectUri(core, this);
            }

            return string.Empty;
        }

        public string Title
        {
            get
            {
                return Subject;
            }
        }

        public bool CanComment
        {
            get
            {
                return Access.Can("COMMENT");
            }
        }
    }

    public class InvalidEventException : InvalidItemException
    {
    }
	
	public class CouldNotInviteEventException : Exception
	{
	}
}
