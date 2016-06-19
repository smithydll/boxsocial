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
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Configuration;
using Elasticsearch.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BoxSocial.Internals
{
    public class ElasticSearch : Search
    {
        private string server;
        private ElasticLowLevelClient client;

        public ElasticSearch(Core core, string server)
            : base(core)
        {
            this.server = server;
        }

        private void Initialise()
        {
            if (!initialised)
            {
                Uri host = new Uri(server);
                ConnectionConfiguration config = new ConnectionConfiguration(host);
                client = new ElasticLowLevelClient(config);
                initialised = true;
            }
        }

        public override void Dispose()
        {
            if (initialised)
            {
                client = null;
            }
        }

        public override SearchResult DoSearch(string input, int pageNumber, Primitive filterByOwner, Type filterByType)
        {
            Initialise();

            int perPage = 10;
            int start = (pageNumber - 1) * perPage;

            List<ISearchableItem> results = new List<ISearchableItem>();
            List<ItemKey> itemKeys = new List<ItemKey>();
            List<long> applicationIds = new List<long>();

            int totalResults = 0;

            return new SearchResult(results, totalResults);
        }

        public override bool Index(ISearchableItem item, params SearchField[] customFields)
        {
            return Index(item, false, customFields);
        }

        private bool Index(ISearchableItem item, bool overwrite, params SearchField[] customFields)
        {

            return true;
        }

        public override bool UpdateIndex(ISearchableItem item, params SearchField[] customFields)
        {
            //Initialise();

            return Index(item, true, customFields);
        }

        public override bool DeleteFromIndex(ISearchableItem item)
        {

            return true;
        }
    }
}
