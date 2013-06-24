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
using Lucene.Net;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
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

            QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "item_string", analyzer);

            BooleanQuery query = new BooleanQuery();
            Query bodyQuery = parser.Parse(input);

            query.Add(bodyQuery, Occur.MUST);

            BooleanQuery accessQuery = new BooleanQuery();
            TermQuery accessPublicQuery = new TermQuery(new Term("item_public", "1"));
            accessQuery.Add(accessPublicQuery, Occur.SHOULD);

            if (core.Session.IsLoggedIn)
            {
                List<long> friends = core.Session.LoggedInMember.GetFriendsWithMeIds();

                BooleanQuery accessFriendQuery = new BooleanQuery();
                TermQuery friendQuery = new TermQuery(new Term("item_public", "2"));
                accessFriendQuery.Add(friendQuery, Occur.MUST);

                string userTypeId = ItemType.GetTypeId(typeof(User)).ToString();
                foreach (long friendId in friends)
                {
                    BooleanQuery ownerQuery = new BooleanQuery();
                    TermQuery ownerIdQuery = new TermQuery(new Term("owner_id", friendId.ToString()));
                    TermQuery ownerTypeQuery = new TermQuery(new Term("owner_type_id", userTypeId));

                    ownerQuery.Add(ownerIdQuery, Occur.MUST);
                    ownerQuery.Add(ownerTypeQuery, Occur.MUST);

                    accessFriendQuery.Add(ownerQuery, Occur.SHOULD);
                }

                accessQuery.Add(accessFriendQuery, Occur.SHOULD);
            }

            query.Add(accessQuery, Occur.MUST);

            if (filterByType != null)
            {
                TermQuery typeQuery = new TermQuery(new Term("item_type_id", ItemType.GetTypeId(filterByType).ToString()));

                query.Add(typeQuery, Occur.MUST);
            }

            if (filterByOwner != null)
            {
                TermQuery ownerIdQuery = new TermQuery(new Term("owner_id", filterByOwner.Id.ToString()));
                TermQuery ownerTypeIdQuery = new TermQuery(new Term("owner_type_id", filterByOwner.TypeId.ToString()));

                query.Add(ownerIdQuery, Occur.MUST);
                query.Add(ownerTypeIdQuery, Occur.MUST);
            }

            NameValueCollection queryString = new NameValueCollection();
            queryString.Add("wt", "json");
            queryString.Add("start", start.ToString());
            queryString.Add("q", query.ToString());

            WebClient wc = new WebClient();
            wc.QueryString = queryString;
            string solrResultString = wc.DownloadString("http://" + server + "/solr/select");

            var solrResult = JsonConvert.DeserializeObject(solrResultString);

            int totalResults = 0;

            return new SearchResult(results, totalResults);
        }

        public override bool Index(ISearchableItem item, params SearchField[] customFields)
        {
            return Index(item, false, customFields);
        }

        private bool Index(ISearchableItem item, bool overwrite, params SearchField[] customFields)
        {
            Initialise();

            int isPublic = 1;

            if (item is IPermissibleItem)
            {
                IPermissibleItem pitem = (IPermissibleItem)item;

                isPublic = pitem.Access.IsPublic() ? 1 : 0;

                if (isPublic == 0)
                {
                    isPublic = pitem.Access.IsPrivateFriendsOrMembers() ? 2 : 0;
                }
            }

            if (item is IPermissibleSubItem)
            {
                IPermissibleItem pitem = ((IPermissibleSubItem)item).PermissiveParent;

                isPublic = pitem.Access.IsPublic() ? 1 : 0;

                if (isPublic == 0)
                {
                    isPublic = pitem.Access.IsPrivateFriendsOrMembers() ? 2 : 0;
                }
            }

            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();
                writer.WritePropertyName("add");
                writer.WriteStartObject();
                writer.WritePropertyName("overwrite");
                writer.WriteValue(overwrite);
                writer.WritePropertyName("doc");
                writer.WriteStartObject();
                writer.WritePropertyName("id");
                writer.WriteValue(item.Id.ToString());
                writer.WritePropertyName("item_type_id");
                writer.WriteValue(item.ItemKey.TypeId.ToString());
                writer.WritePropertyName("owner_id");
                writer.WriteValue(item.Owner.Id.ToString());
                writer.WritePropertyName("owner_type_id");
                writer.WriteValue(item.Owner.ItemKey.TypeId.ToString());
                writer.WritePropertyName("application_id");
                writer.WriteValue(item.ItemKey.ApplicationId.ToString());
                writer.WritePropertyName("item_public");
                writer.WriteValue(isPublic.ToString());

                /*foreach (SearchField field in customFields)
                {
                    writer.WritePropertyName(field.Key);
                    writer.WriteValue(field.Value);
                }*/

                writer.WritePropertyName("item_title");
                writer.WriteValue(item.IndexingTitle);
                writer.WritePropertyName("item_string");
                writer.WriteValue(item.IndexingString);
                writer.WritePropertyName("item_tags");
                writer.WriteValue(item.IndexingTags);

                writer.WriteEndObject();
                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            NameValueCollection values = new NameValueCollection();


            WebClient wc = new WebClient();
            wc.Headers[HttpRequestHeader.ContentType] = "type:application/json";
            wc.UploadString("http://" + server + "/solr/update/json", sb.ToString());

            return true;
        }

        public override bool UpdateIndex(ISearchableItem item, params SearchField[] customFields)
        {
            Initialise();

            //DeleteFromIndex(item);
            return Index(item, true, customFields);
        }

        public override bool DeleteFromIndex(ISearchableItem item)
        {
            Initialise();

            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();
                writer.WritePropertyName("delete");
                writer.WriteStartObject();
                
                writer.WritePropertyName("id");
                writer.WriteValue(item.Id.ToString());
                writer.WritePropertyName("item_type_id");
                writer.WriteValue(item.ItemKey.TypeId.ToString());

                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            WebClient wc = new WebClient();
            wc.Headers[HttpRequestHeader.ContentType] = "type:application/json";
            wc.UploadString("http://" + server + "/solr/update/json", sb.ToString());

            return true;
        }
    }
}
