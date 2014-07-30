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

namespace BoxSocial.Internals
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PrimitiveAttribute : Attribute
    {
        private string type;
        private object defaultLoadOptions;
        private Type loadOptionsType;
        private string idField;
        private string keyField;

        public string Type
        {
            get
            {
                return type;
            }
        }

        public object DefaultLoadOptions
        {
            get
            {
                return defaultLoadOptions;
            }
        }

        public string IdField
        {
            get
            {
                return idField;
            }
        }

        public string KeyField
        {
            get
            {
                return keyField;
            }
        }

        public PrimitiveAttribute(string type)
        {
            this.type = type;
            this.defaultLoadOptions = 0;
        }

        public PrimitiveAttribute(string type, object defaultLoadOptions, string idField, string keyField)
        {
            this.type = type;
            if (defaultLoadOptions.GetType().IsEnum)
            {
                this.loadOptionsType = defaultLoadOptions.GetType();
                this.defaultLoadOptions = defaultLoadOptions;
            }
            this.idField = idField;
            this.keyField = keyField;
        }
    }
}
