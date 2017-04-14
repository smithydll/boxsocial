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
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BoxSocial.Internals
{
    internal class ItemIndexDocument
    {
        public string Id;
        public long ItemId;
        public long ItemTypeId;
        public long OwnerId;
        public long OwnerTypeId;
        public long ApplicationId;
        public int ItemPublic;
        public string ItemTitle;
        public string ItemString;
        public string ItemTags;
    }

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

            
            BoolQuery query = new BoolQuery();
            QueryStringQuery bodyQuery = new QueryStringQuery();
            bodyQuery.Query = input;

            BoolQuery accessQuery = new BoolQuery();
            TermQuery accessPublicQuery = new TermQuery();
            accessPublicQuery.Field = "item_public";
            accessPublicQuery.Value = 1;

            if (core.Session.IsLoggedIn)
            {
                List<long> friends = core.Session.LoggedInMember.GetFriendsWithMeIds();

                BoolQuery accessFriendQuery = new BoolQuery();
                TermQuery friendQuery = new TermQuery();
                friendQuery.Field = "item_public";
                friendQuery.Value = 2;
                accessFriendQuery.Must = new List<QueryContainer> { friendQuery };

                string userTypeId = ItemType.GetTypeId(core, typeof(User)).ToString();
                foreach (long friendId in friends)
                {
                    BoolQuery ownerQuery = new BoolQuery();
                    TermQuery ownerIdQuery = new TermQuery();
                    ownerIdQuery.Field = "owner_id";
                    ownerIdQuery.Value = friendId;
                    TermQuery ownerTypeQuery = new TermQuery();
                    ownerTypeQuery.Field = "owner_type_id";
                    ownerTypeQuery.Value = userTypeId;

                    ownerQuery.Must = new List<QueryContainer> { ownerIdQuery, ownerTypeQuery };

                    accessFriendQuery.Should = new List<QueryContainer> { ownerQuery };
                }

                accessQuery.Should = new List<QueryContainer> { accessPublicQuery, accessFriendQuery };
            }

            query.Must = new List<QueryContainer> { bodyQuery, accessQuery };

            if (filterByType != null)
            {
                TermQuery typeQuery = new TermQuery();
                typeQuery.Field = "item_type_id";
                typeQuery.Value = ItemType.GetTypeId(core, filterByType);

                ((List<QueryContainer>)query.Must).Add(typeQuery);
            }

            if (filterByOwner != null)
            {
                TermQuery ownerIdQuery = new TermQuery();
                ownerIdQuery.Field = "owner_id";
                ownerIdQuery.Value = filterByOwner.Id;
                TermQuery ownerTypeIdQuery = new TermQuery();
                ownerTypeIdQuery.Field = "owner_type_id";
                ownerTypeIdQuery.Value = filterByOwner.TypeId;

                ((List<QueryContainer>)query.Must).Add(ownerIdQuery);
                ((List<QueryContainer>)query.Must).Add(ownerTypeIdQuery);
            }

            int totalResults = 0;

            SearchRequest request = new SearchRequest();
            request.Query = query;
            request.Size = perPage;
            request.From = start;

            ElasticsearchResponse<List<ItemIndexDocument>> response = client.Search<List<ItemIndexDocument>>(request);

            

            foreach (ItemIndexDocument doc in response.Body)
            {

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

            ItemIndexDocument writer = new ItemIndexDocument();
            IndexRequest<ItemIndexDocument> request = new IndexRequest<ItemIndexDocument>(writer);

            writer.Id = item.ItemKey.TypeId.ToString() + "," + item.Id.ToString();
            writer.ItemId = item.Id;
            writer.ItemTypeId = item.ItemKey.TypeId;
            writer.OwnerId = item.Owner.Id;
            writer.OwnerTypeId = item.Owner.ItemKey.TypeId;
            writer.ApplicationId = item.ItemKey.GetType(core).ApplicationId;
            writer.ItemPublic = isPublic;

            /*foreach (SearchField field in customFields)
            {
                writer.WritePropertyName(field.Key);
                writer.WriteValue(field.Value);
            }*/

            writer.ItemTitle = item.IndexingTitle;
            writer.ItemString = item.IndexingString;
            writer.ItemTitle = item.IndexingTags;

            client.Index<ItemIndexDocument>("", "", item.ItemKey.TypeId.ToString() + "," + item.Id.ToString(), request);

            return true;
        }

        public override bool UpdateIndex(ISearchableItem item, params SearchField[] customFields)
        {
            Initialise();

            //client.Update<ItemSearch>("", "", item.ItemKey.TypeId.ToString() + "," + item.Id.ToString(), writer);

            return Index(item, true, customFields);
        }

        public override bool DeleteFromIndex(ISearchableItem item)
        {
            Initialise();



            return true;
        }
    }
}
