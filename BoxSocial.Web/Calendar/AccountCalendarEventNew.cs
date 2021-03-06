﻿/*
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
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Web;
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Calendar
{
    [AccountSubModule(AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network, "calendar", "new-event")]
    public class AccountCalendarEventNew : AccountSubModule, IPermissibleControlPanelSubModule
    {
        Calendar calendar;

        public override string Title
        {
            get
            {
                return core.Prose.GetString("NEW_EVENT");
            }
        }

        public override int Order
        {
            get
            {
                return 2;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountCalendarEventNew class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountCalendarEventNew(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountCalendarEventNew_Load);
            this.Show += new EventHandler(AccountCalendarEventNew_Show);
        }

        void AccountCalendarEventNew_Load(object sender, EventArgs e)
        {
            AddModeHandler("edit", new ModuleModeHandler(AccountCalendarEventNew_Show));
            AddSaveHandler("edit", new EventHandler(AccountCalendarEventNew_Save));
            /*AddModeHandler("delete", new ModuleModeHandler(AccountCalendarEventNew_Delete));
            AddSaveHandler("delete", new EventHandler(AccountCalendarEventNew_Delete_Save));*/
        }

        void AccountCalendarEventNew_Show(object sender, EventArgs e)
        {
            SetTemplate("account_calendar_event_new");

            DateTimePicker startDateTimePicker = new DateTimePicker(core, "start-date");
            startDateTimePicker.ShowTime = true;
            startDateTimePicker.ShowSeconds = false;

            DateTimePicker endDateTimePicker = new DateTimePicker(core, "end-date");
            endDateTimePicker.ShowTime = true;
            endDateTimePicker.ShowSeconds = false;

            UserSelectBox inviteesUserSelectBox = new UserSelectBox(core, "invitees");

            /* */
            SelectBox timezoneSelectBox = UnixTime.BuildTimeZoneSelectBox("timezone");

            bool edit = false;

            if (core.Http.Query["mode"] == "edit")
            {
                edit = true;
            }

            int year = core.Functions.RequestInt("year", tz.Now.Year);
            int month = core.Functions.RequestInt("month", tz.Now.Month);
            int day = core.Functions.RequestInt("day", tz.Now.Day);

            string inviteeUsernameList = core.Http.Form["invitees[raw]"];
            List<long> inviteeIds = UserSelectBox.FormUsers(core, "invitees");

            if (!(string.IsNullOrEmpty(inviteeUsernameList)))
            {
                string[] inviteesUsernames = inviteeUsernameList.Split(new char[] { ' ', '\t', ';', ',' });
                List<string> uns = new List<string>(inviteesUsernames.Length);
                foreach (string inviteeUsername in inviteesUsernames)
                {
                    if (!string.IsNullOrEmpty(inviteeUsername))
                    {
                        try
                        {
                            uns.Add(inviteeUsername);
                        }
                        catch (InvalidUserException)
                        {
                        }
                    }
                }

                inviteeIds.AddRange(core.LoadUserProfiles(uns).Values);
            }

            DateTime startDate = new DateTime(year, month, day, 8, 0, 0);
            DateTime endDate = new DateTime(year, month, day, 9, 0, 0);
            timezoneSelectBox.SelectedKey = core.Tz.TimeZoneCode.ToString();

            string subject = string.Empty;
            string location = string.Empty;
            string description = string.Empty;
			
			if (edit)
            {
                int id = core.Functions.RequestInt("id", -1);

                if (id < 1)
                {
                    core.Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                }

                try
                {
                    Event calendarEvent = new Event(core, id);
                    inviteeIds.AddRange(calendarEvent.GetInviteeIds());

                    template.Parse("EDIT", "TRUE");
                    template.Parse("ID", calendarEvent.EventId.ToString());

                    startDate = calendarEvent.GetStartTime(core.Tz);
                    endDate = calendarEvent.GetEndTime(core.Tz);
                    inviteesUserSelectBox.Invitees = calendarEvent.GetInviteeIds();

                    subject = calendarEvent.Subject;
                    location = calendarEvent.Location;
                    description = calendarEvent.Description;
                }
                catch (InvalidEventException)
                {
                    core.Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                }
            }

            template.Parse("S_YEAR", year.ToString());
            template.Parse("S_MONTH", month.ToString());
            template.Parse("S_DAY", day.ToString());


            startDateTimePicker.Value = startDate;
            endDateTimePicker.Value = endDate;

            List<string> permissions = new List<string>();
            permissions.Add("Can Read");

            //core.Display.ParsePermissionsBox(template, "S_EVENT_PERMS", eventAccess, permissions);

            template.Parse("S_SUBJECT", subject);
            template.Parse("S_LOCATION", location);
            template.Parse("S_DESCRIPTION", description);

            StringBuilder outputInvitees = null;
            StringBuilder outputInviteesIds = null;

            core.LoadUserProfiles(inviteeIds);

            foreach (long inviteeId in inviteeIds)
            {
                if (outputInvitees == null)
                {
                    outputInvitees = new StringBuilder();
                    outputInviteesIds = new StringBuilder();
                }
                else
                {
                    outputInvitees.Append("; ");
                    outputInviteesIds.Append(",");
                }

                outputInvitees.Append(core.PrimitiveCache[inviteeId].DisplayName);
                outputInviteesIds.Append(inviteeId.ToString());
            }

            if (outputInviteesIds != null)
            {
                template.Parse("INVITEES_ID_LIST", outputInviteesIds.ToString());
            }

            template.Parse("S_START_DATE", startDateTimePicker);
            template.Parse("S_END_DATE", endDateTimePicker);
            template.Parse("S_TIMEZONE", timezoneSelectBox);
            template.Parse("S_INVITEES", inviteesUserSelectBox);

            Save(new EventHandler(AccountCalendarEventNew_Save));
        }

        void AccountCalendarEventNew_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long eventId = 0;
            string subject = string.Empty;
            string location = string.Empty;
            string description = string.Empty;
            long startTime = tz.GetUnixTimeStamp(tz.Now);
            long endTime = tz.GetUnixTimeStamp(tz.Now);
            bool edit = false;
            ushort timezone = core.Functions.FormUShort("timezone", 1);

            if (core.Http.Form["mode"] == "edit")
            {
                edit = true;
            }

            string inviteeUsernameList = core.Http.Form["invitees[raw]"];
            List<long> inviteeIds = UserSelectBox.FormUsers(core, "invitees");
            List<string> inviteesEmailsList = new List<string>();

            if (!(string.IsNullOrEmpty(inviteeUsernameList)))
            {
                string[] inviteesUsernames = inviteeUsernameList.Split(new char[] { ' ', '\t', ';', ',' });
                List<string> inviteesUsernamesList = new List<string>();

                foreach (string inviteeUsername in inviteesUsernames)
                {
                    if (!string.IsNullOrEmpty(inviteeUsername))
                    {
                        if (core.Email.IsEmailAddress(inviteeUsername))
                        {
                            inviteesEmailsList.Add(inviteeUsername);
                        }
                        else
                        {
                            inviteesUsernamesList.Add(inviteeUsername);
                        }
                    }
                }

                inviteeIds.AddRange(core.LoadUserProfiles(inviteesUsernamesList).Values);
            }

            try
            {
                subject = core.Http.Form["subject"];
                location = core.Http.Form["location"];
                description = core.Http.Form["description"];

                startTime = DateTimePicker.FormDate(core, "start-date", timezone);
                endTime = DateTimePicker.FormDate(core, "end-date", timezone);

                if (edit)
                {
                    eventId = long.Parse(core.Http.Form["id"]);
                }
            }
            catch
            {
                core.Display.ShowMessage("Invalid submission", "You have made an invalid form submission.");
                return;
            }


            if (!edit)
            {
                Event calendarEvent = Event.Create(core, Owner, subject, location, description, startTime, endTime);

                foreach (long inviteeId in inviteeIds)
                {
					try
					{
						calendarEvent.Invite(core, core.PrimitiveCache[inviteeId]);
					}
					catch (CouldNotInviteEventException)
					{
					}
                }

                SetRedirectUri(Event.BuildEventUri(core, calendarEvent));
                core.Display.ShowMessage("Event Created", "You have successfully created a new event.");
            }
            else
            {
                Event calendarEvent = new Event(core, eventId);
                calendarEvent.Location = location;
                calendarEvent.Subject = subject;
                calendarEvent.Description = description;
                calendarEvent.StartTimeRaw = startTime;
                calendarEvent.EndTimeRaw = endTime;
                
                calendarEvent.Update();

                List<long> alreadyInvited = calendarEvent.GetInviteeIds();
				
				List<long> idsToBeInvited = new List<long>();
				
				foreach (long inviteeId in inviteeIds)
				{
					if (!alreadyInvited.Contains(inviteeId))
					{
						idsToBeInvited.Add(inviteeId);
					}
				}

                core.LoadUserProfiles(idsToBeInvited);

                foreach (long inviteeId in idsToBeInvited)
                {
					try
					{
						calendarEvent.Invite(core, core.PrimitiveCache[inviteeId]);
					}
					catch (CouldNotInviteEventException)
					{
					}
                }

                foreach (string email in inviteesEmailsList)
                {
                    try
                    {
                        string emailKey = User.GenerateActivationSecurityToken(); ;

                        InsertQuery iQuery = new InsertQuery("event_email_invites");
                        iQuery.AddField("event_id", calendarEvent.Id);
                        iQuery.AddField("invite_email", email);
                        iQuery.AddField("invite_key", emailKey);
                        iQuery.AddField("inviter_id", core.Session.LoggedInMember.Id);
                        iQuery.AddField("invite_date_ut", UnixTime.UnixTimeStamp());
                        iQuery.AddField("invite_accepted", false);
                        iQuery.AddField("invite_status", (byte)EventAttendance.Unknown);

                        core.Db.Query(iQuery);
                    }
                    catch (CouldNotInviteEventException)
                    {
                    }
                }

                SetRedirectUri(Event.BuildEventUri(core, calendarEvent));
                core.Display.ShowMessage("Event Saved", "You have successfully saved your changes to the event.");
            }
        }

        public Access Access
        {
            get
            {
                if (calendar == null)
                {
                    calendar = new Calendar(core, Owner);
                }
                return calendar.Access;
            }
        }

        public string AccessPermission
        {
            get
            {
                return "CREATE_EVENTS";
            }
        }
    }
}
