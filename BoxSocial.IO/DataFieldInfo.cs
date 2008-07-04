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
    public enum DataFieldKeys
    {
        None = 0x00,
        Primary = 0x01,
        Unique = 0x02,
    }

    public class DataFieldInfo
    {
        private string fieldName;
        private Type fieldType;
        private long fieldLength;
        private bool isPrimaryKey;
        private bool isUnique;
        private Type parentType;
        private string parentFieldName;

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

        public bool IsPrimaryKey
        {
            get
            {
                return isPrimaryKey;
            }
            set
            {
                isPrimaryKey = value;
            }
        }

        public bool IsUnique
        {
            get
            {
                return isUnique;
            }
            set
            {
                isUnique = value;
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
            this.isPrimaryKey = false;
            this.isUnique = false;
            this.parentType = null;
            this.parentFieldName = null;
        }

        public DataFieldInfo(string name, Type type, long length, DataFieldKeys key)
        {
            this.fieldName = name;
            this.fieldType = type;
            this.fieldLength = length;
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
            this.parentType = null;
            this.parentFieldName = null;
        }
    }
}
