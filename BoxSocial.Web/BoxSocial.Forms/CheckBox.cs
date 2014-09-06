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
    public class CheckBox : FormField
    {
        private string caption;
        private bool isChecked;
        private bool disabled;
        private StyleLength width;
        private ScriptProperty script;
        private string icon;
        private string cssClass;

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

        public string Icon
        {
            get
            {
                return icon;
            }
            set
            {
                icon = value;
            }
        }

        public bool IsChecked
        {
            get
            {
                return isChecked;
            }
            set
            {
                isChecked = value;
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

        public ScriptProperty Script
        {
            get
            {
                return script;
            }
        }

        public string Class
        {
            get
            {
                return cssClass;
            }
            set
            {
                cssClass = value;
            }
        }

        public CheckBox(string name)
        {
            this.name = name;

            isChecked = false;
            disabled = false;
            width = new StyleLength(100F, LengthUnits.Percentage);
            script = new ScriptProperty();
            cssClass = string.Empty;
        }

        public override string ToString()
        {
            return ToString(Forms.DisplayMedium.Desktop);
        }

        public override string ToString(DisplayMedium medium)
        {
            return string.Format("<input type=\"checkbox\" name=\"{0}\" id=\"{0}\" style=\"{3}\" {1}{2}{5}{6}/>{4}",
                HttpUtility.HtmlEncode(name),
                (IsChecked) ? "checked=\"checked\" " : "",
                (IsDisabled) ? "disabled=\"disabled\" " : "",
                width.Length > 0 ? "width: " + width + ";" : "",
                !string.IsNullOrEmpty(caption) ? string.Format("<label for=\"{0}\">{1}</label>", HttpUtility.HtmlEncode(name), HttpUtility.HtmlEncode(caption)) : string.Empty,
                Script.ToString(),
                !string.IsNullOrEmpty(cssClass) ? string.Format("class=\"{0}\" ", cssClass) : string.Empty);
        }

        public override void SetValue(string value)
        {
            if (value == "checked")
            {
                isChecked = true;
            }
            else
            {
                isChecked = false;
            }
        }
    }
}
