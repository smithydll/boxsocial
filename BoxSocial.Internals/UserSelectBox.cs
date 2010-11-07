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
        private bool selectMultiple = true;
        private List<long> userIds;
        private StyleLength width;

        public bool SelectMultiple
        {
            get
            {
                return selectMultiple;
            }
            set
            {
                selectMultiple = value;
            }
        }

        public StyleLength Width
        {
            get
            {
                return width;
            }
            set
            {
                width = value;
            }
        }

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
            width = new StyleLength(100F, LengthUnits.Percentage);
        }

        public UserSelectBox(Core core, string name, List<long> userIds)
        {
            this.core = core;
            this.name = name;
            this.userIds = userIds;

            width = new StyleLength(100F, LengthUnits.Percentage);
        }

        public override string ToString()
        {
            core.PrimitiveCache.LoadUserProfiles(userIds);

            TextBox rawNamesTextBox = new TextBox(name + "[raw]");
            rawNamesTextBox.Width = width;
            rawNamesTextBox.Script.OnKeyUp = "PickUserName('" + name + "[raw]', " + (!selectMultiple).ToString().ToLower() + ")";

            HiddenField idsHiddenField = new HiddenField(name + "[ids]");

            string userIdList = string.Empty;
            StringBuilder sb = new StringBuilder();

            if (SelectMultiple)
            {
                sb.AppendLine("<div class=\"username-field\">");
                sb.AppendLine("<p id=\"" + name + "[list]\">");
            }
            else
            {
                sb.AppendLine("<span class=\"username-field\">");
                sb.AppendLine("<span id=\"" + name + "[list]\">");
            }

            foreach (long userId in userIds)
            {
                sb.Append("<span id=\"" + name + "[" + userId.ToString() + "]\" class=\"username-name\">" + core.PrimitiveCache[userId].DisplayName + "&nbsp;<a onclick=\"RemoveName('" + name + "','" + userId.ToString() + "')\">X</a></span>");

                if (userIdList != string.Empty)
                {
                    userIdList += ",";
                }
                userIdList += userId.ToString();
            }

            idsHiddenField.Value = userIdList;

            if (SelectMultiple)
            {
                sb.Append("</p>");

                sb.AppendLine("<p>");
            }
            else
            {
                sb.Append("</span>&nbsp;<span>");
            }
            sb.Append(rawNamesTextBox.ToString());
            sb.Append("<ul id=\"" + name + "[dropbox]\" class=\"username-dropbox\" style=\"display: none;\">");
            sb.Append("</ul>");
            if (SelectMultiple)
            {
                sb.Append("</p>");
                sb.AppendLine("</div>");
            }
            else
            {
                sb.Append("</span>");
                sb.Append("</span>");
            }

            sb.AppendLine(idsHiddenField.ToString());

            return sb.ToString();
        }

        public static List<long> FormUsers(Core core, string name)
        {
            List<long> userIds = new List<long>();

            string formValue = core.Http.Form[name + "[ids]"];
            if (!string.IsNullOrEmpty(formValue))
            {
                string[] ids = formValue.Split(new char[] { ',' });

                foreach (string idString in ids)
                {
                    long id;
                    long.TryParse(idString, out id);

                    if (id > 0)
                    {
                        userIds.Add(id);
                    }
                }
            }

            int limit = 10;
            string userNames = core.Http.Form[name + "[raw]"];

            if (!string.IsNullOrEmpty(userNames))
            {
                string[] usernames = userNames.Split(new char[] { ',', ';', ' ' });

                foreach (string username in usernames)
                {
                    if (limit > 0)
                    {
                        long id = core.PrimitiveCache.LoadUserProfile(username);
                        userIds.Add(id);
                        limit--;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return userIds;
        }

        public override void SetValue(string value)
        {
            userIds = FormUsers(core, value);
        }
    }
}
