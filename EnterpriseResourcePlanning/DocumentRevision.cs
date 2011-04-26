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
    public enum DocumentStatus : byte
    {
        Unreleased = 0,
        Current = 1,
        Superseded = 2,
    }

    [DataTable("erp_document_revisions")]
    public class DocumentRevision : NumberedItem
    {
        [DataField("document_revision_id", DataFieldKeys.Primary)]
        private long documentRevisionId;
        [DataField("document_id")]
        private long documentId;
        [DataField("document_revision", 3)]
        private string documentRevision;
        [DataField("document_revision_status")]
        private byte documentRevisionStatus;
        //private string revisionComment;

        public DocumentRevision(Core core, long documentRevisionId)
            : base (core)
        {
            ItemLoad += new ItemLoadHandler(DocumentRevision_ItemLoad);

            try
            {
                LoadItem(documentRevisionId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidDocumentRevisionException();
            }
        }

        public DocumentRevision(Core core, DataRow documentRevisionDataRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(DocumentRevision_ItemLoad);

            try
            {
                loadItemInfo(documentRevisionDataRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidDocumentRevisionException();
            }
        }

        void DocumentRevision_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return documentRevisionId;
            }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class InvalidDocumentRevisionException : Exception
    {
    }
}
