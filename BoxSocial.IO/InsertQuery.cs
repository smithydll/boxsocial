using System;
using System.Collections.Generic;
using System.Text;

namespace BoxSocial.IO
{
    public sealed class InsertQuery : Query
    {
        private string table;
        private Dictionary<string, object> fieldValues;

        public InsertQuery(string tableName)
        {
            fieldValues = new Dictionary<string, object>();

            table = tableName;
        }

        public void AddField(string field, object value)
        {
            fieldValues.Add(field, value);
        }

        public override string ToString()
        {
            string query = string.Format("INSERT INTO {0}",
                table);
            string fields = "";
            string values = "";

            if (fieldValues.Count > 0)
            {
                bool first = true;
                foreach (string field in fieldValues.Keys)
                {
                    if (first)
                    {
                        fields = string.Format("{1}",
                            fields, field);
                        values = string.Format("{1}",
                            values, Query.ObjectToSql(fieldValues[field]));
                        first = false;
                    }
                    else
                    {
                        fields = string.Format("{0}, {1}",
                            fields, field);
                        values = string.Format("{0}, {1}",
                            values, Query.ObjectToSql(fieldValues[field]));
                    }
                }

                query = string.Format("{0} ({1}) VALUES ({2})",
                    query, fields, values);
            }

            return string.Format("{0};", query);
        }
    }
}
