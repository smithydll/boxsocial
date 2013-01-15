﻿/*
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
    public class TextBox : FormField
    {
        private string value;
        protected bool disabled;
        protected bool visible;
        private int maxLength;
        private bool isFormatted;
        private int lines;
        private StyleLength width;
        private ScriptProperty script;

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
                visible = value;
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

        public ScriptProperty Script
        {
            get
            {
                return script;
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
            script = new ScriptProperty();
        }

        public override string ToString()
        {
            if (isFormatted)
            {
                return string.Format("<textarea id=\"{0}\" name=\"{0}\" style=\"margin: 0px; width: {6}; height: {3}px;{5}\" cols=\"70\" rows=\"{2}\"{4}{7}>{1}</textarea><div class=\"bbcode-enabled\"></div>",
                    HttpUtility.HtmlEncode(name),
                    HttpUtility.HtmlEncode(Value),
                    lines,
                    17 * lines,
                    (IsDisabled) ? " disabled=\"disabled\"" : string.Empty,
                    (!IsVisible) ? " display: none;" : string.Empty,
                    width,
                    Script.ToString());
            }
            else
            {
                return string.Format("<input type=\"text\" name=\"{0}\" id=\"{0}\" value=\"{1}\" style=\"width: {5};{4}\"{2}{3}{6}/>",
                    HttpUtility.HtmlEncode(name),
                    HttpUtility.HtmlEncode(Value),
                    (IsDisabled) ? " disabled=\"disabled\"" : string.Empty,
                    (MaxLength > -1) ? " maxlength=\"" + MaxLength.ToString() + "\"" : string.Empty,
                    (!IsVisible) ? " display: none;" : string.Empty,
                    width,
                    Script.ToString());
            }
        }

        public override void SetValue(string value)
        {
            this.Value = value;
        }
    }
}
