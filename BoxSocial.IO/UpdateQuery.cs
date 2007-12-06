using System;
using System.Collections.Generic;
using System.Text;

namespace BoxSocial.IO
{
    public sealed class UpdateQuery : Query
    {
        public string table;
        public Dictionary<string, object> fieldValues;
        public Dictionary<string, object> condition;

        public UpdateQuery(string tableName)
        {
            fieldValues = new Dictionary<string, object>();
            condition = new Dictionary<string, object>();

            table = tableName;
        }

        public override string ToString()
        {
            string query = string.Format("UPDATE {0}",
                table);

            if (fieldValues.Count > 0)
            {
                bool first = true;
                foreach (string field in fieldValues.Keys)
                {
                    if (first)
                    {
                        query = string.Format("{0} SET {1} = {2}",
                            query, field, Query.ObjectToSql(fieldValues[field]));
                        first = false;
                    }
                    else
                    {
                        query = string.Format("{0}, {1} = {2}",
                            query, field, Query.ObjectToSql(fieldValues[field]));
                    }
                }
            }

            if (condition.Count > 0)
            {
                bool first = true;
                foreach (string field in condition.Keys)
                {
                    if (first)
                    {
                        query = string.Format("{0} WHERE {1} = {2}",
                            query, field, Query.ObjectToSql(condition[field]));
                        first = false;
                    }
                    else
                    {
                        query = string.Format("{0} AND {1} = {2}",
                            query, field, Query.ObjectToSql(condition[field]));
                    }
                }
            }

            return string.Format("{0};", query);
        }
    }
}
