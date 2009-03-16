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
    public sealed class ShowAttribute : Attribute
    {
        private string slug;
        private string stub;
        private AppPrimitives primitives;
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

        public AppPrimitives Primitives
        {
            get
            {
                return primitives;
            }
        }

        public int Order
        {
            get
            {
                return order;
            }
        }

        public ShowAttribute(string slug, AppPrimitives primitives)
            : this(slug, primitives, -1)
        {
        }

        public ShowAttribute(string stub, string slug, AppPrimitives primitives) :
            this(stub, slug, primitives, -1)
        {
        }

        public ShowAttribute(string slug, AppPrimitives primitives, int order)
            : this(null, slug, primitives, order)
        {
        }

        public ShowAttribute(string stub, string slug, AppPrimitives primitives, int order)
        {
            this.stub = stub;
            this.slug = slug;
            this.primitives = primitives;
            this.order = order;
        }
    }
}
