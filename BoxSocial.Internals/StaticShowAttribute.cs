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

namespace BoxSocial.Internals
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class StaticShowAttribute : Attribute
    {
        private string slug;
        private string stub;
        private int order;

        public string Stub
        {
            get
            {
                return stub;
            }
        }

        public string Slug
        {
            get
            {
                return slug;
            }
        }

        public int Order
        {
            get
            {
                return order;
            }
        }

        public StaticShowAttribute(string slug)
            : this(slug, -1)
        {
        }

        public StaticShowAttribute(string stub, string slug)
            : this(stub, slug, -1)
        {
        }

        public StaticShowAttribute(string slug, int order)
            : this(null, slug, order)
        {
        }

        public StaticShowAttribute(string stub, string slug, int order)
        {
            this.stub = stub;
            this.slug = slug;
            this.order = order;
        }
    }
}
