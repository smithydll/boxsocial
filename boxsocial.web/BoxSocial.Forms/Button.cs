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
    public class Button : FormField
    {
        private bool disabled;
        protected bool visible;
        private string caption;
        private string value;

        public string Caption
        {
            get
            {
                return caption;
            }
            set
            {
                caption = value;
            }
        }

        public string Value
        {
            get
            {
                return value;
            }
            set
            {
                value = value;
            }
        }

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

        public Button(string name, string caption, string value)
        {
            this.name = name;
            this.caption = caption;
            this.value = value;

            disabled = false;
            visible = true;
        }

        public override string ToString()
        {
            return ToString(Forms.DisplayMedium.Desktop);
        }

        public override string ToString(DisplayMedium medium)
        {
            return string.Format("<button name=\"{0}\" value=\"{1}\" style=\"{4}\"{3}>{2}</button>",
                HttpUtility.HtmlEncode(name),
                HttpUtility.HtmlEncode(value),
                HttpUtility.HtmlEncode(caption),
                (IsDisabled) ? " disabled=\"disabled\"" : "",
                (!IsVisible) ? " display: none;" : "");
        }

        public override void SetValue(string value)
        {
            // Do Nothing
        }
    }
}
