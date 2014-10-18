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
using System.Data.OleDb;
using System.Reflection;
using System.Web;
using System.Web.Caching;

namespace BoxSocial.IO
{
    public class LocalCache : Cache
    {
        private System.Web.Caching.Cache cache;
        private Dictionary<string, object> cache2;
        private Http http;

        public LocalCache(Http http)
        {
            this.http = http;
            if (http != null && http.Cache != null)
            {
                cache = http.Cache;
            }
            else
            {
                //cache = new System.Web.Caching.Cache();
                cache2 = new Dictionary<string, object>();
            }
        }

        public override object GetCached(string key)
        {
            try
            {
                if (cache != null)
                {
                    return cache.Get(key);
                }
                else if (cache2 != null)
                {
                    object returnValue = null;
                    cache2.TryGetValue(key, out returnValue);
                    return returnValue;
                }
            }
            catch (NullReferenceException)
            {                
            }

            return null;
        }

        public override void SetCached(string key, object value, TimeSpan expiresIn, CacheItemPriority priority)
        {
            if (cache != null)
            {
                try
                {
                    cache.Insert(key, value, null, System.Web.Caching.Cache.NoAbsoluteExpiration, expiresIn, priority, null);
                }
                catch (InvalidOperationException)
                {
                    // Not sure why failed, but I guess will try to cache again and succeed
                    // System.Web.HttpUnhandledException: Exception of type 'System.Web.HttpUnhandledException' was thrown. ---> System.InvalidOperationException: Operation is not valid due to the current state of the object
                }
            }
            else if (cache2 != null)
            {
                cache2[key] = value;
            }
        }

        public override void Close()
        {
            if (http == null)
            {
                cache2 = null;
            }
        }
    }
}
