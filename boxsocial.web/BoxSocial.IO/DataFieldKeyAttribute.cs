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
    [AttributeUsage(AttributeTargets.Field)]
    public class DataFieldKeyAttribute : Attribute
    {
        private DataFieldKeys key;
        private Index index;

        public DataFieldKeyAttribute(DataFieldKeys key, string keyName)
        {
            this.key = key;
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
                    break;
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
                return this.index;
            }
        }
    }
}
