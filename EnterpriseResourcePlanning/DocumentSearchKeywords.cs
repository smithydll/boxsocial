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
using System.Reflection;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;

namespace BoxSocial.Applications.EnterpriseResourcePlanning
{
    [DataTable("erp_wordlist")]
    public class DocumentSearchKeywords : NumberedItem
    {
        [DataField("keyword_id", DataFieldKeys.Primary)]
        private long keywordId;
        [DataField("keyword_word", 31)]
        private string keyword;

        public DocumentSearchKeywords(Core core, DataRow keywordRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(DocumentSearchKeywords_ItemLoad);

            try
            {
                loadItemInfo(keywordRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidDocumentSearchKeywordsException();
            }
        }

        void DocumentSearchKeywords_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return keywordId;
            }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class InvalidDocumentSearchKeywordsException : Exception
    {
    }
}
