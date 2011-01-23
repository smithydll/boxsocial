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
using System.Web;

namespace BoxSocial.IO
{
    public class Index : IComparable
    {
        protected string key;
		
		public DataFieldKeys KeyType
		{
			get
			{
				if (this.GetType() == typeof(PrimaryKey))
				{
					return DataFieldKeys.Primary;
				}
				if (this.GetType() == typeof(UniqueKey))
				{
					return DataFieldKeys.Unique;
				}
				if (this.GetType() == typeof(Index))
				{
					return DataFieldKeys.Index;
				}
				// this should never happen
				return DataFieldKeys.None;
			}
		}

        public string Key
        {
            get
            {
                return key;
            }
        }

        public Index(string key)
        {
            this.key = key;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType().IsSubclassOf(typeof(Index)) || obj.GetType() == typeof(Index))
            {
                if (((Index)obj).key == key)
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

        public override int GetHashCode()
        {
            return key.GetHashCode();
        }

        public override string ToString()
        {
            return key;
        }

        public static List<Index> GetIndexes(List<DataFieldInfo> fields)
        {
            List<Index> indexes = new List<Index>();

            foreach (DataFieldInfo field in fields)
            {
                if (field.Indicies != null)
                {
                    foreach (Index index in field.Indicies)
                    {
                        if (!indexes.Contains(index))
                        {
                            indexes.Add(index);
                        }
                    }
                }
            }

            return indexes;
        }

        public static List<DataFieldInfo> GetFields(string key, List<DataFieldInfo> fields)
        {
            List<DataFieldInfo> indexFields = new List<DataFieldInfo>();

            foreach (DataFieldInfo field in fields)
            {
                if (field.Indicies != null)
                {
                    foreach (Index index in field.Indicies)
                    {
                        if (index.key == key)
                        {
                            indexFields.Add(field);
                        }
                    }
                }
            }

            return indexFields;
        }

        public int CompareTo(object obj)
        {
            if (obj.GetType().IsSubclassOf(typeof(Index)) || obj.GetType() == typeof(Index))
            {
                return ((Index)obj).key.CompareTo(key);
            }
            else
            {
                return -1;
            }
        }

    }
}
