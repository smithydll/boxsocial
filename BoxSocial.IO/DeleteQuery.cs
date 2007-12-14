using System;
using System.Collections.Generic;
using System.Text;

namespace BoxSocial.IO
{
    public sealed class DeleteQuery : Query
    {
        public string table;
        private QueryCondition conditions;

        public DeleteQuery(string tableName)
        {
            conditions = new QueryCondition();

            table = tableName;
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
            string query = string.Format("DELETE FROM {0}",
                table);

            if (conditions.Count > 0)
            {
                query = string.Format("{0} WHERE {1}",
                            query, conditions.ToString());
            }

            return string.Format("{0};", query);
        }
    }
}
