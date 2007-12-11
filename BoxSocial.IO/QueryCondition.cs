using System;
using System.Collections.Generic;
using System.Text;

namespace BoxSocial.IO
{
    public enum ConditionRelations
    {
        First,
        Or,
        And,
    }

    public enum ConditionEquality
    {
        Equal,
        NotEqual,
        GreaterThan,
        LessThan,
        GreaterThanEqual,
        LessThanEqual,
        In,
    }

    public class QueryCondition
    {
        private Dictionary<QueryCondition, ConditionRelations> conditions;
        private ConditionEquality equality;
        private ConditionRelations relation;
        private string field;
        private object value;

        public QueryCondition()
        {
            conditions = new Dictionary<QueryCondition, ConditionRelations>();
        }

        public QueryCondition(string field, ConditionEquality equality, object value)
        {
            this.relation = ConditionRelations.First;
            this.field = field;
            this.equality = equality;
            this.value = value;
        }

        public QueryCondition(ConditionRelations relation, string field, ConditionEquality equality, object value)
        {
            this.relation = relation;
            this.field = field;
            this.equality = equality;
            this.value = value;
        }

        public QueryCondition AddCondition(ConditionRelations relation, string field, ConditionEquality equality, object value)
        {
            QueryCondition condition = new QueryCondition(field, equality, value);
            
            conditions.Add(condition, relation);

            return condition;
        }

        public int Count
        {
            get
            {
                return conditions.Count;
            }
        }

        public override string ToString()
        {
            string query = "";

            if (!string.IsNullOrEmpty(field))
            {
                query = string.Format("{0} {1} {2} {3} {4}",
                    query, RelationToString(relation), field, EqualityToString(equality), Query.ObjectToSql(value));
            }
            else
            {
                foreach (QueryCondition condition in conditions.Keys)
                {
                    query = string.Format("{0} {1} ({2})",
                        query, RelationToString(conditions[condition]), condition.ToString());
                }
            }

            return query;
        }

        public static string RelationToString(ConditionRelations relation)
        {
            switch (relation)
            {
                case ConditionRelations.First:
                    return "";
                case ConditionRelations.And:
                    return "AND";
                case ConditionRelations.Or:
                    return "OR";
            }
            return "";
        }

        public static string EqualityToString(ConditionEquality equality)
        {
            switch (equality)
            {
                case ConditionEquality.Equal:
                    return "=";
                case ConditionEquality.GreaterThan:
                    return ">";
                case ConditionEquality.GreaterThanEqual:
                    return ">=";
                case ConditionEquality.LessThan:
                    return "<";
                case ConditionEquality.LessThanEqual:
                    return "<=";
                case ConditionEquality.NotEqual:
                    return "<>";
                case ConditionEquality.In:
                    return "IN";
            }
            return "";
        }
    }
}
