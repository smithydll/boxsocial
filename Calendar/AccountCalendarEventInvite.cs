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
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Calendar
{
    [AccountSubModule("calendar", "invite-event")]
    public class AccountCalendarEventInvite : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return null;
            }
        }

        public override int Order
        {
            get
            {
                return -1;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountCalendarEventInvite class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountCalendarEventInvite(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountCalendarEventInvite_Load);
            this.Show += new EventHandler(AccountCalendarEventInvite_Show);
        }

        void AccountCalendarEventInvite_Load(object sender, EventArgs e)
        {
            AddModeHandler("accept", new ModuleModeHandler(AccountCalendarEventInvite_StatusChanged));
            AddModeHandler("reject", new ModuleModeHandler(AccountCalendarEventInvite_StatusChanged));
            AddModeHandler("maybe", new ModuleModeHandler(AccountCalendarEventInvite_StatusChanged));
        }

        void AccountCalendarEventInvite_Show(object sender, EventArgs e)
        {
        }

        void AccountCalendarEventInvite_StatusChanged(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            long eventId = core.Functions.RequestLong("id", 0);

            if (eventId > 0)
            {
                Event calendarEvent = new Event(core, eventId);
                UpdateQuery uQuery = new UpdateQuery("event_invites");

                UpdateQuery uEventQuery = new UpdateQuery("events");
                uEventQuery.AddCondition("event_id", calendarEvent.EventId);

                switch (e.Mode)
                {
                    case "accept":
                        uQuery.AddField("invite_accepted", true);
                        uQuery.AddField("invite_status", (byte)EventAttendance.Yes);

                        uEventQuery.AddField("event_attendees", new QueryOperation("event_attendees", QueryOperations.Addition, 1));
                        break;
                    case "reject":
                        uQuery.AddField("invite_accepted", false);
                        uQuery.AddField("invite_status", (byte)EventAttendance.No);
                        break;
                    case "maybe":
                        uQuery.AddField("invite_accepted", false);
                        uQuery.AddField("invite_status", (byte)EventAttendance.Maybe);
                        break;
                    default:
                        DisplayGenericError();
                        return;
                }

                // TODO: look into this
                uQuery.AddCondition("event_id", eventId);
                uQuery.AddCondition("item_id", LoggedInMember.Id);
                uQuery.AddCondition("item_type", "USER");

                db.BeginTransaction();
                db.Query(uQuery);
                db.Query(uEventQuery);

                SetRedirectUri(calendarEvent.Uri);
                core.Display.ShowMessage("Invitation Accepted", "You have accepted the invitation to this event.");
                return;
            }
            else
            {
                core.Display.ShowMessage("Invalid submission", "You have made an invalid form submission.");
                return;
            }
        }
    }
}
