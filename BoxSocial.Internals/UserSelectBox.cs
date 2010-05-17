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
using System.Globalization;
using System.Text;
using System.Web;
using BoxSocial.Forms;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class UserSelectBox : FormField
    {
        private Core core;
        private List<long> userIds;

        public List<long> Invitees
        {
            get
            {
                return userIds;
            }
            set
            {
                userIds = value;
            }
        }

        public UserSelectBox(Core core, string name)
        {
            this.core = core;
            this.name = name;

            userIds = new List<long>();
        }

        public UserSelectBox(Core core, string name, List<long> userIds)
        {
            this.core = core;
            this.name = name;
            this.userIds = userIds;
        }

        public override string ToString()
        {
            core.PrimitiveCache.LoadUserProfiles(userIds);

            TextBox rawNamesTextBox = new TextBox(name + "[raw]");
            rawNamesTextBox.Width = new StyleLength(100, LengthUnits.Percentage);
            rawNamesTextBox.Script.OnKeyUp = "PickUserName('" + name + "[raw]')";

            string userIdList = string.Empty;
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div class=\"username-field\">");
            sb.AppendLine("<p id=\"" + name + "[list]\">");

            foreach (long userId in userIds)
            {
                sb.Append("<span class=\"username-name\">" + core.PrimitiveCache[userId].DisplayName + "</span>");

                if (userIdList != string.Empty)
                {
                    userIdList += ",";
                }
                userIdList += userId.ToString();
            }
            sb.Append("</p>");

            sb.AppendLine("<p>");
            sb.Append(rawNamesTextBox.ToString());
            sb.Append("<ul id=\"" + name + "[dropbox]\" class=\"username-dropbox\" style=\"display: none;\">");
            sb.Append("</ul>");
            sb.Append("</p>");

            sb.AppendLine("</div>");

            sb.AppendLine(string.Format("<input type=\"hidden\" name=\"{0}\" id=\"{0}\" value=\"{1}\" />",
                name + "[ids]", userIdList));

            return sb.ToString();
        }

        public static List<long> FormUsers(Core core, string name)
        {
            List<long> userIds = new List<long>();

            return userIds;
        }
    }
}
