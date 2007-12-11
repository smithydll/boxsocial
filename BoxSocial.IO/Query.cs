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
            else
            {
                throw new UnknownFieldTypeException();
            }
        }
    }

    public class UnknownFieldTypeException : Exception
    {
    }
}
