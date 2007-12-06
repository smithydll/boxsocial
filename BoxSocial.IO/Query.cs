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

            return "''";
        }
    }
}
