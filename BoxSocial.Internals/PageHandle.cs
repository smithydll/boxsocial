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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public sealed class PageHandle : IComparable
    {
        private int order;
        private string expression;

        /// <summary>
        /// The function that gets executed if a match
        /// </summary>
        private Core.PageHandler handler;

        public string Expression
        {
            get
            {
                return expression;
            }
        }

        public PageHandle(string expression, Core.PageHandler pageHandle, int order)
        {
            this.order = order;
            this.handler = pageHandle;
            this.expression = expression;
        }

        public void Execute(Core core, object sender)
        {
            this.handler(core, sender);
        }

        public int CompareTo(object obj)
        {
            if (!(obj is PageHandle)) return -1;
            return order.CompareTo(((PageHandle)obj).order);
        }
    }
}
