﻿/*
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
using Lucene.Net;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace BoxSocial.Internals
{
    /*public enum SearchCriteria
    {
        Name,
        Email,
        Age,
        Gender,
        School,
        Sexuality,
    }

    public class SearchCriterion
    {
    }*/

    public struct SearchField
    {
        private string key;
        private string value;

        public string Key
        {
            get
            {
                return key;
            }
        }

        public string Value
        {
            get
            {
                return value;
            }
        }

        public SearchField(string key, string value)
        {
            this.key = key;
            this.value = value;
        }
    }

    public struct SearchResult
    {
        private int results;
        private List<ISearchableItem> items;

        public int Results
        {
            get
            {
                return results;
            }
        }

        public List<ISearchableItem> Items
        {
            get
            {
                return items;
            }
        }

        public SearchResult(List<ISearchableItem> items, int results)
        {
            this.items = items;
            this.results = results;
        }
    }

    public class Search
    {
        Core core;
        Directory directory;
        Analyzer analyzer;
        IndexWriter writer;

        public Search(Core core)
        {
            this.core = core;

            directory = FSDirectory.Open(HttpContext.Current.Server.MapPath("./lucene/"));
            analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
            writer = new IndexWriter(directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

            IndexReader red = IndexReader.Open(directory, true);
            int totDocs = red.MaxDoc;
            red.Dispose();


        }

        ~Search()
        {
            writer.Dispose();
        }

        public SearchResult DoSearch(string input, int pageNumber, Primitive filterByOwner, Type filterByType)
        {
            int perPage = 10;
            int start = (pageNumber - 1) * perPage;

            List<ISearchableItem> results = new List<ISearchableItem>();
            List<ItemKey> itemKeys = new List<ItemKey>();
            List<long> applicationIds = new List<long>();

            IndexSearcher searcher = new IndexSearcher(directory);

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

                string userTypeId =  ItemType.GetTypeId(typeof(User)).ToString();
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

            TopScoreDocCollector collector = TopScoreDocCollector.Create(start + perPage, true);

            searcher.Search(query, collector);

            ScoreDoc[] hits = collector.TopDocs().ScoreDocs;

            int totalResults = collector.TotalHits;
            int returnResults = hits.Length;

            int end = Math.Min(hits.Length, start + perPage);

            for (int i = start; i < end; i++)
            {
                Document doc = searcher.Doc(hits[i].Doc);

                long itemId = 0;
                long itemTypeId = 0;
                long applicationId = 0;

                long.TryParse(doc.GetField("item_id").StringValue, out itemId);
                long.TryParse(doc.GetField("item_type_id").StringValue, out itemTypeId);
                long.TryParse(doc.GetField("application_id").StringValue, out applicationId);

                ItemKey key = new ItemKey(itemId, itemTypeId);

                if (!applicationIds.Contains(applicationId))
                {
                    applicationIds.Add(applicationId);
                }
                
                itemKeys.Add(key);
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

            foreach (ItemKey key in itemKeys)
            {
                NumberedItem thisItem = null;
                try
                {
                    thisItem = NumberedItem.Reflect(core, key);
                }
                catch
                {
                    // sometimes we cannot load an item for unknown reasons, we just have to continue trucking on
                }

                if (thisItem != null)
                {
                    results.Add((ISearchableItem)thisItem);
                }
            }

            return new SearchResult(results, totalResults);
        }

        public bool Index(ISearchableItem item, params SearchField[] customFields)
        {
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

            Document doc = new Document();
            /* we store numbers are strings, because a NumericField is 32 bit, and our IDs are all 64 bit.
             * These Ids are all stored to make things faster to lookup, none of the IDs are transferable,
             * so they will be static and therefore suitable for indexing. */
            doc.Add(new Field("item_id", item.Id.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("item_type_id", item.ItemKey.TypeId.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("owner_id", item.Owner.Id.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("owner_type_id", item.Owner.ItemKey.TypeId.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("application_id", item.ItemKey.ApplicationId.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            /* Because of the dynamic nature of ACLs, they can only be effectively evaluated when querying results */
            doc.Add(new Field("item_public", isPublic.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            foreach (SearchField field in customFields)
            {
                doc.Add(new Field(field.Key, field.Value, Field.Store.YES, Field.Index.NOT_ANALYZED));
            }
            doc.Add(new Field("item_title", item.IndexingTitle, Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("item_string", item.IndexingString, Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("item_tags", item.IndexingTags, Field.Store.YES, Field.Index.ANALYZED));
            writer.AddDocument(doc);

            writer.Commit();
            
            return true;
        }

        public bool UpdateIndex(ISearchableItem item, params SearchField[] customFields)
        {
            DeleteFromIndex(item);
            return Index(item, customFields);
        }

        public bool DeleteFromIndex(ISearchableItem item)
        {
            writer.DeleteDocuments(new Term("item_id", item.Id.ToString()));

            return true;
        }
    }
}
