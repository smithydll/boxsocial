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
        Analyzer analyzer;

        public SolrSearch(Core core, string server)
            : base(core)
        {
            this.server = server;
        }

        private void Initialise()
        {
            if (!initialised)
            {
                analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
                initialised = true;
            }
        }

        public override void Dispose()
        {
            if (initialised)
            {
                analyzer.Dispose();
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

                string userTypeId = ItemType.GetTypeId(core, typeof(User)).ToString();
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
                TermQuery typeQuery = new TermQuery(new Term("item_type_id", ItemType.GetTypeId(core, filterByType).ToString()));

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
            string solrResultString = wc.DownloadString("http://" + server + "/select");

            //HttpContext.Current.Response.Write(solrResultString + "<br />");

            JsonTextReader reader = new JsonTextReader(new StringReader(solrResultString));

            int totalResults = 0;
            List<Dictionary<string, string>> docs = new List<Dictionary<string, string>>();
            bool readingResponse = false;
            bool inDocument = false;
            string lastToken = string.Empty;
            int current = -1;

            while (reader.Read())
            {
                if (readingResponse)
                {
                    if (reader.Value != null)
                    {
                        if (inDocument)
                        {
                            if (reader.TokenType == JsonToken.PropertyName)
                            {
                                lastToken = reader.Value.ToString();
                                //HttpContext.Current.Response.Write(lastToken + "<br />\n");
                            }
                            else
                            {
                                docs[current].Add(lastToken, reader.Value.ToString());
                                lastToken = string.Empty;
                            }
                            /*else if (reader.TokenType == JsonToken.Integer)
                            {
                                docs[docs.Count - 1].Add(lastToken, reader.Value.ToString());
                            }
                            else if (reader.TokenType == JsonToken.Boolean)
                            {
                                docs[docs.Count - 1].Add(lastToken, reader.Value.ToString());
                            }
                            else if (reader.TokenType == JsonToken.Float)
                            {
                                docs[docs.Count - 1].Add(lastToken, reader.Value.ToString());
                            }*/
                        }
                        else
                        {
                            if (reader.TokenType == JsonToken.PropertyName && (string)reader.Value == "numFound")
                            {
                                lastToken = reader.Value.ToString();
                            }
                            if (reader.TokenType == JsonToken.PropertyName && (string)reader.Value == "docs")
                            {
                                lastToken = reader.Value.ToString();
                            }
                            if (reader.TokenType == JsonToken.Integer && lastToken == "numFound")
                            {
                                totalResults = int.Parse(reader.Value.ToString());
                                lastToken = string.Empty;
                                //HttpContext.Current.Response.Write(totalResults + " results<br />\n");
                            }
                        }
                    }
                    else
                    {
                        if (reader.TokenType == JsonToken.StartArray && lastToken == "docs")
                        {
                            inDocument = true;
                            lastToken = string.Empty;
                        }
                        if (reader.TokenType == JsonToken.StartObject && inDocument)
                        {
                            docs.Add(new Dictionary<string,string>());
                            current++;
                        }
                        if (reader.TokenType == JsonToken.EndArray && inDocument)
                        {
                            inDocument = false;
                        }
                    }
                }
                else
                {
                    if (reader.Value != null)
                    {
                        if (reader.TokenType == JsonToken.PropertyName && (string)reader.Value == "response")
                        {
                            readingResponse = true;
                        }
                    }

                }
            }

            for (int i = 0; i < docs.Count; i++)
            {
                long itemId = 0;
                long itemTypeId = 0;
                long applicationId = 0;

                long.TryParse(docs[i]["item_id"], out itemId);
                long.TryParse(docs[i]["item_type_id"], out itemTypeId);
                long.TryParse(docs[i]["application_id"], out applicationId);

                ItemKey key = new ItemKey(itemId, itemTypeId);

                if (!applicationIds.Contains(applicationId))
                {
                    applicationIds.Add(applicationId);
                }
                
                itemKeys.Add(key);
                //HttpContext.Current.Response.Write("item_id: " + itemId + ", item_type_id:" + itemTypeId + "<br />\n");
            }

            // Force each application with results to load
            for (int i = 0; i < applicationIds.Count; i++)
            {
                if (applicationIds[i] > 0)
                {
                    ApplicationEntry ae = new ApplicationEntry(core, applicationIds[i]);

                    BoxSocial.Internals.Application.LoadApplication(core, AppPrimitives.Any, ae);
                }
            }

            List<IPermissibleItem> tempResults = new List<IPermissibleItem>();

            foreach (ItemKey key in itemKeys)
            {
                core.ItemCache.RequestItem(key);
            }

            core.ItemCache.ExecuteQueue();
            foreach (ItemKey key in itemKeys)
            {
                try
                {
                    if (core.ItemCache.ContainsItem(key))
                    {
                        NumberedItem thisItem = core.ItemCache[key];

                        if (thisItem != null)
                        {
                            if (thisItem is IPermissibleItem)
                            {
                                tempResults.Add((IPermissibleItem)thisItem);
                            }
                            if (thisItem is IPermissibleSubItem)
                            {
                                tempResults.Add(((IPermissibleSubItem)thisItem).PermissiveParent);
                            }
                            results.Add((ISearchableItem)thisItem);
                        }
                    }
                    else
                    {
                        totalResults--;
                    }
                }
                catch (InvalidItemException)
                {
                }
            }

            if (tempResults.Count > 0)
            {
                core.AcessControlCache.CacheGrants(tempResults);
            }

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
                writer.WriteValue(item.ItemKey.TypeId.ToString() + "," + item.Id.ToString());
                writer.WritePropertyName("item_id");
                writer.WriteValue(item.Id.ToString());
                writer.WritePropertyName("item_type_id");
                writer.WriteValue(item.ItemKey.TypeId.ToString());
                writer.WritePropertyName("owner_id");
                writer.WriteValue(item.Owner.Id.ToString());
                writer.WritePropertyName("owner_type_id");
                writer.WriteValue(item.Owner.ItemKey.TypeId.ToString());
                writer.WritePropertyName("application_id");
                writer.WriteValue(item.ItemKey.GetType(core).ApplicationId.ToString());
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
            string response = wc.UploadString("http://" + server + "/update/json", sb.ToString());
            //HttpContext.Current.Response.Write(sb.ToString() + "<br />\r\n\r\n" + response + "<br />");
            wc.DownloadString("http://" + server + "/update?commit=true");

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
                writer.WriteValue(item.ItemKey.TypeId.ToString() + "," + item.Id.ToString());

                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            try
            {
                WebClient wc = new WebClient();
                wc.Headers[HttpRequestHeader.ContentType] = "type:application/json";
                wc.UploadString("http://" + server + "/update/json", sb.ToString());
                wc.DownloadString("http://" + server + "/update?commit=true");
            }
            catch
            {
            }

            return true;
        }
    }
}
