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

    public abstract class Search
    {
        protected Core core;
        protected bool initialised;

        public Search(Core core)
        {
            this.core = core;
            initialised = false;
        }

        abstract public SearchResult DoSearch(string input, int pageNumber, Primitive filterByOwner, Type filterByType);

        abstract public bool Index(ISearchableItem item, params SearchField[] customFields);

        abstract public bool UpdateIndex(ISearchableItem item, params SearchField[] customFields);

        abstract public bool DeleteFromIndex(ISearchableItem item);

        abstract public void Dispose();
    }
}
