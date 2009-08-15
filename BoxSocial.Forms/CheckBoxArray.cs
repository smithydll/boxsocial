﻿/*
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

        public CheckBoxArray(string name)
        {
            this.name = name;

            disabled = false;
            items = new List<RadioListItem>();
            itemKeys = new Dictionary<string, RadioListItem>();
        }

        public void Add(CheckBox item)
        {
            if (!itemKeys.ContainsKey(item.Key))
            {
                items.Add(item);
                itemKeys.Add(item.Key, item);
            }
        }
    }
}
