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
using System.Text;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.FrontEnd
{
    public partial class functions : TPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string function = Request.Form["fun"];

            switch (function)
            {
                case "date":
                    string date = core.Functions.InterpretDate(Request.Form["date"]);
                    core.Ajax.SendRawText("date", date);
                    return;
                case "time":
                    string time = core.Functions.InterpretTime(Request.Form["time"]);
                    core.Ajax.SendRawText("time", time);
                    return;
                case "user-list":
                    ReturnUserList();
                    return;
            }
        }

        private void ReturnUserList()
        {
            string namePart = Request.Form["name-field"];

            if (core.Session.IsLoggedIn)
            {
                List<Friend> friends = core.Session.LoggedInMember.GetFriends(namePart);

                Dictionary<long, string> friendNames = new Dictionary<long, string>();

                foreach (Friend friend in friends)
                {
                    friendNames.Add(friend.Id, friend.DisplayName);
                }

                core.Ajax.SendDictionary("user-select", friendNames);
            }
        }
    }
}
