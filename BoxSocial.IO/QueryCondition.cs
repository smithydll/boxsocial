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
        NotIn,
        Like,
    }

    public class QueryCondition
    {
        private List<KeyValuePair<QueryCondition, ConditionRelations>> conditions;
        private ConditionEquality equality;
        private ConditionRelations relation;
        private string field;
        private object value;

        public QueryCondition()
        {
            conditions = new List<KeyValuePair<QueryCondition, ConditionRelations>>();
        }

        public QueryCondition(string field, ConditionEquality equality, object value) : this()
        {
            this.field = field;
            this.equality = equality;
            this.value = value;
        }

        public QueryCondition(ConditionRelations relation, string field, ConditionEquality equality, object value) : this()
        {
            this.relation = relation;
            this.field = field;
            this.equality = equality;
            this.value = value;
        }

        public QueryCondition AddCondition(QueryOperation field, object value)
        {
            return AddCondition(field.ToString(), value);
        }

        public QueryCondition AddCondition(string field, object value)
        {
            if (conditions.Count == 0 && string.IsNullOrEmpty(field))
            {
                return AddCondition(ConditionRelations.First, field, ConditionEquality.Equal, value);
            }
            else
            {
                return AddCondition(ConditionRelations.And, field, ConditionEquality.Equal, value);
            }
        }

        public QueryCondition AddCondition(ConditionRelations relation, QueryOperation field, object value)
        {
            return AddCondition(relation, field.ToString(), value);
        }

        public QueryCondition AddCondition(ConditionRelations relation, string field, object value)
        {
            if (conditions.Count == 0 && string.IsNullOrEmpty(field))
            {
                return AddCondition(ConditionRelations.First, field, ConditionEquality.Equal, value);
            }
            else
            {
                return AddCondition(relation, field, ConditionEquality.Equal, value);
            }
        }

        public QueryCondition AddCondition(QueryOperation field, ConditionEquality equality, object value)
        {
            return AddCondition(field.ToString(), equality, value);
        }

        public QueryCondition AddCondition(string field, ConditionEquality equality, object value)
        {
            if (conditions.Count == 0 && string.IsNullOrEmpty(field))
            {
                return AddCondition(ConditionRelations.First, field, equality, value);
            }
            else
            {
                return AddCondition(ConditionRelations.And, field, equality, value);
            }
        }

        public QueryCondition AddCondition(ConditionRelations relation, QueryOperation field, ConditionEquality equality, object value)
        {
            return AddCondition(relation, field.ToString(), equality, value);
        }

        public QueryCondition AddCondition(ConditionRelations relation, string field, ConditionEquality equality, object value)
        {
            QueryCondition condition = new QueryCondition(field, equality, value);

            conditions.Add(new KeyValuePair<QueryCondition, ConditionRelations>(condition, relation));

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
            string query = String.Empty;

            if (!string.IsNullOrEmpty(field))
            {
                query = string.Format("{0} {1} {2} {3} {4}",
                    query, RelationToString(relation), field, EqualityToString(equality), Query.ObjectToSql(value));
            }
            //else
            if (conditions.Count > 0)
            {
                /*foreach (QueryCondition condition in conditions.Keys)
                {
                    query = string.Format("{0} {1} ({2})",
                        query, RelationToString(conditions[condition]), condition.ToString());
                }*/
                foreach (KeyValuePair<QueryCondition, ConditionRelations> keypair in conditions)
                {
                    query = string.Format("{0} {1} ({2})",
                        query, RelationToString(keypair.Value), keypair.Key);
                }
            }

            return query;
        }

        public static string RelationToString(ConditionRelations relation)
        {
            switch (relation)
            {
                case ConditionRelations.First:
                    return String.Empty;
                case ConditionRelations.And:
                    return "AND";
                case ConditionRelations.Or:
                    return "OR";
            }
            return String.Empty;
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
                case ConditionEquality.NotIn:
                    return "NOT IN";
                case ConditionEquality.Like:
                    return "LIKE";
            }
            return String.Empty;
        }

        public static string EscapeLikeness(string input)
        {
            // TODO: apply escape sequence
            //return input.Replace("%", 
            return input;
        }
    }
}
