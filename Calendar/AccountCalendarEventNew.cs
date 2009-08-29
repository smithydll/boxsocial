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
    [AccountSubModule("calendar", "new-event")]
    public class AccountCalendarEventNew : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "New Event";
            }
        }

        public override int Order
        {
            get
            {
                return 2;
            }
        }

        public AccountCalendarEventNew()
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

            bool edit = false;
            ushort eventAccess = 0;

            if (core.Http.Query["mode"] == "edit")
            {
                edit = true;
            }

            int year = core.Functions.RequestInt("year", tz.Now.Year);
            int month = core.Functions.RequestInt("month", tz.Now.Month);
            int day = core.Functions.RequestInt("day", tz.Now.Day);

            string inviteeIdList = core.Http.Form["inviteeses"];
            string inviteeUsernameList = core.Http.Form["invitees"];
            List<long> inviteeIds = new List<long>();

            if (!(string.IsNullOrEmpty(inviteeIdList)))
            {
                string[] inviteesIds = inviteeIdList.Split(new char[] { ' ', '\t', ';', ',' });
                foreach (string inviteeId in inviteesIds)
                {
                    try
                    {
                        inviteeIds.Add(long.Parse(inviteeId));
                    }
                    catch
                    {
                    }
                }
            }

            if (!(string.IsNullOrEmpty(inviteeUsernameList)))
            {
                string[] inviteesUsernames = inviteeUsernameList.Split(new char[] { ' ', '\t', ';', ',' });
                List<string> uns = new List<string>();
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

                inviteeIds.AddRange(core.LoadUserProfiles(uns));
            }

            DateTime startDate = new DateTime(year, month, day, 8, 0, 0);
            DateTime endDate = new DateTime(year, month, day, 9, 0, 0);

            string subject = "";
            string location = "";
            string description = "";
			
			if (edit)
            {
                int id = core.Functions.RequestInt("id", -1);

                if (id < 1)
                {
                    core.Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                }

                try
                {
                    Event calendarEvent = new Event(core, Owner, id);
                    inviteeIds.AddRange(calendarEvent.GetInvitees());

                    template.Parse("EDIT", "TRUE");
                    template.Parse("ID", calendarEvent.EventId.ToString());

                    startDate = calendarEvent.GetStartTime(core.tz);
                    endDate = calendarEvent.GetEndTime(core.tz);

                    eventAccess = calendarEvent.Permissions;

                    subject = calendarEvent.Subject;
                    location = calendarEvent.Location;
                    description = calendarEvent.Description;
                }
                catch (InvalidEventException)
                {
                    core.Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                }
            }

            SelectBox yearsStartSelectBox = new SelectBox("start-year");
            SelectBox yearsEndSelectBox = new SelectBox("end-year");

            for (int i = DateTime.Now.AddYears(-110).Year; i < DateTime.Now.AddYears(110).Year; i++)
            {
                yearsStartSelectBox.Add(new SelectBoxItem(i.ToString(), i.ToString()));
                yearsEndSelectBox.Add(new SelectBoxItem(i.ToString(), i.ToString()));
            }

            yearsStartSelectBox.SelectedKey = startDate.Year.ToString();
            yearsEndSelectBox.SelectedKey = endDate.Year.ToString();

            SelectBox monthsStartSelectBox = new SelectBox("start-month");
            SelectBox monthsEndSelectBox = new SelectBox("end-month");

            for (int i = 1; i < 13; i++)
            {
                monthsStartSelectBox.Add(new SelectBoxItem(i.ToString(), core.Functions.IntToMonth(i)));
                monthsEndSelectBox.Add(new SelectBoxItem(i.ToString(), core.Functions.IntToMonth(i)));
            }

            monthsStartSelectBox.SelectedKey = startDate.Month.ToString();
            monthsEndSelectBox.SelectedKey = endDate.Month.ToString();

            SelectBox daysStartSelectBox = new SelectBox("start-day");
            SelectBox daysEndSelectBox = new SelectBox("end-day");

            for (int i = 1; i < 32; i++)
            {
                daysStartSelectBox.Add(new SelectBoxItem(i.ToString(), i.ToString()));
                daysEndSelectBox.Add(new SelectBoxItem(i.ToString(), i.ToString()));
            }

            daysStartSelectBox.SelectedKey = startDate.Day.ToString();
            daysEndSelectBox.SelectedKey = endDate.Day.ToString();

            SelectBox hoursStartSelectBox = new SelectBox("start-hour");
            SelectBox hoursEndSelectBox = new SelectBox("end-hour");

            for (int i = 0; i < 24; i++)
            {
                DateTime hourTime = new DateTime(year, month, day, i, 0, 0);
                hoursStartSelectBox.Add(new SelectBoxItem(i.ToString(), hourTime.ToString("h tt").ToLower()));
                hoursEndSelectBox.Add(new SelectBoxItem(i.ToString(), hourTime.ToString("h tt").ToLower()));
            }

            hoursStartSelectBox.SelectedKey = startDate.Hour.ToString();
            hoursEndSelectBox.SelectedKey = endDate.Hour.ToString();

            SelectBox minutesStartSelectBox = new SelectBox("start-minute");
            SelectBox minutesEndSelectBox = new SelectBox("end-minute");

            for (int i = 0; i < 60; i++)
            {
                minutesStartSelectBox.Add(new SelectBoxItem(i.ToString(), string.Format("{0:00}", i)));
                minutesEndSelectBox.Add(new SelectBoxItem(i.ToString(), string.Format("{0:00}", i)));
            }

            minutesStartSelectBox.SelectedKey = startDate.Minute.ToString();
            minutesEndSelectBox.SelectedKey = endDate.Minute.ToString();

            template.Parse("S_YEAR", year.ToString());
            template.Parse("S_MONTH", month.ToString());
            template.Parse("S_DAY", day.ToString());


            template.Parse("S_START_YEAR", yearsStartSelectBox);
            template.Parse("S_END_YEAR", yearsEndSelectBox);

            template.Parse("S_START_MONTH", monthsStartSelectBox);
            template.Parse("S_END_MONTH", monthsEndSelectBox);

            template.Parse("S_START_DAY", daysStartSelectBox);
            template.Parse("S_END_DAY", daysEndSelectBox);

            template.Parse("S_START_HOUR", hoursStartSelectBox);
            template.Parse("S_END_HOUR", hoursEndSelectBox);

            template.Parse("S_START_MINUTE", minutesStartSelectBox);
            template.Parse("S_END_MINUTE", minutesEndSelectBox);

            List<string> permissions = new List<string>();
            permissions.Add("Can Read");

            core.Display.ParsePermissionsBox(template, "S_EVENT_PERMS", eventAccess, permissions);

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

                outputInvitees.Append(core.UserProfiles[inviteeId].DisplayName);
                outputInviteesIds.Append(inviteeId.ToString());
            }

            if (outputInvitees != null)
            {
                template.Parse("S_INVITEES", outputInvitees.ToString());
            }

            if (outputInviteesIds != null)
            {
                template.Parse("INVITEES_ID_LIST", outputInviteesIds.ToString());
            }

            Save(new EventHandler(AccountCalendarEventNew_Save));
        }

        void AccountCalendarEventNew_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long eventId = 0;
            string subject = "";
            string location = "";
            string description = "";
            DateTime startTime = tz.Now;
            DateTime endTime = tz.Now;
            bool edit = false;

            if (core.Http.Form["mode"] == "edit")
            {
                edit = true;
            }

            string inviteeIdList = core.Http.Form["inviteeses"];
            string inviteeUsernameList = core.Http.Form["invitees"];
            List<long> inviteeIds = new List<long>();

            if (!(string.IsNullOrEmpty(inviteeIdList)))
            {
                string[] inviteesIds = inviteeIdList.Split(new char[] { ' ', '\t', ';', ',' });
                foreach (string inviteeId in inviteesIds)
                {
                    try
                    {
                        inviteeIds.Add(long.Parse(inviteeId));
                    }
                    catch
                    {
                    }
                }
            }

            if (!(string.IsNullOrEmpty(inviteeUsernameList)))
            {
                string[] inviteesUsernames = inviteeUsernameList.Split(new char[] { ' ', '\t', ';', ',' });
                List<string> inviteesUsernamesList = new List<string>();

                foreach (string inviteeUsername in inviteesUsernames)
                {
                    if (!string.IsNullOrEmpty(inviteeUsername))
                    {
                        inviteesUsernamesList.Add(inviteeUsername);
                    }
                }

                inviteeIds.AddRange(core.LoadUserProfiles(inviteesUsernamesList));
            }

            try
            {
                subject = core.Http.Form["subject"];
                location = core.Http.Form["location"];
                description = core.Http.Form["description"];

                startTime = new DateTime(
                    int.Parse(core.Http.Form["start-year"]),
                    int.Parse(core.Http.Form["start-month"]),
                    int.Parse(core.Http.Form["start-day"]),
                    int.Parse(core.Http.Form["start-hour"]),
                    int.Parse(core.Http.Form["start-minute"]),
                    0);

                endTime = new DateTime(
                    int.Parse(core.Http.Form["end-year"]),
                    int.Parse(core.Http.Form["end-month"]),
                    int.Parse(core.Http.Form["end-day"]),
                    int.Parse(core.Http.Form["end-hour"]),
                    int.Parse(core.Http.Form["end-minute"]),
                    0);

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
                Event calendarEvent = Event.Create(core, LoggedInMember, Owner, subject, location, description, tz.GetUnixTimeStamp(startTime), tz.GetUnixTimeStamp(endTime));

                foreach (long inviteeId in inviteeIds)
                {
					try
					{
						calendarEvent.Invite(core, core.UserProfiles[inviteeId]);
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
                Event calendarEvent = new Event(core, Owner, eventId);
                calendarEvent.Location = location;
                calendarEvent.Subject = subject;
                calendarEvent.Description = description;
                calendarEvent.StartTimeRaw = tz.GetUnixTimeStamp(startTime);
                calendarEvent.EndTimeRaw = tz.GetUnixTimeStamp(endTime);
                
                calendarEvent.Update();
				
				List<long> alreadyInvited = calendarEvent.GetInvitees();
				
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
						calendarEvent.Invite(core, core.UserProfiles[inviteeId]);
					}
					catch (CouldNotInviteEventException)
					{
					}
                }

                SetRedirectUri(Event.BuildEventUri(core, calendarEvent));
                core.Display.ShowMessage("Event Saved", "You have successfully saved your changes to the event.");
            }
        }
    }
}
