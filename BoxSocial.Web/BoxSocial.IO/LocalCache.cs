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
        private Http http;

        public LocalCache(Http http)
        {
            this.http = http;
            if (http != null && http.Cache != null)
            {
                cache = http.Cache;
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
                cache.Insert(key, value, null, System.Web.Caching.Cache.NoAbsoluteExpiration, expiresIn, priority, null);
            }
        }
    }
}
