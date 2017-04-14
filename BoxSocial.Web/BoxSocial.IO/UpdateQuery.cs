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
        private List<TableSort> sorts;
        private int limitCount = -1;
        private SortOrder limitOrder = SortOrder.Ascending;

        public int LimitCount
        {
            get
            {
                return limitCount;
            }
            set
            {
                limitCount = value;
            }
        }

        public SortOrder LimitOrder
        {
            get
            {
                return limitOrder;
            }
            set
            {
                limitOrder = value;
            }
        }

        public UpdateQuery(string tableName)
        {
            fieldValues = new Dictionary<string, object>(StringComparer.Ordinal);
            conditions = new QueryCondition();
            sorts = new List<TableSort>();

            table = tableName;
        }

        public UpdateQuery(Type type)
        {
            fieldValues = new Dictionary<string, object>(StringComparer.Ordinal);
            conditions = new QueryCondition();
            sorts = new List<TableSort>();

            table = DataFieldAttribute.GetTable(type);
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

        public void SetBitField(string field, object value)
        {
            fieldValues.Add(field, new QueryOperation(field, QueryOperations.BinaryOr, value));
        }

        public void UnsetBitField(string field, object value)
        {
            fieldValues.Add(field, new QueryOperation(field, QueryOperations.BinaryAnd, new QueryBinaryInverse(value)));
        }

        public void AddSort(SortOrder order, string field)
        {
            sorts.Add(new TableSort(order, field, null));
        }

        public void AddSort(SortOrder order, DataField field)
        {
            sorts.Add(new TableSort(order, field, null));
        }

        public void AddSort(SortOrder order, QueryCondition lastSort)
        {
            sorts.Add(new TableSort(order, string.Empty, lastSort));
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
            StringBuilder query = new StringBuilder();
            query.AppendFormat("UPDATE {0}", table);

            /*string query = string.Format("UPDATE {0}",
                table);*/

            if (fieldValues.Count > 0)
            {
                bool first = true;
                foreach (string field in fieldValues.Keys)
                {
                    if (first)
                    {
                        query.AppendFormat(" SET {0} = {1}", field, Query.ObjectToSql(fieldValues[field]));
                        first = false;
                    }
                    else
                    {
                        query.AppendFormat(", {0} = {1}", field, Query.ObjectToSql(fieldValues[field]));
                    }
                }
            }

            if (conditions.Count > 0)
            {
                query.AppendFormat(" WHERE {0}", conditions.ToString());
            }


            if (sorts.Count > 0)
            {
                bool first = true;
                foreach (TableSort sort in sorts)
                {
                    if (first)
                    {
                        query.Append(" ORDER BY ");
                        query.Append(sort.ToString());
                        first = false;
                    }
                    else
                    {
                        query.Append(", ");
                        query.Append(sort.ToString());
                    }
                }
            }

            if (limitCount >= 0)
            {
                query.Append(" LIMIT ");
                query.Append(limitCount.ToString());
            }

            query.Append(";");

            string retQuery = query.ToString();

            if (LimitOrder == SortOrder.Descending)
            {
                StringBuilder limitQuery = new StringBuilder();

                if (sorts.Count > 0)
                {
                    bool first = true;
                    for (int i = sorts.Count - 1; i >= 0; i--)
                    {
                        if (first)
                        {
                            limitQuery.Append(" ORDER BY ");
                            first = false;
                        }
                        else
                        {
                            limitQuery.Append(", ");
                        }
                        if (i == 0)
                        {
                            limitQuery.Append(sorts[i].ToString(true));
                        }
                        else
                        {
                            limitQuery.Append(sorts[i].ToString(false));
                        }
                    }
                }

                retQuery = string.Format("({0}) {1}", retQuery.TrimEnd(new char[] { ';', ' ' }), limitQuery.ToString());
            }

            return retQuery;
        }
    }
}
