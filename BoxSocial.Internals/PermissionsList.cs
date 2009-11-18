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
            permissions = new List<string>();
        }

        public void Add(string permission, bool can)
        {
            if (can)
            {
                permissions.Add(core.Bbcode.Parse(string.Format(permission, core.prose.GetString("CAN"))));
            }
            else
            {
                permissions.Add(core.Bbcode.Parse(string.Format(permission, core.prose.GetString("CANNOT"))));
            }
        }

        public void Add(string permission, bool can, params string[] args)
        {
            List<string> newArgs = new List<string>();

            if (can)
            {
                newArgs.Add(core.prose.GetString("CAN"));
                
            }
            else
            {
                newArgs.Add(core.prose.GetString("CANNOT"));
            }

            newArgs.AddRange(args);
            permissions.Add(core.Bbcode.Parse(string.Format(permission, newArgs.ToArray())));
        }

        public void Parse(string variable)
        {
            core.template.ParseRaw(variable, this.ToString());
        }

        public override string ToString()
        {
            string returnValue = "<ul>";

            foreach (string permission in permissions)
            {

                returnValue += string.Format("<li>{0}</li>", permission);
            }

            returnValue += "</ul>";

            return returnValue;
        }
    }
}
