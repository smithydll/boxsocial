/*
 * Box Social™
 * http://boxsocial.net/
  * Copyright © 2007, David Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 2 of
 * the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace BoxSocial.IO
{

    public enum QueryOperations
    {
        Addition,
        Subtraction,
        Multiplication,
        Division,
        BinaryAnd,
        BinaryOr,
    }

    public class QueryOperation
    {
        private string field;
        private QueryOperations operation;
        private object value;

        public QueryOperation(string field, QueryOperations operation, object value)
        {
            this.field = field;
            this.operation = operation;
            this.value = value;
        }

        public override string ToString()
        {
            return string.Format("`{0}` {1} {2}", field, OperationToString(operation), Query.ObjectToSql(value));
        }

        private static string OperationToString(QueryOperations operation)
        {
            switch (operation)
            {
                case QueryOperations.Addition:
                    return "+";
                case QueryOperations.Subtraction:
                    return "-";
                case QueryOperations.Multiplication:
                    return "*";
                case QueryOperations.Division:
                    return "/";
                case QueryOperations.BinaryAnd:
                    return "&";
                case QueryOperations.BinaryOr:
                    return "|";
            }
            return "!";
        }
    }
}
