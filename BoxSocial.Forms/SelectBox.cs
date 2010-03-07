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
    /// Represents a select box on an XHTML form.
    /// </summary>
    public sealed class SelectBox : FormField, IEnumerable
    {
        private List<SelectBoxItem> items;
        private Dictionary<string, SelectBoxItem> itemKeys;
        private string selectedKey;
        protected bool visible;

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
                    foreach (SelectBoxItem item in items)
                    {
                        item.selected = false;
                    }

                    // select the newly selected
                    selectedKey = value;
                    itemKeys[value].selected = true;
                }
            }
        }

        /// <summary>
        /// Select box constructor.
        /// </summary>
        /// <param name="name">Select box name</param>
        public SelectBox(string name)
        {
            this.name = name;

            items = new List<SelectBoxItem>();
            itemKeys = new Dictionary<string, SelectBoxItem>();
            visible = true;
        }

        /// <summary>
        /// Get a select box item.
        /// </summary>
        /// <param name="key">Item key</param>
        /// <returns>The select box item with the key</returns>
        public SelectBoxItem this[string key]
        {
            get
            {
                return itemKeys[key];
            }
        }

        /// <summary>
        /// Add a select box item to the select box.
        /// </summary>
        /// <param name="item"></param>
        public void Add(SelectBoxItem item)
        {
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

        /// <summary>
        /// Creates a string representing the XHTML syntax for the select box.
        /// </summary>
        /// <returns>Returns XHTML</returns>
        public override string ToString()
        {
            StringBuilder selectBox = new StringBuilder();
            selectBox.AppendLine(string.Format("<select name=\"{0}\" id=\"{0}\" style=\"{1}\">",
                HttpUtility.HtmlEncode(name),
                (!IsVisible) ? " display: none;" : ""));

            foreach (SelectBoxItem item in items)
            {
                selectBox.AppendLine(item.ToString());
            }

            selectBox.AppendLine("</select>");

            return selectBox.ToString();
        }
        
        public IEnumerator GetEnumerator ()
        {
           return items.GetEnumerator();
        }

        public override int GetHashCode ()
        {
            return items.GetHashCode();
        }

    }
}
