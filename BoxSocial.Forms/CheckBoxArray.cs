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
    public class CheckBoxArray : FormField
    {
        private List<CheckBox> items;
        private Dictionary<string, CheckBox> itemKeys;
        private bool disabled;
        private int columns;

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

        public int Columns
        {
            get
            {
                return columns;
            }
            set
            {
                columns = value;
            }
        }

        public CheckBoxArray(string name)
        {
            this.name = name;

            columns = 1;
            disabled = false;
            items = new List<CheckBox>();
            itemKeys = new Dictionary<string, CheckBox>();
        }

        public void Add(CheckBox item)
        {
            if (!itemKeys.ContainsKey(item.Name))
            {
                items.Add(item);
                itemKeys.Add(item.Name, item);
            }
        }

        public override string ToString()
        {
            StringBuilder checkBoxArray = new StringBuilder();
            checkBoxArray.AppendLine(string.Format("<ul id=\"cl-" + name + "\">",
                name));

            foreach (CheckBox item in items)
            {
                checkBoxArray.AppendLine("<li>" + item.ToString() + "</li>");
            }

            checkBoxArray.AppendLine("</ul>");

            return checkBoxArray.ToString();
        }
    }
}
