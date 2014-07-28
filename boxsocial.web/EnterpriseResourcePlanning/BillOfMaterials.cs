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

    [DataTable("erp_bom")]
    public class BillOfMaterials : NumberedItem
    {
        [DataField("bom_id", DataFieldKeys.Primary)]
        private long bomId;
        [DataField("document_id", typeof(Document))]
        private long documentId;
        [DataField("bom_parent_id", typeof(Document))]
        private long bomParentId;
        [DataField("bom_quantity")]
        private int quantity;

        private Document document;

        public Document BomDocument
        {
            get
            {
                if (document == null || document.Id != documentId)
                {
                    //document = new Document(core, documentId);
                    document = (Document)core.ItemCache[new ItemKey(documentId, typeof(Document))];
                }
                return document;
            }
        }

        public BillOfMaterials(Core core, long bomId)
            : base (core)
        {
            ItemLoad += new ItemLoadHandler(BillOfMaterials_ItemLoad);

            try
            {
                LoadItem(bomId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidBillOfMaterialsException();
            }
        }

        public BillOfMaterials(Core core, DataRow bomDataRow)
            : this(core, bomDataRow, false)
        {
        }

        private BillOfMaterials(Core core, DataRow bomDataRow, bool containsDocument)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(BillOfMaterials_ItemLoad);

            try
            {
                loadItemInfo(bomDataRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidBillOfMaterialsException();
            }

            if (containsDocument)
            {
                document = new Document(core, bomDataRow);
            }
        }

        void BillOfMaterials_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return bomId;
            }
        }

        public override string Uri
        {
            get
            {
                return core.Hyperlink.AppendSid(string.Format("{0}bom/{1}",
                        BomDocument.Owner.UriStub, BomDocument.DocumentKey));
            }
        }

        public List<BillOfMaterials> GetBom()
        {
            List<BillOfMaterials> bom = new List<BillOfMaterials>();

            SelectQuery query = BillOfMaterials.GetSelectQueryStub(typeof(BillOfMaterials));
            query.AddFields(Document.GetFieldsPrefixed(typeof(Document)));
            query.AddJoin(JoinTypes.Inner, new DataField(typeof(Document), "document_id"), new DataField(typeof(BillOfMaterials), "document_id"));
            query.AddCondition("bom_parent_id", Id);

            DataTable bomDataTable = db.Query(query);

            if (bomDataTable.Rows.Count == 1)
            {
                bom.Add(new BillOfMaterials(core, bomDataTable.Rows[0], true));
            }

            return bom;
        }
    }

    public class InvalidBillOfMaterialsException : Exception
    {
    }
}
