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
    public class RadioListItem : FormField
    {
        private new string name;
        private string key;
        private string text;
        private string icon;
        private bool selectable;
        internal bool selected;

        /// <summary>
        /// The radio button name.
        /// </summary>
        public new string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        /// <summary>
        /// The radio button key.
        /// </summary>
        public string Key
        {
            get
            {
                return key;
            }
            set
            {
                key = value;
            }
        }

        /// <summary>
        /// The radio button caption.
        /// </summary>
        public string Text
        {
            get
            {
                return text;
            }
            set
            {
                text = value;
            }
        }

        /// <summary>
        /// The radio button icon.
        /// </summary>
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

        /// <summary>
        /// True if the radio button is selectable.
        /// </summary>
        public bool Selectable
        {
            get
            {
                return selectable;
            }
            set
            {
                selectable = value;
            }
        }

        /// <summary>
        /// Radio button constructor.
        /// </summary>
        /// <param name="key">Item key</param>
        /// <param name="text">Item caption</param>
        public RadioListItem(string name, string key, string text)
        {
            this.name = name;
            this.key = key;
            this.text = text;
            selectable = true;
        }

        /// <summary>
        /// Select box item constructor.
        /// </summary>
        /// <param name="key">Item key</param>
        /// <param name="text">Item caption</param>
        /// <param name="icon">Item icon</param>
        public RadioListItem(string name, string key, string text, string icon)
        {
            this.name = name;
            this.key = key;
            this.text = text;
            this.icon = icon;
            selectable = true;
        }

        /// <summary>
        /// Select box item constructor.
        /// </summary>
        /// <param name="key">Item key</param>
        /// <param name="text">Item caption</param>
        /// <param name="selectable">Item selectable</param>
        public RadioListItem(string name, string key, string text, bool selectable)
        {
            this.name = name;
            this.key = key;
            this.text = text;
            this.selectable = selectable;
        }

        /// <summary>
        /// Select box item constructor.
        /// </summary>
        /// <param name="key">Item key</param>
        /// <param name="text">Item caption</param>
        /// <param name="icon">Item icon</param>
        /// <param name="selectable">Item selectable</param>
        public RadioListItem(string name, string key, string text, string icon, bool selectable)
        {
            this.name = name;
            this.key = key;
            this.text = text;
            this.icon = icon;
            this.selectable = selectable;
        }

        /// <summary>
        /// Creates a string representing the XHTML syntax for the item.
        /// </summary>
        /// <returns>Returns XHTML</returns>
        public override string ToString()
        {
            if (selected && (!selectable))
            {
                return string.Format("<input type=\"radio\" name=\"{0}\" value=\"{1}\" checked=\"checked\" disabled=\"disabled\" /> {2}",
                    name, key, text);
            }
            else if (selected)
            {
                return string.Format("<input type=\"radio\" name=\"{0}\" value=\"{1}\" checked=\"checked\" /> {2}",
                    name, key, text);
            }
            else if (!selectable)
            {
                return string.Format("<input type=\"radio\" name=\"{0}\" value=\"{1}\" disabled=\"disabled\" /> {2}",
                    name, key, text);
            }
            else
            {
                return string.Format("<input type=\"radio\" name=\"{0}\" value=\"{1}\" /> {2}",
                    name, key, text);
            }
        }
    }
}
