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
    public class ScriptProperty
    {
        private string onchange;
        private string onkeypress;
        private string onkeydown;
        private string onkeyup;
        private string onfocus;
        private string onblur;
        private string onclick;

        public string OnChange
        {
            get
            {
                return onchange;
            }
            set
            {
                onchange = value;
            }
        }

        public string OnKeyPress
        {
            get
            {
                return onkeypress;
            }
            set
            {
                onkeypress = value;
            }
        }

        public string OnKeyDown
        {
            get
            {
                return onkeydown;
            }
            set
            {
                onkeydown = value;
            }
        }

        public string OnKeyUp
        {
            get
            {
                return onkeyup;
            }
            set
            {
                onkeyup = value;
            }
        }

        public string OnFocus
        {
            get
            {
                return onfocus;
            }
            set
            {
                onfocus = value;
            }
        }

        public string OnBlur
        {
            get
            {
                return onblur;
            }
            set
            {
                onblur = value;
            }
        }

        public string OnClick
        {
            get
            {
                return onclick;
            }
            set
            {
                onclick = value;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrEmpty(onchange))
            {
                sb.Append(" onchange=\"");
                sb.Append(onchange);
                sb.Append("\"");
            }

            if (!string.IsNullOrEmpty(onkeypress))
            {
                sb.Append(" onkeypress=\"");
                sb.Append(onkeypress);
                sb.Append("\"");
            }

            if (!string.IsNullOrEmpty(onkeyup))
            {
                sb.Append(" onkeyup=\"");
                sb.Append(onkeyup);
                sb.Append("\"");
            }

            if (!string.IsNullOrEmpty(onkeydown))
            {
                sb.Append(" onkeydown=\"");
                sb.Append(onkeydown);
                sb.Append("\"");
            }

            if (!string.IsNullOrEmpty(onfocus))
            {
                sb.Append(" onfocus=\"");
                sb.Append(onfocus);
                sb.Append("\"");
            }

            if (!string.IsNullOrEmpty(onblur))
            {
                sb.Append(" onblur=\"");
                sb.Append(onblur);
                sb.Append("\"");
            }

            if (!string.IsNullOrEmpty(onclick))
            {
                sb.Append(" onclick=\"");
                sb.Append(onclick);
                sb.Append("\"");
            }

            return sb.ToString();
        }
    }
}
