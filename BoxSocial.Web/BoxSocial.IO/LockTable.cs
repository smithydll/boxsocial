using System;
using System.Collections.Generic;
using System.Text;

namespace BoxSocial.IO
{
    public sealed class LockTable : Query
    {
        private string table;

        public LockTable(string tableName)
        {
            table = tableName;
        }

        public override string ToString()
        {
            string query = string.Format("LOCK TABLES {0}",
                table);

            return string.Format("{0};", query);
        }
    }
}
