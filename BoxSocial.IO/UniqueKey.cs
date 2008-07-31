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
using System.Collections.Generic;
using System.Text;

namespace BoxSocial.IO
{
    public class UniqueKey
    {
        private string key;

        public string Key
        {
            get
            {
                return key;
            }
        }

        public UniqueKey(string key)
        {
            this.key = key;
        }

        public override bool Equals(object obj)
        {
            if (obj is UniqueKey)
            {
                if (((UniqueKey)obj).key == key)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static List<string> GetKeys(List<DataFieldInfo> fields)
        {
            List<string> keys = new List<string>();

            foreach (DataFieldInfo field in fields)
            {
                if (field.Key != null)
                {
                    if (!keys.Contains(field.Key.Key))
                    {
                        keys.Add(field.Key.Key);
                    }
                }
            }

            return keys;
        }

        public static List<DataFieldInfo> GetFields(string key, List<DataFieldInfo> fields)
        {
            List<DataFieldInfo> keyFields = new List<DataFieldInfo>();

            foreach (DataFieldInfo field in fields)
            {
                if (field.Key != null)
                {
                    if (field.Key.Key == key)
                    {
                        keyFields.Add(field);
                    }
                }
            }

            return keyFields;
        }
    }
}
