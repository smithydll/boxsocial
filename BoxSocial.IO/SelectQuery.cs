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

    public class TableSort
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
                    return String.Empty;
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
        private QueryCondition conditions;

        /// <summary>
        /// Type of join
        /// </summary>
        public JoinTypes Type;

        /// <summary>
        /// 
        /// </summary>
        public string JoinTable;

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

        public TableJoin(JoinTypes type, DataField joinField, DataField tableField)
        {
            conditions = new QueryCondition();

            Type = type;
            JoinTable = joinField.Table;
            JoinField = joinField.Name;
            Table = tableField.Table;
            TableField = tableField.Name;

            AddCondition(joinField.ToString(), tableField);
        }

        public TableJoin(JoinTypes type, string joinTable, string table, string joinField, string tableField)
        {
            conditions = new QueryCondition();

            Type = type;
            JoinTable = joinTable;
            Table = table;
            JoinField = joinField;
            TableField = tableField;

            AddCondition(new DataField(joinTable, joinField).ToString(), new DataField(table, tableField));
        }

        public QueryCondition AddCondition(QueryOperation field, object value)
        {
            return AddCondition(field.ToString(), value);
        }

        public QueryCondition AddCondition(DataField field, object value)
        {
            if (conditions.Count == 0)
            {
                return conditions.AddCondition(ConditionRelations.First, field.ToString(), ConditionEquality.Equal, value);
            }
            else
            {
                return conditions.AddCondition(ConditionRelations.And, field.ToString(), ConditionEquality.Equal, value);
            }
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

        public QueryCondition AddCondition(ConditionRelations relation, QueryOperation field, object value)
        {
            return conditions.AddCondition(relation, field, value);
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

        public QueryCondition AddCondition(QueryOperation field, ConditionEquality equality, object value)
        {
            return AddCondition(field.ToString(), equality, value);
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

        public QueryCondition AddCondition(ConditionRelations relation, QueryOperation field, ConditionEquality equality, object value)
        {
            return AddCondition(relation, field.ToString(), equality, value);
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
            switch (Type)
            {
                case JoinTypes.Inner:
                    return string.Format("INNER JOIN `{0}` ON {1}",
                        Table, conditions.ToString());
                case JoinTypes.Left:
                    return string.Format("LEFT JOIN `{0}` ON {1}",
                        Table, conditions.ToString());
                case JoinTypes.Right:
                    return string.Format("RIGHT JOIN `{0}` ON {1}",
                        Table, conditions.ToString());
                default:
                    return String.Empty;
            }
        }
    }

    public sealed class SelectQuery : Query
    {
        private QueryStub stub;
        private List<string> tables;
        private List<string> fields;
        private List<string> groupings;
        private QueryCondition conditions;
        private List<TableJoin> joins;
        private List<TableSort> sorts;
        private int limitCount = -1;
        private int limitStart = -1;
        private bool isDistinct = false;

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

        public int LimitStart
        {
            get
            {
                return limitStart;
            }
            set
            {
                limitStart = value;
            }
        }

        public bool Distinct
        {
            set
            {
                isDistinct = value;
            }
        }

        internal SelectQuery(Type type, QueryStub stub)
        {
            this.stub = stub;

            tables = new List<string>();
            fields = new List<string>();
            groupings = new List<string>();
            conditions = new QueryCondition();
            joins = new List<TableJoin>();
            sorts = new List<TableSort>();

            tables.Add(DataFieldAttribute.GetTable(type));
        }

        public SelectQuery(string baseTableName)
        {
            tables = new List<string>();
            fields = new List<string>();
            groupings = new List<string>();
            conditions = new QueryCondition();
            joins = new List<TableJoin>();
            sorts = new List<TableSort>();

            tables.Add(baseTableName);
        }
        
        public SelectQuery(Type type)
        {
            tables = new List<string>();
            fields = new List<string>();
            groupings = new List<string>();
            conditions = new QueryCondition();
            joins = new List<TableJoin>();
            sorts = new List<TableSort>();

            tables.Add(DataFieldAttribute.GetTable(type));
        }

        public void AddFields(params string[] fields)
        {
            this.fields.AddRange(fields);
        }

        public void AddField(QueryFunction field)
        {
            this.fields.Add(field.ToString());
        }

        public void AddField(DataField field)
        {
            this.fields.Add(field.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">Type of table join</param>
        /// <param name="table">Table adjoining the parent</param>
        /// <param name="joinField">Join field in the table being adjoined to the parent</param>
        /// <param name="tableField">Join field in the parent table</param>
        public TableJoin AddJoin(JoinTypes type, string table, string joinField, string tableField)
        {
            TableJoin tj = new TableJoin(type, this.tables[0], table, joinField, tableField);
            joins.Add(tj);
            return tj;
        }

        public TableJoin AddJoin(JoinTypes type, DataField joinField, DataField tableField)
        {
            TableJoin tj = new TableJoin(type, joinField, tableField);
            joins.Add(tj);
            return tj;
        }

        public void AddSort(SortOrder order, string field)
        {
            sorts.Add(new TableSort(order, field));
        }

        public void AddGrouping(params string[] fields)
        {
            this.groupings.AddRange(fields);
        }

        public QueryCondition AddBitFieldCondition(string field, byte bitmap)
        {
            return AddCondition(new QueryOperation(field, QueryOperations.BinaryAnd, bitmap), ConditionEquality.NotEqual, false);
        }

        public QueryCondition AddBitFieldCondition(string field, ushort bitmap)
        {
            return AddCondition(new QueryOperation(field, QueryOperations.BinaryAnd, bitmap), ConditionEquality.NotEqual, false);
        }

        public QueryCondition AddBitFieldCondition(string field, uint bitmap)
        {
            return AddCondition(new QueryOperation(field, QueryOperations.BinaryAnd, bitmap), ConditionEquality.NotEqual, false);
        }

        public QueryCondition AddBitFieldCondition(string field, ulong bitmap)
        {
            return AddCondition(new QueryOperation(field, QueryOperations.BinaryAnd, bitmap), ConditionEquality.NotEqual, false);
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

        public QueryCondition AddCondition(QueryFunction field, object value)
        {
            if (conditions.Count == 0)
            {
                return conditions.AddCondition(ConditionRelations.First, field.ToString(), ConditionEquality.Equal, value);
            }
            else
            {
                return conditions.AddCondition(ConditionRelations.And, field.ToString(), ConditionEquality.Equal, value);
            }
        }

        public QueryCondition AddCondition(QueryOperation field, object value)
        {
            if (conditions.Count == 0)
            {
                return conditions.AddCondition(ConditionRelations.First, field.ToString(), ConditionEquality.Equal, value);
            }
            else
            {
                return conditions.AddCondition(ConditionRelations.And, field.ToString(), ConditionEquality.Equal, value);
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

        public QueryCondition AddCondition(ConditionRelations relation, QueryFunction field, object value)
        {
            if (conditions.Count == 0)
            {
                return conditions.AddCondition(ConditionRelations.First, field.ToString(), ConditionEquality.Equal, value);
            }
            else
            {
                return conditions.AddCondition(relation, field.ToString(), ConditionEquality.Equal, value);
            }
        }

        public QueryCondition AddCondition(DataField field, object value)
        {
            if (conditions.Count == 0)
            {
                return conditions.AddCondition(ConditionRelations.First, field.ToString(), ConditionEquality.Equal, value);
            }
            else
            {
                return conditions.AddCondition(ConditionRelations.And, field.ToString(), ConditionEquality.Equal, value);
            }
        }

        public QueryCondition AddCondition(DataField field, ConditionEquality equality, object value)
        {
            if (conditions.Count == 0)
            {
                return conditions.AddCondition(ConditionRelations.First, field.ToString(), equality, value);
            }
            else
            {
                return conditions.AddCondition(ConditionRelations.And, field.ToString(), equality, value);
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

        public QueryCondition AddCondition(QueryFunction field, ConditionEquality equality, object value)
        {
            if (conditions.Count == 0)
            {
                return conditions.AddCondition(ConditionRelations.First, field.ToString(), equality, value);
            }
            else
            {
                return conditions.AddCondition(ConditionRelations.And, field.ToString(), equality, value);
            }
        }

        public QueryCondition AddCondition(QueryOperation field, ConditionEquality equality, object value)
        {
            if (conditions.Count == 0)
            {
                return conditions.AddCondition(ConditionRelations.First, field.ToString(), equality, value);
            }
            else
            {
                return conditions.AddCondition(ConditionRelations.And, field.ToString(), equality, value);
            }
        }

        public QueryCondition AddCondition(ConditionRelations relation, DataField field, ConditionEquality equality, object value)
        {
            if (conditions.Count == 0)
            {
                return conditions.AddCondition(ConditionRelations.First, field.ToString(), equality, value);
            }
            else
            {
                return conditions.AddCondition(relation, field.ToString(), equality, value);
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

        public QueryCondition AddCondition(ConditionRelations relation, ConditionEquality field, ConditionEquality equality, object value)
        {
            if (conditions.Count == 0)
            {
                return conditions.AddCondition(ConditionRelations.First, field.ToString(), equality, value);
            }
            else
            {
                return conditions.AddCondition(relation, field.ToString(), equality, value);
            }
        }

        public override string ToString()
        {
            StringBuilder query = new StringBuilder();

            string newFields = string.Empty;

            if (stub != null)
            {
                query.Append(stub.Stub.TrimEnd(new char[] { ';', ' ' }) + " ");

                if (fields.Count > 0)
                {

                    foreach (string field in fields)
                    {
                        newFields += ", ";
                        newFields += field;
                    }
                }
            }
            else
            {
                query.Append("SELECT");

                if (isDistinct)
                {
                    query.Append(" DISTINCT");
                }

                if (limitCount >= 0)
                {
                    query.Append(" SQL_CALC_FOUND_ROWS");
                }

                if (fields.Count > 0)
                {
                    bool first = true;
                    foreach (string field in fields)
                    {
                        if (first)
                        {
                            /*query = string.Format("{0} {1}",
                                query, field);*/
                            query.Append(" ");
                            query.Append(field);
                            first = false;
                        }
                        else
                        {
                            /*query = string.Format("{0}, {1}",
                                query, field);*/
                            query.Append(", ");
                            query.Append(field);
                        }
                    }
                }
                else
                {
                    /*query = string.Format("{0} *",
                                query);*/
                    query.Append(" *");
                }

                if (tables.Count > 0)
                {
                    bool first = true;
                    foreach (string table in tables)
                    {
                        if (first)
                        {
                            /*query = string.Format("{0} FROM {1}",
                                query, table);*/
                            query.Append(" FROM ");
                            query.Append(table);
                            first = false;
                        }
                        else
                        {
                            /*query = string.Format("{0}, {1}",
                                query, table);*/
                            query.Append(", ");
                            query.Append(table);
                        }
                    }
                }
            }

            if (joins.Count > 0)
            {
                foreach (TableJoin join in joins)
                {
                    /*query = string.Format("{0} {1}",
                            query, join.ToString());*/
                    query.Append(" ");
                    query.Append(join.ToString());
                }
            }


            if (conditions.Count > 0)
            {
                /*query = string.Format("{0} WHERE {1}",
                            query, conditions.ToString());*/
                query.Append(" WHERE ");
                query.Append(conditions.ToString());
            }

            if (sorts.Count > 0)
            {
                bool first = true;
                foreach (TableSort sort in sorts)
                {
                    if (first)
                    {
                        /*query = string.Format("{0} ORDER BY {1}",
                            query, sort.ToString());*/
                        query.Append(" ORDER BY ");
                        query.Append(sort.ToString());
                        first = false;
                    }
                    else
                    {
                        /*query = string.Format("{0}, {1}",
                            query, sort.ToString());*/
                        query.Append(", ");
                        query.Append(sort.ToString());
                    }
                }
            }

            if (groupings.Count > 0)
            {
                bool first = true;
                foreach (string field in groupings)
                {
                    if (first)
                    {
                        /*query = string.Format("{0} GROUP BY {1}",
                            query, field);*/
                        query.Append(" GROUP BY ");
                        query.Append(field);
                        first = false;
                    }
                    else
                    {
                        /*query = string.Format("{0}, {1}",
                            query, field);*/
                        query.Append(", ");
                        query.Append(field);
                    }
                }
            }

            if (limitCount >= 0 && limitStart >= 0)
            {
                /*query = string.Format("{0} LIMIT {2}, {1}",
                            query, limitCount, limitStart);*/
                query.AppendFormat(" LIMIT {1}, {0}", limitCount, limitStart);
            }
            else if (limitCount >= 0)
            {
                /*query = string.Format("{0} LIMIT {1}",
                            query, limitCount);*/
                query.Append(" LIMIT ");
                query.Append(limitCount.ToString());
            }

            /*return string.Format("{0};", query);*/
            query.Append(";");
            string retQuery = query.ToString();

            if (newFields != string.Empty)
            {
                int insertPoint = retQuery.IndexOf(" FROM ");
                if (insertPoint > 0)
                {
                    retQuery = retQuery.Insert(insertPoint, newFields);
                }
            }

            return retQuery;
        }
    }
}
