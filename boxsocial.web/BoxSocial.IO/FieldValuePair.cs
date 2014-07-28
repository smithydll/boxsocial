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
	public class FieldValuePair
	{
		private string field;
		private object fieldValue;
		
		public string Field
		{
			get
			{
				return field;
			}
		}

        public string Table
        {
            get
            {
                if (field.Contains("`.`"))
                {
                    int i = field.IndexOf("`.`");
                    return field.Substring(1, i - 1);
                }
                else
                {
                    return null;
                }
            }
        }

        public string TableField
        {
            get
            {
                if (field.Contains("`.`"))
                {
                    int i = field.IndexOf("`.`");
                    return field.Substring(i + 3, field.Length - i - 3 - 1);
                }
                else
                {
                    return field;
                }
            }
        }
		
		public object Value
		{
			get
			{
				return fieldValue;
			}
		}
		
		public FieldValuePair(string field, object fieldValue)
		{
			this.field = field;
			this.fieldValue = fieldValue;
		}
	}
}
