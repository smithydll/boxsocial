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
    [AttributeUsage(AttributeTargets.Field)]
    public class DataFieldAttribute : Attribute
    {
        private string fieldName;
        private bool isPrimaryKey;
        private bool isUnique;
        private long length;
        private Type parentType;
        private string parentFieldName;
        private UniqueKey key;

        public DataFieldAttribute()
        {
        }

        public DataFieldAttribute(string fieldName)
        {
            this.fieldName = fieldName;
            this.isPrimaryKey = false;
            this.isUnique = false;
            this.length = 0;
            this.parentFieldName = null;
            this.parentType = null;
            this.key = null;
        }

        public DataFieldAttribute(string fieldName, long fieldLength)
        {
            this.fieldName = fieldName;
            this.isPrimaryKey = false;
            this.isUnique = false;
            this.length = fieldLength;
            this.parentFieldName = null;
            this.parentType = null;
            this.key = null;
        }

        public DataFieldAttribute(string fieldName, DataFieldKeys key)
        {
            this.fieldName = fieldName;
            switch (key)
            {
                case DataFieldKeys.Primary:
                    this.isUnique = this.isPrimaryKey = true;
                    break;
                case DataFieldKeys.Unique:
                    this.isUnique = true;
                    this.isPrimaryKey = false;
                    break;
            }
            this.length = 0;
            this.parentFieldName = null;
            this.parentType = null;
            this.key = null;
        }

        public DataFieldAttribute(string fieldName, UniqueKey key)
        {
            this.fieldName = fieldName;
            if (key != null)
            {
                this.isUnique = true;
            }
            else
            {
                this.isUnique = false;
            }
            this.isPrimaryKey = false;
            this.length = 0;
            this.parentFieldName = null;
            this.parentType = null;
            this.key = key;
        }

        public DataFieldAttribute(string fieldName, DataFieldKeys key, long fieldLength)
            : this(fieldName, key)
        {
            this.length = fieldLength;
            this.parentFieldName = null;
            this.parentType = null;
            this.key = null;
        }

        public DataFieldAttribute(string fieldName, UniqueKey key, long fieldLength)
        {
            this.fieldName = fieldName;
            if (key != null)
            {
                this.isUnique = true;
            }
            else
            {
                this.isUnique = false;
            }
            this.isPrimaryKey = false;
            this.length = fieldLength;
            this.parentFieldName = null;
            this.parentType = null;
            this.key = key;
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
            this.isPrimaryKey = false;
            this.isUnique = false;
            this.length = 0;
            this.parentType = parentType;
            this.parentFieldName = parentFieldName;
            this.key = null;
        }

        public string FieldName
        {
            get
            {
                return fieldName;
            }
        }

        public bool IsPrimaryKey
        {
            get
            {
                return isPrimaryKey;
            }
        }

        public bool IsUnique
        {
            get
            {
                return isUnique;
            }
        }

        public DataFieldKeys Indexes
        {
            get
            {
                if (isPrimaryKey)
                {
                    return DataFieldKeys.Primary;
                }
                if (isUnique)
                {
                    return DataFieldKeys.Unique;
                }
                return DataFieldKeys.None;
            }
        }

        public UniqueKey Key
        {
            get
            {
                return key;
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
