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
    [AccountSubModule("calendar", "delete-event")]
    public class AccountCalendarEventDelete : AccountSubModule
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

        public AccountCalendarEventDelete()
        {
            this.Load += new EventHandler(AccountCalendarEventDelete_Load);
            this.Show += new EventHandler(AccountCalendarEventDelete_Show);
        }

        void AccountCalendarEventDelete_Load(object sender, EventArgs e)
        {
        }

        void AccountCalendarEventDelete_Show(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long eventId = core.Functions.RequestLong("id", 0);

            if (core.Display.GetConfirmBoxResult() != ConfirmBoxResult.None)
            {
                Save(new EventHandler(AccountCalendarEventDelete_Save));
            }
            else
            {
                if (eventId > 0)
                {
                    Dictionary<string, string> hiddenFieldList = new Dictionary<string, string>();
                    hiddenFieldList.Add("module", "calendar");
                    hiddenFieldList.Add("sub", "delete-event");
                    hiddenFieldList.Add("id", eventId.ToString());

                    core.Display.ShowConfirmBox(HttpUtility.HtmlEncode(core.Uri.AppendSid(Owner.AccountUriStub, true)), "Do you want to delete this event?", "Are you sure you want to delete this event, you cannot undo this delete operation.", hiddenFieldList);
                }
                else
                {
                    DisplayGenericError();
                    return;
                }
            }
        }

        void AccountCalendarEventDelete_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long eventId = core.Functions.FormLong("id", 0);

            if (core.Display.GetConfirmBoxResult() == ConfirmBoxResult.Yes)
            {
                if (eventId > 0)
                {
                    Event calendarEvent = new Event(core, null, eventId);

                    try
                    {
                        calendarEvent.Delete(core);

                        SetRedirectUri(core.Uri.BuildAccountModuleUri(ModuleKey));
                        core.Display.ShowMessage("Event Deleted", "You have deleted an event from your calendar.");
                    }
                    catch (NotLoggedInException)
                    {
                        core.Display.ShowMessage("Unauthorised", "You are unauthorised to delete this event.");
                    }
                }
                else
                {
                    DisplayGenericError();
                    return;
                }
            }
            else
            {
            }
        }
    }
}
