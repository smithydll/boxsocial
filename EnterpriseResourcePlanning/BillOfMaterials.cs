/*
 * Box Social™
 * http://boxsocial.net/
 * Copyright © 2007, David Lachlan Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
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
    public class BillOfMaterials : NumberedItem
    {
        [DataField("bom_id", DataFieldKeys.Primary)]
        private long bomId;
        [DataField("document_id")]
        private long documentId;
        [DataField("bom_parent_id")]
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
                    document = new Document(core, documentId);
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
                return core.Uri.AppendSid(string.Format("{0}indented-bom/",
                        BomDocument.Owner.UriStub));
            }
        }
    }

    public class InvalidBillOfMaterialsException : Exception
    {
    }
}
