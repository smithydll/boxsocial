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

namespace BoxSocial.Internals
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ShowAttribute : Attribute
    {
        private string slug;
        private string cleanSlug;
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

        public string CleanSlug
        {
            get
            {
                return cleanSlug;
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
            bool pathIsRegex = false;
            string path = slug;
            string clean = string.Empty;

            if (path.StartsWith(@"^") && path.EndsWith(@"$"))
            {
                path = path.Substring(1, path.Length - 2).TrimStart(new char[] { '/' });
                if (path.EndsWith("(|/)"))
                {
                    path = path.Substring(0, path.Length - 4);
                }
                clean = path;
                pathIsRegex = true;
            }
            else
            {
                clean = path;
            }

            if (string.IsNullOrEmpty(stub))
            {
                string[] parts = path.Split(new char[] {'/'});
                if (parts.Length > 0)
                {
                    stub = parts[0];
                }
                else
                {
                    stub = string.Empty;
                }
            }

            if (!pathIsRegex)
            {
                slug = @"^/" + slug + @"(|/)$";
            }

            this.stub = stub;
            this.slug = slug;
            this.cleanSlug = clean;
            this.primitives = primitives;
            this.order = order;
        }
    }
}
