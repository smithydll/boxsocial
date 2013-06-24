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
using System.Web;
using System.Web.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BoxSocial.Internals
{
    public class SolrSearch : Search
    {
        private string server;

        public SolrSearch(Core core, string server)
            : base(core)
        {
            this.server = server;
        }

        private void Initialise()
        {
            if (!initialised)
            {
                initialised = true;
            }
        }

        public override void Dispose()
        {
            if (initialised)
            {
            }
        }

        public override SearchResult DoSearch(string input, int pageNumber, Primitive filterByOwner, Type filterByType)
        {
            Initialise();

            int perPage = 10;
            int start = (pageNumber - 1) * perPage;

            List<ISearchableItem> results = new List<ISearchableItem>();

            return new SearchResult(results, 0 /*totalResults*/);
        }

        public override bool Index(ISearchableItem item, params SearchField[] customFields)
        {
            Initialise();

            var obj = new string[] { };
            string postVal = JsonConvert.SerializeObject(obj);

            return true;
        }

        public override bool UpdateIndex(ISearchableItem item, params SearchField[] customFields)
        {
            Initialise();

            DeleteFromIndex(item);
            return Index(item, customFields);
        }

        public override bool DeleteFromIndex(ISearchableItem item)
        {
            Initialise();


            return true;
        }
    }
}
