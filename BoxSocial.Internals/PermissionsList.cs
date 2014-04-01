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
using System.Collections.Generic;
using System.Text;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class PermissionsList
    {
        private Core core;
        private List<string> permissions;

        public PermissionsList(Core core)
        {
            this.core = core;
            permissions = new List<string>(8);
        }

        public void Add(string permission, bool can)
        {
            if (can)
            {
                permissions.Add(core.Bbcode.Parse(string.Format(permission, core.Prose.GetString("CAN"))));
            }
            else
            {
                permissions.Add(core.Bbcode.Parse(string.Format(permission, core.Prose.GetString("CANNOT"))));
            }
        }

        public void Add(string permission, bool can, params string[] args)
        {
            List<string> newArgs = new List<string>(8);

            if (can)
            {
                newArgs.Add(core.Prose.GetString("CAN"));
                
            }
            else
            {
                newArgs.Add(core.Prose.GetString("CANNOT"));
            }

            newArgs.AddRange(args);
            permissions.Add(core.Bbcode.Parse(string.Format(permission, newArgs.ToArray())));
        }

        public void Parse(string variable)
        {
            core.Template.ParseRaw(variable, this.ToString());
        }

        public override string ToString()
        {
            string returnValue = "<ul class=\"permissions-block\">";

            foreach (string permission in permissions)
            {

                returnValue += string.Format("<li>{0}</li>", permission);
            }

            returnValue += "</ul>";

            return returnValue;
        }
    }
}
