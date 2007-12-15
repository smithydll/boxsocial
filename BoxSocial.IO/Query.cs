/*
 * Box Social�
 * http://boxsocial.net/
 * Copyright � 2007, David Lachlan Smith
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
    public abstract class Query
    {

        public static string ObjectToSql(object value)
        {
            if (value is string)
            {
                return string.Format("'{0}'", Mysql.Escape((string)value));
            }
            else if (value is long)
            {
                return ((long)value).ToString();
            }
            else if (value is int)
            {
                return ((int)value).ToString();
            }
            else if (value is short)
            {
                return ((short)value).ToString();
            }
            else if (value is ulong)
            {
                return ((ulong)value).ToString();
            }
            else if (value is uint)
            {
                return ((uint)value).ToString();
            }
            else if (value is ushort)
            {
                return ((ushort)value).ToString();
            }
            else if (value is byte)
            {
                return ((byte)value).ToString();
            }
            else if (value is double)
            {
                return ((double)value).ToString();
            }
            else if (value is float)
            {
                return ((float)value).ToString();
            }
            else if (value is bool)
            {
                return ((int)value).ToString();
            }
            else if (value is List<string>)
            {
                StringBuilder temp = new StringBuilder();
                bool first = true;
                foreach (string item in (List<string>)value)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        temp.Append(", ");
                    }
                    temp.Append(ObjectToSql(item));
                }
                return string.Format("({0})",
                    temp.ToString());
            }
            else if (value is List<long>)
            {
                StringBuilder temp = new StringBuilder();
                bool first = true;
                foreach (long item in (List<long>)value)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        temp.Append(", ");
                    }
                    temp.Append(ObjectToSql(item));
                }
                return string.Format("({0})",
                    temp.ToString());
            }
            else if (value is List<int>)
            {
                StringBuilder temp = new StringBuilder();
                bool first = true;
                foreach (int item in (List<int>)value)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        temp.Append(", ");
                    }
                    temp.Append(ObjectToSql(item));
                }
                return string.Format("({0})",
                    temp.ToString());
            }
            else if (value is SelectQuery)
            {
                return string.Format("({0})",
                    ((SelectQuery)value).ToString());
            }
            else if (value is QueryField)
            {
                return string.Format("({0})",
                    ((QueryField)value).ToString());
            }
            else
            {
                throw new UnknownFieldTypeException(value);
            }
        }
    }

    public class UnknownFieldTypeException : Exception
    {
        public UnknownFieldTypeException(object field) : base("unknown field of type " + field.GetType().ToString())
        {
        }
    }
}