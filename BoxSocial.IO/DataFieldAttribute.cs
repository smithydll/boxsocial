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
    [AttributeUsage(AttributeTargets.Field)]
    public class DataFieldAttribute : Attribute
    {
		private DataFieldKeys key;
        private string fieldName;
        private long length;
        private Type parentType;
        private string parentFieldName;
        private List<Index> indicies;

        public DataFieldAttribute()
        {
        }

        public DataFieldAttribute(string fieldName)
        {
            this.fieldName = fieldName;
            this.length = 0;
            this.indicies = new List<Index>();
        }

        public DataFieldAttribute(string fieldName, long fieldLength)
        {
            this.fieldName = fieldName;
            this.length = fieldLength;
            this.indicies = new List<Index>();
        }

        public DataFieldAttribute(string fieldName, DataFieldKeys key)
        {
			this.key = key;
            this.fieldName = fieldName;
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
            this.length = 0;
        }

        public DataFieldAttribute(string fieldName, DataFieldKeys key, string keyName)
        {
			this.key = key;
            this.fieldName = fieldName;
            this.length = 0;
            this.indicies = new List<Index>();
			switch (key)
			{
				case DataFieldKeys.Primary:
                    this.indicies.Add(new PrimaryKey());
					break;
				case DataFieldKeys.Unique:
                    this.indicies.Add(new UniqueKey(keyName));
					break;
				case DataFieldKeys.Index:
                    this.indicies.Add(new Index(keyName));
					break;
				default:
					break;
			}
        }

        public DataFieldAttribute(string fieldName, params Index[] indicies)
        {
            this.fieldName = fieldName;
            this.length = 0;
            this.indicies = new List<Index>();
            foreach (Index index in indicies)
            {
                this.key |= index.KeyType;

                this.indicies.Add(index);
            }
        }

        public DataFieldAttribute(string fieldName, DataFieldKeys key, long fieldLength)
            : this(fieldName, key)
        {
			this.key = key;
            this.length = fieldLength;
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
        }

        public DataFieldAttribute(string fieldName, DataFieldKeys key, string keyName, long fieldLength)
        {
			this.key = key;
            this.fieldName = fieldName;
            this.length = fieldLength;
            this.indicies = new List<Index>();
			switch (key)
			{
				case DataFieldKeys.Primary:
                    this.indicies.Add(new PrimaryKey());
					break;
				case DataFieldKeys.Unique:
                    this.indicies.Add(new UniqueKey(keyName));
					break;
				case DataFieldKeys.Index:
                    this.indicies.Add(new Index(keyName));
					break;
				default:
					break;
			}
        }

        public DataFieldAttribute(string fieldName, long fieldLength, params Index[] indicies)
        {
            this.fieldName = fieldName;
            this.length = fieldLength;
            this.indicies = new List<Index>();
            foreach (Index index in indicies)
            {
                this.key |= index.KeyType;

                this.indicies.Add(index);
            }
        }

        /// <summary>
        /// Parent type assumes that the relationship is of the same field name in the parent table
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="parentType"></param>
        public DataFieldAttribute(string fieldName, Type parentType)
            : this (fieldName, parentType, fieldName)
        {
        }

        public DataFieldAttribute(string fieldName, Type parentType, string parentFieldName)
        {
            this.fieldName = fieldName;
            this.length = 0;
            this.parentType = parentType;
            this.parentFieldName = parentFieldName;
            this.indicies = new List<Index>();
        }
		
		public DataFieldAttribute(string fieldName, Type parentType, DataFieldKeys key)
            : this(fieldName, parentType, fieldName, key)
        {
        }
		
		public DataFieldAttribute(string fieldName, Type parentType, string parentFieldName, DataFieldKeys key)
        {
			this.key = key;
            this.fieldName = fieldName;
            this.length = 0;
            this.parentType = parentType;
            this.parentFieldName = parentFieldName;
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
        }

        public DataFieldAttribute(string fieldName, Type parentType, DataFieldKeys key, string keyName)
            : this(fieldName, parentType, fieldName, key, keyName)
        {
        }

        public DataFieldAttribute(string fieldName, Type parentType, string parentFieldName, DataFieldKeys key, string keyName)
        {
			this.key = key;
            this.fieldName = fieldName;
            this.length = 0;
            this.parentType = parentType;
            this.parentFieldName = parentFieldName;
            this.indicies = new List<Index>();
            switch (key)
			{
				case DataFieldKeys.Primary:
                    this.indicies.Add(new PrimaryKey());
					break;
				case DataFieldKeys.Unique:
                    this.indicies.Add(new UniqueKey(keyName));
					break;
				case DataFieldKeys.Index:
                    this.indicies.Add(new Index(keyName));
					break;
				default:
					break;
			}
        }

        public DataFieldAttribute(string fieldName, Type parentType, string parentFieldName, params Index[] indicies)
        {
            this.fieldName = fieldName;
            this.length = 0;
            this.parentType = parentType;
            this.parentFieldName = parentFieldName;
            this.indicies = new List<Index>();
            foreach (Index index in indicies)
            {
                this.key |= index.KeyType;

                this.indicies.Add(index);
            }
        }

        public DataFieldAttribute(string fieldName, Type parentType, DataFieldKeys key, string keyName, long fieldLength)
            : this(fieldName, parentType, fieldName, key, keyName)
        {
        }

        public DataFieldAttribute(string fieldName, Type parentType, string parentFieldName, DataFieldKeys key, string keyName, long fieldLength)
        {
			this.key = key;
            this.fieldName = fieldName;
            this.length = fieldLength;
            this.parentType = parentType;
            this.parentFieldName = parentFieldName;
            this.indicies = new List<Index>();
			switch (key)
			{
				case DataFieldKeys.Primary:
                    this.indicies.Add(new PrimaryKey());
					break;
				case DataFieldKeys.Unique:
                    this.indicies.Add(new UniqueKey(keyName));
					break;
				case DataFieldKeys.Index:
                    this.indicies.Add(new Index(keyName));
					break;
				default:
					break;
			}
        }

        public DataFieldAttribute(string fieldName, Type parentType, string parentFieldName, long fieldLength, params Index[] indicies)
        {
            this.fieldName = fieldName;
            this.length = fieldLength;
            this.parentType = parentType;
            this.parentFieldName = parentFieldName;
            this.indicies = new List<Index>();
            foreach (Index index in indicies)
            {
                this.key |= index.KeyType;

                this.indicies.Add(index);
            }
        }

        public string FieldName
        {
            get
            {
                return fieldName;
            }
        }

        public DataFieldKeys Indexes
        {
            get
            {
                return this.key;
            }
        }

        public Index[] Indicies
        {
            get
            {
                return indicies.ToArray();
            }
        }

        public void AddIndex(DataFieldKeyAttribute index)
        {
            if (index != null)
            {
                indicies.Add(index.Index);
            }
        }

        public void AddIndexes(DataFieldKeyAttribute[] indexes)
        {
            foreach (DataFieldKeyAttribute index in indexes)
            {
                indicies.Add(index.Index);
            }
        }

        /// <summary>
        /// String types only
        /// </summary>
        public long MaxLength
        {
            get
            {
                return length;
            }
        }

        public Type ParentType
        {
            get
            {
                return parentType;
            }
        }

        public string ParentFieldName
        {
            get
            {
                return parentFieldName;
            }
        }
        
        public static string GetTable(Type type)
        {
            bool attributeFound = false;
            foreach (Attribute attr in type.GetCustomAttributes(typeof(DataTableAttribute), false))
            {
                DataTableAttribute dtattr = (DataTableAttribute)attr;
                if (dtattr != null)
                {
                    if (dtattr.TableName != null)
                    {
                        return dtattr.TableName;
                    }
                    attributeFound = true;
                }
            }

            /* Maybe is a Table View if haven't found a DataTable */
            if (!attributeFound)
            {
                foreach (Attribute attr in type.GetCustomAttributes(typeof(TableViewAttribute), false))
                {
                    TableViewAttribute tvattr = (TableViewAttribute)attr;
                    if (tvattr != null)
                    {
                        if (tvattr.TableName != null)
                        {
                            return tvattr.TableName;
                        }
                        attributeFound = true;
                    }
                }
            }

            if (attributeFound)
            {
                return type.Name;
            }
            else
            {
                return null;
            }
        }
    }
}
