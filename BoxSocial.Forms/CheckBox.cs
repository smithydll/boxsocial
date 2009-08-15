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
using System.Text;

namespace BoxSocial.Forms
{
    public class CheckBox : FormField
    {
        private string caption;
        private bool isChecked;
        private bool disabled;

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

        public CheckBox(string name)
        {
            this.name = name;

            isChecked = false;
            disabled = false;
        }

        public override string ToString()
        {
            return string.Format("<input type=\"checkbox\" name=\"{0}\" id = \"{0}\" value=\"{1}\" style=\"width: 100%;\" {2}{3}/>",
                name, Value, (IsChecked) ? "checked=\"checked\" " : "", (IsDisabled) ? "disabled=\"disabled\" " : "");
        }
    }
}
