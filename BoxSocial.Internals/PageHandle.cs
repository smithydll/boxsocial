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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public sealed class PageHandle : IComparable
    {
        private AppPrimitives primitives;
        private int order;
        private string expression;
        private bool staticPage;

        /// <summary>
        /// The function that gets executed if a match
        /// </summary>
        private Core.PageHandler handler;

        public AppPrimitives Primitives
        {
            get
            {
                return primitives;
            }
        }

        public string Expression
        {
            get
            {
                return expression;
            }
        }

        public string MethodName
        {
            get
            {
                return handler.Method.Name;
            }
        }

        public bool StaticPage
        {
            get
            {
                return staticPage;
            }
        }

        public PageHandle(AppPrimitives primitives, string expression, Core.PageHandler pageHandle, int order, bool staticPage)
        {
            this.order = order;
            this.handler = pageHandle;
            this.expression = expression;
            this.primitives = primitives;
            this.staticPage = staticPage;
        }

        public void Execute(Core core, object sender)
        {
            //HttpContext.Current.Response.Write(handler.Method.Name);
            this.handler(core, sender);
        }

        public int CompareTo(object obj)
        {
            if (!(obj is PageHandle)) return -1;
            return order.CompareTo(((PageHandle)obj).order);
        }
    }
}
