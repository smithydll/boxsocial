using System;
using System.Collections.Generic;
using System.Text;

namespace BoxSocial.IO
{
    public sealed class DeleteQuery : Query
    {
        public string table;
        public Dictionary<string, object> condition;

        public DeleteQuery(string tableName)
        {
            condition = new Dictionary<string, object>();

            table = tableName;
        }

        public override string ToString()
        {
            string query = string.Format("DELETE FROM {0}",
                table);

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
