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
using System.Collections.Generic;
using System.Text;

namespace BoxSocial.IO
{
    public enum DataFieldKeys : byte
    {
        None = 0x00,
        Primary = 0x01,
        Unique = 0x02,
		Index = 0x04,
    }

    public class DataFieldInfo
    {
		private DataFieldKeys key;
        private string fieldName;
        private Type fieldType;
        private long fieldLength;
        private Type parentType;
        private string parentFieldName;
        private List<Index> indicies;

        public string Name
        {
            get
            {
                return fieldName;
            }
        }

        public Type Type
        {
            get
            {
                return fieldType;
            }
        }

        public long Length
        {
            get
            {
                return fieldLength;
            }
        }
		
        /// <summary>
        /// Deletes all other keys from this field and replaced with the set value
        /// </summary>
		public DataFieldKeys Key
		{
			get
			{
				return key;
			}
			set
			{
				this.key = value;
                this.indicies = new List<Index>();
				switch (this.key)
	            {
	                case DataFieldKeys.Primary:
                        this.indicies.Add(new PrimaryKey());
	                    break;
	                case DataFieldKeys.Unique:
                        this.indicies.Add(new UniqueKey("u_" + fieldName));
	                    break;
					case DataFieldKeys.Index:
                        this.indicies.Add(new Index("i_" + fieldName));
						break;
				    default:
                        this.indicies = null;
						break;
	            }
			}
		}

        public Index[] Indicies
        {
            get
            {
                return indicies.ToArray();
            }
        }

        public Index PrimaryIndex
        {
            get
            {
                foreach (Index index in indicies)
                {
                    if (index.KeyType == DataFieldKeys.Primary)
                    {
                        return index;
                    }
                }
                return null;
            }
        }

        public Type ParentType
        {
            get
            {
                return parentType;
            }
            set
            {
                parentType = value;
            }
        }

        public string ParentFieldName
        {
            get
            {
                return parentFieldName;
            }
            set
            {
                parentFieldName = value;
            }
        }

        public DataFieldInfo(string name, Type type, long length)
        {
            this.fieldName = name;
            this.fieldType = type;
            this.fieldLength = length;
            this.indicies = new List<Index>();
        }

        public DataFieldInfo(string name, Type type, long length, DataFieldKeys key)
        {
            this.fieldName = name;
            this.fieldType = type;
            this.fieldLength = length;
            this.indicies = new List<Index>();
            switch (key)
            {
                case DataFieldKeys.Primary:
                    this.indicies.Add(new PrimaryKey());
                    break;
                case DataFieldKeys.Unique:
                    this.indicies.Add(new UniqueKey("u_" + fieldName));
                    break;
				case DataFieldKeys.Index:
                    this.indicies.Add(new Index("i_" + fieldName));
					break;
			    default:
					break;
            }
			this.key = key;
        }

        public DataFieldInfo(string name, Type type, long length, Index key)
        {
            this.fieldName = name;
            this.fieldType = type;
            this.fieldLength = length;
            this.indicies = new List<Index>();
            this.indicies.Add(key);
			if (key != null)
			{
				this.key |= key.KeyType;
			}
			else
			{
				this.key = DataFieldKeys.None;
			}
        }

        public DataFieldInfo(string name, Type type, long length, Index[] indicies)
        {
            this.fieldName = name;
            this.fieldType = type;
            this.fieldLength = length;
            this.indicies = new List<Index>();
            this.indicies.AddRange(indicies);
            foreach (Index index in indicies)
            {
                if (key != null)
                {
                    this.key |= index.KeyType;
                }
            }
        }
    }
}
