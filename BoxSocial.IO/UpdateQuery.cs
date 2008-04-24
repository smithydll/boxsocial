using System;
using System.Collections.Generic;
using System.Text;

namespace BoxSocial.IO
{
    public sealed class UpdateQuery : Query
    {
        private string table;
        private Dictionary<string, object> fieldValues;
        private QueryCondition conditions;

        public UpdateQuery(string tableName)
        {
            fieldValues = new Dictionary<string, object>();
            conditions = new QueryCondition();

            table = tableName;
        }

        public bool HasFields
        {
            get
            {
                return (fieldValues.Count > 0);
            }
        }

        public void AddField(string field, object value)
        {
            fieldValues.Add(field, value);
        }
        
        public QueryCondition AddCondition(string field, object value)
        {
            if (conditions.Count == 0)
            {
                return conditions.AddCondition(ConditionRelations.First, field, ConditionEquality.Equal, value);
            }
            else
            {
                return conditions.AddCondition(ConditionRelations.And, field, ConditionEquality.Equal, value);
            }
        }

        public QueryCondition AddCondition(ConditionRelations relation, string field, object value)
        {
            if (conditions.Count == 0)
            {
                return conditions.AddCondition(ConditionRelations.First, field, ConditionEquality.Equal, value);
            }
            else
            {
                return conditions.AddCondition(relation, field, ConditionEquality.Equal, value);
            }
        }

        public QueryCondition AddCondition(string field, ConditionEquality equality, object value)
        {
            if (conditions.Count == 0)
            {
                return conditions.AddCondition(ConditionRelations.First, field, equality, value);
            }
            else
            {
                return conditions.AddCondition(ConditionRelations.And, field, equality, value);
            }
        }

        public QueryCondition AddCondition(ConditionRelations relation, string field, ConditionEquality equality, object value)
        {
            if (conditions.Count == 0)
            {
                return conditions.AddCondition(ConditionRelations.First, field, equality, value);
            }
            else
            {
                return conditions.AddCondition(relation, field, equality, value);
            }
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

            if (conditions.Count > 0)
            {
                query = string.Format("{0} WHERE {1}",
                            query, conditions.ToString());
            }

            return string.Format("{0};", query);
        }
    }
}
