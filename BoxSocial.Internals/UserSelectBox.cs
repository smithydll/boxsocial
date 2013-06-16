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
        protected bool visible;
        protected bool disabled;
        private ScriptProperty script;

        public bool IsDisabled
        {
            get
            {
                return disabled;
            }
            set
            {
                disabled = value;
            }
        }

        public bool IsVisible
        {
            get
            {
                return visible;
            }
            set
            {
                visible = value;
            }
        }

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

            disabled = false;
            visible = true;

            userIds = new List<long>();
            width = new StyleLength(100F, LengthUnits.Percentage);
            script = new ScriptProperty();
        }

        public UserSelectBox(Core core, string name, List<long> userIds)
        {
            this.core = core;
            this.name = name;
            this.userIds = userIds;

            disabled = false;
            visible = true;

            width = new StyleLength(100F, LengthUnits.Percentage);
            script = new ScriptProperty();
        }

        public void AddUserId(long userId)
        {
            this.userIds.Add(userId);
        }

        public override string ToString()
        {
            core.PrimitiveCache.LoadUserProfiles(userIds);

            StringBuilder users = new StringBuilder();
            StringBuilder idList = new StringBuilder();

            bool first = true;
            foreach (long userId in userIds)
            {
                if (!first)
                {
                    idList.Append(",");
                }
                else
                {
                    first = false;
                }
                idList.Append(userId.ToString());

                users.Append(string.Format("<span class=\"username\">{1}<span class=\"delete\" onclick=\"rvl($(this).parent().siblings('.ids'),'{0}'); $(this).parent().remove();\">x</span><input type=\"hidden\" id=\"user-{0}\" name=\"user[{0}]\" value=\"{0}\" /></span>", userId, core.PrimitiveCache[userId].DisplayName));
            }

            return string.Format("<div id=\"{0}\" class=\"user-droplist\" onclick=\"$(this).children('.textbox').focus();\" style=\"width: {4};{3}\">{6}<input type=\"text\" name=\"{0}-text\" id=\"{0}-text\" {8}value=\"{1}\" class=\"textbox\" style=\"\"{2}{5}/><input type=\"hidden\" name=\"{0}-ids\" id=\"{0}-ids\" class=\"ids\" value=\"{7}\"/></div>",
                    HttpUtility.HtmlEncode(name),
                    HttpUtility.HtmlEncode(string.Empty),
                    (IsDisabled) ? " disabled=\"disabled\"" : string.Empty,
                    (!IsVisible) ? " display: none;" : string.Empty,
                    width,
                    Script.ToString(),
                    users.ToString(),
                    idList.ToString(),
                    (core.IsMobile) ? "data-role=\"none\" " : "");
        }

        public static List<long> FormUsers(Core core, string name)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            List<long> userIds = new List<long>();

            string formValue = core.Http.Form[name + "-ids"];
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
            string userNames = core.Http.Form[name + "-text"];

            if (!string.IsNullOrEmpty(userNames))
            {
                string[] usernames = userNames.Split(new char[] { ',', ';', ' ' });

                foreach (string username in usernames)
                {
                    if (limit > 0)
                    {
                        long id = core.PrimitiveCache.LoadUserProfile(username);
                        if (id > 0)
                        {
                            userIds.Add(id);
                            limit--;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return userIds;
        }

        public ScriptProperty Script
        {
            get
            {
                return script;
            }
        }

        public override void SetValue(string value)
        {
            userIds = FormUsers(core, value);
        }
    }
}
