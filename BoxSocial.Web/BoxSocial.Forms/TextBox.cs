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
    public enum InputType
    {
        Text,
        Password,
        Number,
        Email,
        Url,
        Date,
        Time,
        DateTime,
        Month,
        Telephone,
    }

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
        private InputType type;

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

        public InputType Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
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
            type = InputType.Text;
        }

        public TextBox(string name, InputType type)
        {
            this.name = name;

            disabled = false;
            visible = true;
            maxLength = -1;
            lines = 1;
            width = new StyleLength(100F, LengthUnits.Percentage);
            script = new ScriptProperty();
            this.type = type;
        }

        public override string ToString()
        {
            return ToString(DisplayMedium.Desktop);
        }

        public override string ToString(DisplayMedium medium)
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
                return string.Format("<input type=\"{7}\" name=\"{0}\" id=\"{0}\" value=\"{1}\" style=\"width: {5};{4}\"{2}{3}{6}/>",
                    HttpUtility.HtmlEncode(name),
                    HttpUtility.HtmlEncode(Value),
                    (IsDisabled) ? " disabled=\"disabled\"" : string.Empty,
                    (MaxLength > -1) ? " maxlength=\"" + MaxLength.ToString() + "\"" : string.Empty,
                    (!IsVisible) ? " display: none;" : string.Empty,
                    width,
                    Script.ToString(),
                    InputTypeToString(type));
            }
        }

        public override void SetValue(string value)
        {
            this.Value = value;
        }

        private string InputTypeToString(InputType type)
        {
            switch (type)
            {
                case  InputType.Text:
                    return "text";
                case InputType.Password:
                    return "password";
                case InputType.Number:
                    return "number";
                case InputType.Email:
                    return "email";
                case InputType.Url:
                    return "url";
                case InputType.Date:
                    return "date";
                case InputType.Time:
                    return "time";
                case InputType.DateTime:
                    return "datetime";
                case InputType.Month:
                    return "month";
                case InputType.Telephone:
                    return "tel";
            }
            return "text";
        }
    }
}
