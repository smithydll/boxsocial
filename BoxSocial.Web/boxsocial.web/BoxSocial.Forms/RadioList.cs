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
    public enum Layout
    {
        Horizontal,
        Vertical,
        TwoColumn,
        ThreeColumn,
    }

    public class RadioList : FormField
    {
        private List<RadioListItem> items;
        private Dictionary<string, RadioListItem> itemKeys;
        private string selectedKey;
        private Layout layout;

        /// <summary>
        /// Selected item.
        /// </summary>
        public string SelectedKey
        {
            get
            {
                return selectedKey;
            }
            set
            {
                if (itemKeys.ContainsKey(value))
                {
                    // unselect previous selected
                    foreach (RadioListItem item in items)
                    {
                        item.selected = false;
                    }

                    // select the newly selected
                    selectedKey = value;
                    itemKeys[value].selected = true;
                }
            }
        }

        public Layout Layout
        {
            get
            {
                return layout;
            }
            set
            {
                layout = value;
            }
        }

        /// <summary>
        /// Radio List constructor.
        /// </summary>
        /// <param name="name">Radio list name</param>
        public RadioList(string name)
        {
            this.name = name;
            layout = Forms.Layout.Vertical;

            items = new List<RadioListItem>();
            itemKeys = new Dictionary<string, RadioListItem>();
        }

        /// <summary>
        /// Get a radio list item.
        /// </summary>
        /// <param name="key">Item key</param>
        /// <returns>The select box item with the key</returns>
        public RadioListItem this[string key]
        {
            get
            {
                return itemKeys[key];
            }
        }

        /// <summary>
        /// Add a radio list item to the select box.
        /// </summary>
        /// <param name="item"></param>
        public void Add(RadioListItem item)
        {
            if (!itemKeys.ContainsKey(item.Key))
            {
                items.Add(item);
                itemKeys.Add(item.Key, item);
            }
        }

        public void Add(string key, string text)
        {
            RadioListItem item = new RadioListItem(Name, key, text);
            if (!itemKeys.ContainsKey(item.Key))
            {
                items.Add(item);
                itemKeys.Add(item.Key, item);
            }
        }

        public void Add(string key, string text, string icon)
        {
            RadioListItem item = new RadioListItem(Name, key, text, icon);
            if (!itemKeys.ContainsKey(item.Key))
            {
                items.Add(item);
                itemKeys.Add(item.Key, item);
            }
        }

        public void Add(string key, string text, bool selectable)
        {
            RadioListItem item = new RadioListItem(Name, key, text, selectable);
            if (!itemKeys.ContainsKey(item.Key))
            {
                items.Add(item);
                itemKeys.Add(item.Key, item);
            }
        }

        public void Add(string key, string text, string icon, bool selectable)
        {
            RadioListItem item = new RadioListItem(Name, key, text, icon, selectable);
            if (!itemKeys.ContainsKey(item.Key))
            {
                items.Add(item);
                itemKeys.Add(item.Key, item);
            }
        }

        /// <summary>
        /// Returns true is contains the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key)
        {
            return itemKeys.ContainsKey(key);
        }

        public override string ToString()
        {
            return ToString(Forms.DisplayMedium.Desktop);
        }

        /// <summary>
        /// Creates a string representing the XHTML syntax for the radio list.
        /// </summary>
        /// <returns>Returns XHTML</returns>
        public override string ToString(DisplayMedium medium)
        {
            StringBuilder selectBox = new StringBuilder();

            switch (layout)
            {
                case Forms.Layout.Vertical:
                    selectBox.AppendLine(string.Format("<ul id=\"rl-" + HttpUtility.HtmlEncode(name) + "\">",
                        name));

                    foreach (RadioListItem item in items)
                    {
                        selectBox.AppendLine("  <li>" + item.ToString() + "</li>");
                    }

                    selectBox.AppendLine("</ul>");
                    break;
                case Forms.Layout.Horizontal:
                    selectBox.AppendLine(string.Format("<span id=\"rl-" + HttpUtility.HtmlEncode(name) + "\">",
                        name));
                    foreach (RadioListItem item in items)
                    {
                        selectBox.AppendLine("  <span>" + item.ToString() + "</span>");
                    }
                    selectBox.AppendLine("</span>");
                    break;
            }

            return selectBox.ToString();
        }

        public override void SetValue(string value)
        {
            SelectedKey = value;
        }
    }
}
