using System;
using System.Collections.Generic;
using System.Text;

namespace BoxSocial.IO
{
    public enum SortOrder
    {
        Ascending,
        Descending,
    }

    public struct TableSort
    {
        public SortOrder Order;
        public string Field;

        public TableSort(SortOrder order, string field)
        {
            Order = order;
            Field = field;
        }

        public override string ToString()
        {
            switch (Order)
            {
                case SortOrder.Ascending:
                    return string.Format("{0} ASC",
                        Field);
                case SortOrder.Descending:
                    return string.Format("{0} DESC",
                        Field);
                default:
                    return "";
            }
        }
    }

    public enum JoinTypes
    {
        Inner,
        Left,
        Right,
    }

    public struct TableJoin
    {
        /// <summary>
        /// Type of join
        /// </summary>
        public JoinTypes Type;

        /// <summary>
        /// table to join
        /// </summary>
        public string Table;

        /// <summary>
        /// Field on table
        /// </summary>
        public string JoinField;

        /// <summary>
        /// Field on table Table
        /// </summary>
        public string TableField;

        public TableJoin(JoinTypes type, string table, string joinField, string tableField)
        {
            Type = type;
            Table = table;
            JoinField = joinField;
            TableField = tableField;
        }

        public override string ToString()
        {
            switch (Type)
            {
                case JoinTypes.Inner:
                    return string.Format("INNER JOIN {0} ON {1} = {2}",
                        Table, TableField, JoinField);
                case JoinTypes.Left:
                    return string.Format("LEFT JOIN {0} ON {1} = {2}",
                        Table, TableField, JoinField);
                case JoinTypes.Right:
                    return string.Format("RIGHT JOIN {0} ON {1} = {2}",
                        Table, TableField, JoinField);
                default:
                    return "";
            }
        }
    }

    public sealed class SelectQuery : Query
    {

        private List<string> tables;
        private List<string> fields;
        private QueryCondition conditions;
        private List<TableJoin> joins;
        private List<TableSort> sorts;
        private int limitCount = -1;
        private int limitStart = -1;

        public int LimitCount
        {
            set
            {
                limitCount = value;
            }
        }

        public int LimitStart
        {
            set
            {
                limitStart = value;
            }
        }

        public SelectQuery(string baseTableName)
        {
            tables = new List<string>();
            fields = new List<string>();
            conditions = new QueryCondition();
            joins = new List<TableJoin>();
            sorts = new List<TableSort>();

            tables.Add(baseTableName);
        }

        public void AddFields(params string[] fields)
        {
            this.fields.AddRange(fields);
        }

        public void AddJoin(JoinTypes type, string table, string joinField, string tableField)
        {
            joins.Add(new TableJoin(type, table, joinField, tableField));
        }

        public void AddSort(SortOrder order, string field)
        {
            sorts.Add(new TableSort(order, field));
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
            string query = "SELECT";

            if (fields.Count > 0)
            {
                bool first = true;
                foreach (string field in fields)
                {
                    if (first)
                    {
                        query = string.Format("{0} {1}",
                            query, field);
                        first = false;
                    }
                    else
                    {
                        query = string.Format("{0}, {1}",
                            query, field);
                    }
                }
            }
            else
            {
                query = string.Format("{0} *",
                            query);
            }

            if (tables.Count > 0)
            {
                bool first = true;
                foreach (string table in tables)
                {
                    if (first)
                    {
                        query = string.Format("{0} FROM {1}",
                            query, table);
                        first = false;
                    }
                    else
                    {
                        query = string.Format("{0}, {1}",
                            query, table);
                    }
                }
            }

            if (joins.Count > 0)
            {
                foreach (TableJoin join in joins)
                {
                    query = string.Format("{0} {1}",
                            query, join.ToString());
                }
            }

            if (conditions.Count > 0)
            {
                query = string.Format("{0} WHERE {1}",
                            query, conditions.ToString());
            }

            if (sorts.Count > 0)
            {
                bool first = true;
                foreach (TableSort sort in sorts)
                {
                    if (first)
                    {
                        query = string.Format("{0} ORDER BY {1}",
                            query, sort.ToString());
                        first = false;
                    }
                    else
                    {
                        query = string.Format("{0}, {1}",
                            query, sort.ToString());
                    }
                }
            }

            if (limitCount >= 0 && limitStart >= 0)
            {
                query = string.Format("{0} LIMIT {2}, {1}",
                            query, limitCount, limitStart);
            }
            else if (limitCount >= 0)
            {
                query = string.Format("{0} LIMIT {1}",
                            query, limitCount);
            }

            return string.Format("{0};", query);
        }
    }
}