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

namespace BoxSocial.Internals
{
    public class PerformanceMeter
    {
        public long last;
        private List<string> events;

        public PerformanceMeter()
        {
            last = DateTime.Now.Ticks;
            events = new List<string>();
            Add("Initialised Counter");
        }

        public void Add(string eventName)
        {
            events.Add(string.Format("{1}\t{2}",
                DateTime.Now.Ticks / 10000000.0, (DateTime.Now.Ticks - last) / 10000000.0, eventName));
            last = DateTime.Now.Ticks;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (string s in events)
            {
                sb.AppendLine(s + "<br />");
            }

            return sb.ToString();
        }
    }
}
