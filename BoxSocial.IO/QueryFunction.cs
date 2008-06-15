/*
 * Box Social™
 * http://boxsocial.net/
 * Copyright © 2007, David Lachlan Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
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
    public enum QueryFunctions
    {
        Count,
        Sum,
        Min,
        Max,
        Average,
        ToLowerCase,
        ToUpperCase,
    }

    public class QueryFunction
    {
        private string field;
        private QueryFunctions function;
        private string alias;

        public QueryFunction(string field, QueryFunctions function)
        {
            this.field = field;
            this.function = function;
            this.alias = null;
        }

        public QueryFunction(string field, QueryFunctions function, string alias)
        {
            this.field = field;
            this.function = function;
            this.alias = alias;
        }

        public override string ToString()
        {
            if (alias == null)
            {
                return string.Format("{1}(`{0}`)", field, FunctionToString(function));
            }
            else
            {
                return string.Format("{1}(`{0}`) AS `{2}`", field, FunctionToString(function), alias);
            }
        }

        private static string FunctionToString(QueryFunctions function)
        {
            switch (function)
            {
                case QueryFunctions.Sum:
                    return "SUM";
                case QueryFunctions.Min:
                    return "MIN";
                case QueryFunctions.Max:
                    return "MAX";
                case QueryFunctions.Count:
                    return "COUNT";
                case QueryFunctions.Average:
                    return "AVERAGE";
                case QueryFunctions.ToLowerCase:
                    return "LCASE";
                case QueryFunctions.ToUpperCase:
                    return "UCASE";
            }
            return "";
        }
    }
}
