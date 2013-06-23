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
using System.IO;
using System.Collections.Generic;
using System.Resources;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;

namespace BoxSocial.IO
{
    public class QueryCache
    {
        private static Object queryLock = new object();
        private static Dictionary<long, string> queries = new Dictionary<long, string>();

        public static Query GetQuery(Type type, long typeId)
        {
            lock (queryLock)
            {
                return Query.FromStub(type, queries[typeId]);
            }
        }

        public static bool HasQuery(long typeId)
        {
            bool hasQuery = false;

            lock (queryLock)
            {
                if (queries != null && queries.ContainsKey(typeId))
                {
                    hasQuery = true;
                }
            }

            return hasQuery;
        }

        public static bool populateQueryCache()
        {
            System.Web.Caching.Cache cache;
            object o = null;
            object p = null;

            if (HttpContext.Current != null && HttpContext.Current.Cache != null)
            {
                cache = HttpContext.Current.Cache;
            }
            else
            {
                cache = new Cache();
            }

            if (cache != null)
            {
                try
                {
                    o = cache.Get("queries");
                    p = cache.Get("fields");
                    return true;
                }
                catch (NullReferenceException)
                {
                }
            }

            lock (queryLock)
            {
                if (o != null && o.GetType() == typeof(System.Collections.Generic.Dictionary<long, string>))
                {
                    queries = (Dictionary<long, string>)o;
                }
                else
                {
                    queries = new Dictionary<long, string>();

                    if (cache != null)
                    {
                        cache.Add("queries", queries, null, Cache.NoAbsoluteExpiration, new TimeSpan(8, 0, 0), CacheItemPriority.High, null);
                    }
                }
            }
            return false;
        }

        public static void AddQueryToCache(long typeId, Query query)
        {
            System.Web.Caching.Cache cache;

            if (HttpContext.Current != null && HttpContext.Current.Cache != null)
            {
                cache = HttpContext.Current.Cache;
            }
            else
            {
                cache = new Cache();
            }

            if (cache != null)
            {
                lock (queryLock)
                {
                    if (!queries.ContainsKey(typeId))
                    {
                        queries.Add(typeId, query.ToString());
                        cache.Add("queries", queries, null, Cache.NoAbsoluteExpiration, new TimeSpan(8, 0, 0), CacheItemPriority.High, null);
                    }
                }
            }
        }
    }
}
