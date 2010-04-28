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
using System.Web;

namespace BoxSocial.Forms
{
    public class TextBox : FormField
    {
        private string value;
        private bool disabled;
        protected bool visible;
        private int maxLength;
        private bool isFormatted;
        private int lines;
        private StyleLength width;

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

        public bool IsVisible
        {
            get
            {
                return visible;
            }
            set
            {
                visible = false;
            }
        }

        /// <summary>
        /// True if formatted for BBcode
        /// </summary>
        public bool IsFormatted
        {
            get
            {
                return isFormatted;
            }
            set
            {
                isFormatted = value;
                if (isFormatted)
                {
                    MaxLength = -1;
                    lines = 15;
                }
                else
                {
                    lines = 1;
                }
            }
        }

        public int MaxLength
        {
            get
            {
                return maxLength;
            }
            set
            {
                maxLength = value;

                // Update the value
                if (maxLength > -1)
                {
                    if (!string.IsNullOrEmpty(this.value))
                    {
                        if (this.value.Length > maxLength)
                        {
                            this.value = this.value.Substring(0, maxLength);
                        }
                    }
                }
            }
        }

        public int Lines
        {
            get
            {
                return lines;
            }
            set
            {
                lines = value;
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

        public TextBox(string name)
        {
            this.name = name;

            disabled = false;
            visible = true;
            maxLength = -1;
            lines = 1;
            width = new StyleLength(100F, LengthUnits.Percentage);
        }

        public override string ToString()
        {
            if (isFormatted)
            {
                return string.Format("<textarea id=\"{0}\" name=\"{0}\" style=\"margin: 0px; width: {6}; height: {3}px; border: solid 1px #666666;{5}\" cols=\"70\" rows=\"{2}\"{4}>{1}</textarea><div style=\"background-image: url('/images/tab_shadow.png'); background-repeat: repeat-x; position: relative; top: -2px; margin-left: 77px;\"><div style=\"background-image: url('/images/bbcode_tab.png'); width: 77px; height: 18px; margin: 0px; padding: 0px; margin-left: -77px;\"></div></div>",
                    HttpUtility.HtmlEncode(name),
                    HttpUtility.HtmlEncode(Value),
                    lines,
                    17 * lines,
                    (IsDisabled) ? " disabled=\"disabled\"" : string.Empty,
                    (!IsVisible) ? " display: none;" : string.Empty,
                    width);
            }
            else
            {
                return string.Format("<input type=\"text\" name=\"{0}\" id = \"{0}\" value=\"{1}\" style=\"width: {5};{4}\"{2}{3}/>",
                    HttpUtility.HtmlEncode(name),
                    HttpUtility.HtmlEncode(Value),
                    (IsDisabled) ? " disabled=\"disabled\"" : string.Empty,
                    (MaxLength > -1) ? " maxlength=\"" + MaxLength + "\"" : string.Empty,
                    (!IsVisible) ? " display: none;" : string.Empty,
                    width);
            }
        }
    }
}
