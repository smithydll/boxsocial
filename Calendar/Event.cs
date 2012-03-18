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
    [DataTable("events", "EVENT")]
    [Permission("VIEW", "Can view the event", PermissionTypes.View)]
    [Permission("COMMENT", "Can leave comments on the event", PermissionTypes.Interact)]
    [Permission("INVITE", "Can invite people to the event", PermissionTypes.CreateAndEdit)]
    public class Event : NumberedItem, ICommentableItem, IPermissibleItem, IComparable
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
        protected long attendees;
        [DataField("event_comments")]
        protected long comments;
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
        #endregion

        protected Access access;
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

        private void Event_ItemLoad()
        {
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
            Access.CreateGrantForPrimitive(core, newEvent, EventInvite.InviteesGroupKey, "VIEW", "COMMENT");

            /*if (isPublicEvent)
            {
                Access.CreateGrantForPrimitive(core, newEvent, Friend.FriendsGroupKey, "VIEW");
            }*/

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
                if (invitee.IsFriend(user))
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

                    RawTemplate emailTemplate = new RawTemplate(core.Http.TemplateEmailPath, "event_invitation.eml");

                    emailTemplate.Parse("FROM_NAME", user.DisplayName);
                    emailTemplate.Parse("FROM_EMAIL", user.Info.PrimaryEmail);
                    emailTemplate.Parse("FROM_NAMES", user.DisplayNameOwnership);
                    emailTemplate.Parse("EVENT_SUBJECT", this.Subject);
                    emailTemplate.Parse("U_EVENT", "http://zinzam.com" + core.Uri.StripSid(Event.BuildEventUri(core, this)));
                    emailTemplate.Parse("U_ACCEPT", "http://zinzam.com" + core.Uri.StripSid(Event.BuildEventAcceptUri(core, this)));
                    emailTemplate.Parse("U_REJECT", "http://zinzam.com" + core.Uri.StripSid(Event.BuildEventRejectUri(core, this)));

                    core.CallingApplication.SendNotification(invitee, string.Format("{0} has invited you to {1}.",
                        user.DisplayName, subject), string.Format("[iurl=\"{0}\" sid=true]Click Here[/iurl] accept the invitation.", Event.BuildEventAcceptUri(core, this)), emailTemplate);

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
                    if (invitee.IsFriend(user))
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

                        RawTemplate emailTemplate = new RawTemplate(core.Http.TemplateEmailPath, "event_invitation.eml");

                        emailTemplate.Parse("FROM_NAME", user.DisplayName);
                        emailTemplate.Parse("FROM_EMAIL", user.Info.PrimaryEmail);
                        emailTemplate.Parse("FROM_NAMES", user.DisplayNameOwnership);
                        emailTemplate.Parse("EVENT_SUBJECT", this.Subject);
                        emailTemplate.Parse("U_EVENT", "http://zinzam.com" + core.Uri.StripSid(Event.BuildEventUri(core, this)));
                        emailTemplate.Parse("U_ACCEPT", "http://zinzam.com" + core.Uri.StripSid(Event.BuildEventAcceptUri(core, this)));
                        emailTemplate.Parse("U_REJECT", "http://zinzam.com" + core.Uri.StripSid(Event.BuildEventRejectUri(core, this)));

                        core.CallingApplication.SendNotification(invitee, string.Format("{0} has invited you to {1}.",
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
                if ((long)dr["item_type_id"] == ItemKey.GetTypeId(typeof(User)))
                {
                    ids.Add((long)dr["item_id"]);
                }
            }

            return ids;
        }

        public List<EventInvite> GetInvites()
        {
            List<EventInvite> invites = new List<EventInvite>();

            SelectQuery query = new SelectQuery(typeof(EventInvite));
            query.AddFields("item_id", "item_type_id", "inviter_id", "event_id");
            query.AddCondition("event_id", EventId);

            DataTable invitesDataTable = db.Query(query);

            foreach (DataRow dr in invitesDataTable.Rows)
            {
                invites.Add(new EventInvite(core, dr));
            }

            return invites;
        }

        public bool IsInvitee(User member)
        {
            if (member != null)
            {
                SelectQuery query = EventInvite.GetSelectQueryStub(typeof(EventInvite));
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

        public bool IsAttending(User member, EventAttendance attendance)
        {
            if (member != null)
            {
                SelectQuery query = EventInvite.GetSelectQueryStub(typeof(EventInvite));
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
                if ((long)dr["item_type_id"] == ItemKey.GetTypeId(typeof(User)))
                {
                    ids.Add((long)dr["item_id"]);
                }
            }

            return ids;
        }

        public static string BuildEventUri(Core core, Event calendarEvent)
        {
            return core.Uri.AppendSid(string.Format("{0}calendar/event/{1}",
                calendarEvent.Owner.Uri, calendarEvent.EventId));
        }

        public static string BuildEventAcceptUri(Core core, Event calendarEvent)
        {
            return core.Uri.BuildAccountSubModuleUri("calendar", "invite-event", "accept", calendarEvent.Id, true);
        }

        public static string BuildEventRejectUri(Core core, Event calendarEvent)
        {
            return core.Uri.BuildAccountSubModuleUri("calendar", "invite-event", "reject", calendarEvent.Id, true);
        }

        public static void Show(object sender, ShowPPageEventArgs e)
        {
            e.Template.SetTemplate("Calendar", "viewcalendarevent");

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
                calendarEvent = new Event(e.Core, e.ItemId);
            }

            if (e.Core.LoggedInMemberId == e.Page.Owner.Id && e.Page.Owner.GetType() == typeof(User))
            {
                e.Template.Parse("U_NEW_EVENT", e.Core.Uri.BuildAccountSubModuleUri("calendar", "new-event", true,
                    string.Format("year={0}", e.Core.Tz.Now.Year),
                    string.Format("month={0}", e.Core.Tz.Now.Month),
                    string.Format("day={0}", e.Core.Tz.Now.Day)));
                if (!(calendarEvent is BirthdayEvent))
                {
                    e.Template.Parse("U_EDIT_EVENT", e.Core.Uri.BuildAccountSubModuleUri("calendar", "new-event", "edit", e.ItemId, true));
                    e.Template.Parse("U_DELETE_EVENT", e.Core.Uri.BuildAccountSubModuleUri("calendar", "delete-event", e.ItemId, true));
                    e.Template.Parse("U_EDIT_PERMISSIONS", Access.BuildAclUri(e.Core, calendarEvent));
                }
            }

            if (calendarEvent is BirthdayEvent)
            {
            }
            else
            {
                if (!calendarEvent.Access.Can("VIEW") && !calendarEvent.IsInvitee(e.Core.Session.LoggedInMember))
                {
                    e.Core.Functions.Generate403();
                    return;
                }
            }

            e.Template.Parse("SUBJECT", calendarEvent.Subject);
            e.Template.Parse("LOCATION", calendarEvent.Location);
            e.Template.Parse("DESCRIPTION", calendarEvent.Description);
            e.Template.Parse("START_TIME", calendarEvent.GetStartTime(e.Core.Tz).ToString());
            e.Template.Parse("END_TIME", calendarEvent.GetEndTime(e.Core.Tz).ToString());

            List<string[]> calendarPath = new List<string[]>();
            calendarPath.Add(new string[] { "calendar", "Calendar" });
            //calendarPath.Add(new string[] { "events", "Events" });
            calendarPath.Add(new string[] { "event/" + calendarEvent.EventId.ToString(), calendarEvent.Subject });
            e.Page.Owner.ParseBreadCrumbs(calendarPath);

            if (!(calendarEvent is BirthdayEvent))
            {
                if (calendarEvent.Access.Can("COMMENT") || (e.Core.Session.IsLoggedIn && calendarEvent.IsInvitee(e.Core.Session.LoggedInMember)))
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
                    if (ei.Invited.TypeId == ItemType.GetTypeId(typeof(User)))
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
                    if (ei.Invited.TypeId == ItemType.GetTypeId(typeof(User)))
                    {
                        VariableCollection listVariableCollection = null;

                        switch (ei.InviteStatus)
                        {
                            case EventAttendance.Yes:
                                i++;
                                if (i > 4)
                                {
                                    break;
                                }
                                listVariableCollection = e.Template.CreateChild("attendee_list");
                                break;
                            case EventAttendance.No:
                                j++;
                                if (j > 4)
                                {
                                    break;
                                }
                                listVariableCollection = e.Template.CreateChild("not_attending_list");
                                break;
                            case EventAttendance.Maybe:
                                k++;
                                if (k > 4)
                                {
                                    break;
                                }
                                listVariableCollection = e.Template.CreateChild("maybe_attending_list");
                                break;
                            case EventAttendance.Unknown:
                                l++;
                                if (l > 4)
                                {
                                    break;
                                }
                                listVariableCollection = e.Template.CreateChild("unresponded_list");
                                break;
                        }

                        User user = e.Core.PrimitiveCache[ei.Invited.Id];

                        listVariableCollection.Parse("U_PROFILE", user.Uri);
                        listVariableCollection.Parse("USER_DISPLAY_NAME", user.DisplayName);
                        listVariableCollection.Parse("ICON", user.UserTile);
                        listVariableCollection.Parse("ICON", user.UserTile);
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
                return Owner;
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

        public bool GetDefaultCan(string permission)
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

        public static List<PrimitivePermissionGroup> Event_GetItemGroups(Core core)
        {
            List<PrimitivePermissionGroup> itemGroups = new List<PrimitivePermissionGroup>();

            itemGroups.Add(new PrimitivePermissionGroup(EventInvite.InviteesGroupKey, "Event Invitees"));
            itemGroups.Add(new PrimitivePermissionGroup(EventInvite.AttendingGroupKey, "Event Attending"));
            itemGroups.Add(new PrimitivePermissionGroup(EventInvite.MaybeAttendingGroupKey, "Event Maybe Attending"));
            itemGroups.Add(new PrimitivePermissionGroup(EventInvite.NotAttendingGroupKey, "Event Not Attending"));

            return itemGroups;
        }

        public bool IsItemGroupMember(User viewer, ItemKey key)
        {
            if (key == EventInvite.InviteesGroupKey)
            {
                if (IsInvitee(viewer))
                {
                    return true;
                }
            }
            if (key == EventInvite.AttendingGroupKey)
            {
                if (IsAttending(viewer, EventAttendance.Yes))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class InvalidEventException : Exception
    {
    }
	
	public class CouldNotInviteEventException : Exception
	{
	}
}
