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
        private Index index;

        public DataFieldAttribute()
        {
        }

        public DataFieldAttribute(string fieldName)
        {
			this.key = DataFieldKeys.None;
            this.fieldName = fieldName;
            this.length = 0;
            this.parentFieldName = null;
            this.parentType = null;
            this.index = null;
        }

        public DataFieldAttribute(string fieldName, long fieldLength)
        {
			this.key = DataFieldKeys.None;
            this.fieldName = fieldName;
            this.length = fieldLength;
            this.parentFieldName = null;
            this.parentType = null;
            this.index = null;
        }

        public DataFieldAttribute(string fieldName, DataFieldKeys key)
        {
			this.key = key;
            this.fieldName = fieldName;
            switch (key)
            {
                case DataFieldKeys.Primary:
				    this.index = new PrimaryKey();
                    break;
                case DataFieldKeys.Unique:
				    this.index = new UniqueKey("u_" + fieldName);
                    break;
				case DataFieldKeys.Index:
				    this.index = new Index("i_" + fieldName);
					break;
				default:
				    this.index = null;
					break;
            }
            this.length = 0;
            this.parentFieldName = null;
            this.parentType = null;
        }

        public DataFieldAttribute(string fieldName, DataFieldKeys key, string keyName)
        {
			this.key = key;
            this.fieldName = fieldName;
            this.length = 0;
            this.parentFieldName = null;
            this.parentType = null;
			switch (key)
			{
				case DataFieldKeys.Primary:
		            this.index = new PrimaryKey();
					break;
				case DataFieldKeys.Unique:
		            this.index = new UniqueKey(keyName);
					break;
				case DataFieldKeys.Index:
		            this.index = new Index(keyName);
					break;
				default:
		            this.index = null;
					break;
			}
        }

        public DataFieldAttribute(string fieldName, DataFieldKeys key, long fieldLength)
            : this(fieldName, key)
        {
			this.key = key;
            this.length = fieldLength;
            this.parentFieldName = null;
            this.parentType = null;
            switch (key)
			{
				case DataFieldKeys.Primary:
					this.index = new PrimaryKey();
					break;
				case DataFieldKeys.Unique:
				    this.index = new UniqueKey("u_" + fieldName);
					break;
				case DataFieldKeys.Index:
				    this.index = new Index("i_" + fieldName);
					break;
				default:
    				this.index = null;
					break;
			}
        }

        public DataFieldAttribute(string fieldName, DataFieldKeys key, string keyName, long fieldLength)
        {
			this.key = key;
            this.fieldName = fieldName;
            this.length = fieldLength;
            this.parentFieldName = null;
            this.parentType = null;
			switch (key)
			{
				case DataFieldKeys.Primary:
					this.index = new PrimaryKey();
					break;
				case DataFieldKeys.Unique:
				    this.index = new UniqueKey(keyName);
					break;
				case DataFieldKeys.Index:
				    this.index = new Index(keyName);
					break;
				default:
    				this.index = null;
					break;
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
			this.key = DataFieldKeys.None;
            this.fieldName = fieldName;
            this.length = 0;
            this.parentType = parentType;
            this.parentFieldName = parentFieldName;
            this.index = null;
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
            switch (key)
			{
				case DataFieldKeys.Primary:
				    this.index = new PrimaryKey();
					break;
				case DataFieldKeys.Unique:
				    this.index = new UniqueKey("u_" + fieldName);
					break;
				case DataFieldKeys.Index:
				    this.index = new Index("i_" + fieldName);
					break;
				default:
    				this.index = null;
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
            switch (key)
			{
				case DataFieldKeys.Primary:
				    this.index = new PrimaryKey();
					break;
				case DataFieldKeys.Unique:
				    this.index = new UniqueKey(keyName);
					break;
				case DataFieldKeys.Index:
				    this.index = new Index(keyName);
					break;
				default:
    				this.index = null;
					break;
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
			switch (key)
			{
				case DataFieldKeys.Primary:
				    this.index = new PrimaryKey();
					break;
				case DataFieldKeys.Unique:
				    this.index = new UniqueKey(keyName);
					break;
				case DataFieldKeys.Index:
				    this.index = new Index(keyName);
					break;
				default:
    				this.index = null;
					break;
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

        public Index Index
        {
            get
            {
                return index;
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
    }
}
