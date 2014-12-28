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
using System.Text;
using System.Web;

namespace BoxSocial.Forms
{
    public class SubmitButton : Button
    {

        public SubmitButton(string name, string caption)
            : base(name, caption, string.Empty)
        {
        }

        public override string ToString()
        {
            return ToString(Forms.DisplayMedium.Desktop);
        }

        public override string ToString(DisplayMedium medium)
        {
            return string.Format("<input type=\"submit\" name=\"{0}\" value=\"{1}\" style=\"{3}\"{2}/>",
                HttpUtility.HtmlEncode(Name),
                HttpUtility.HtmlEncode(Caption),
                (IsDisabled) ? " disabled=\"disabled\"" : "",
                (!IsVisible) ? " display: none;" : "");
        }
    }
}
