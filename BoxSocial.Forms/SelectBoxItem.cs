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

    /// <summary>
    /// Represents a select box item on an XHTML form.
    /// </summary>
    public sealed class SelectBoxItem
    {
        private string key;
        private string text;
        private string icon;
        private bool selectable;
        internal bool selected;

        /// <summary>
        /// The select box item key.
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
        /// The select box item caption.
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
        /// The select box item icon.
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
        /// True if the select box item is selectable.
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
        /// Select box item constructor.
        /// </summary>
        /// <param name="key">Item key</param>
        /// <param name="text">Item caption</param>
        public SelectBoxItem(string key, string text)
        {
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
        public SelectBoxItem(string key, string text, string icon)
        {
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
        public SelectBoxItem(string key, string text, bool selectable)
        {
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
        public SelectBoxItem(string key, string text, string icon, bool selectable)
        {
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
                return string.Format("<option value=\"{0}\" selected=\"selected\" disabled=\"disabled\">{1}</option>",
                    HttpUtility.HtmlEncode(key), HttpUtility.HtmlEncode(text));
            }
            else if (selected)
            {
                return string.Format("<option value=\"{0}\" selected=\"selected\">{1}</option>",
                    HttpUtility.HtmlEncode(key), HttpUtility.HtmlEncode(text));
            }
            else if (!selectable)
            {
                return string.Format("<option value=\"{0}\" disabled=\"disabled\">{1}</option>",
                    HttpUtility.HtmlEncode(key), HttpUtility.HtmlEncode(text));
            }
            else
            {
                return string.Format("<option value=\"{0}\">{1}</option>",
                    HttpUtility.HtmlEncode(key), HttpUtility.HtmlEncode(text));
            }
        }
    }
}
