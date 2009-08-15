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
    public class TextBox : FormField
    {
        private string value;
        private bool disabled;
        private int maxLength;

        public string Value
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

        private int MaxLength
        {
            get
            {
                return maxLength;
            }
            set
            {
                maxLength = value;

                // Update the value
                if (this.value.Length > maxLength)
                {
                    this.value = this.value.Substring(0, maxLength);
                }
            }
        }

        public TextBox(string name)
        {
            this.name = name;

            disabled = false;
            maxLength = -1;
        }

        public override string ToString()
        {
            return string.Format("<input type=\"text\" name=\"{0}\" id = \"{0}\" value=\"{1}\" style=\"width: 100%;\" {2}/>",
                name, Value, (IsDisabled) ? "disabled=\"disabled\" " : "");
        }
    }
}
