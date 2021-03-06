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
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Calendar
{
    [AccountSubModule(AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network, "calendar", "delete-event")]
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

        /// <summary>
        /// Initializes a new instance of the AccountCalendarEventDelete class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountCalendarEventDelete(Core core, Primitive owner)
            : base(core, owner)
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

                    core.Display.ShowConfirmBox(HttpUtility.HtmlEncode(core.Hyperlink.AppendSid(Owner.AccountUriStub, true)), "Do you want to delete this event?", "Are you sure you want to delete this event, you cannot undo this delete operation.", hiddenFieldList);
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
                    Event calendarEvent = new Event(core, eventId);

                    try
                    {
                        calendarEvent.Delete(core);

                        SetRedirectUri(core.Hyperlink.BuildAccountModuleUri(ModuleKey));
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
