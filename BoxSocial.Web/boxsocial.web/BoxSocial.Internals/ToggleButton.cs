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
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Forms;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class ToggleButton : FormField
    {
        private Core core;
        private List<RadioListItem> items;
        private RadioListItem yesRadioListItem;
        private RadioListItem noRadioListItem;
        private bool value;

        public bool Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
            }
        }

        public ToggleButton(Core core, string name)
        {
            this.core = core;
            this.name = name;
            this.value = false;

            this.yesRadioListItem = new RadioListItem(name, "1", core.Prose.GetString("YES"));
            this.noRadioListItem = new RadioListItem(name, "0", core.Prose.GetString("NO"));
        }

        public override string ToString()
        {
            return ToString(Forms.DisplayMedium.Desktop);
        }

        /// <summary>
        /// Creates a string representing the XHTML syntax for the radio list.
        /// </summary>
        /// <returns>Returns XHTML</returns>
        public override string ToString(DisplayMedium medium)
        {
            StringBuilder selectBox = new StringBuilder();
            selectBox.Append("<span id=\"" + name + "\" class=\"toggle-field\">");
            selectBox.Append(yesRadioListItem.ToString());
            selectBox.Append(noRadioListItem.ToString());
            selectBox.Append("</span>");

            return selectBox.ToString();
        }

        public override void SetValue(string value)
        {
            this.value = (value == "1");
        }
    }
}
