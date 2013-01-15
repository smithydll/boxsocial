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
        [DataField("document_id", DataFieldKeys.Unique, "u_document_id")]
        private long documentId;
        [DataField("document_item")]
        private ItemKey ownerKey;
        [DataField("document_revision", DataFieldKeys.Unique, "u_document_id", 10)]
        private string documentRevision;
        [DataField("revision_sequence")]
        private int revisionSequence;
        [DataField("document_revision_status")]
        private byte documentRevisionStatus;
        [DataField("document_storage_path", 128)]
        private string storagePath;
        [DataField("document_source_storage_path", 128)]
        private string sourceStoragePath;
        [DataField("revision_created_date")]
        private long documentRevisionCreatedDate;
        [DataField("revision_released_date")]
        private long documentRevisionReleasedDate;
        //private string revisionComment;

        public long DocumentId
        {
            get
            {
                return documentId;
            }
        }

        public string Revision
        {
            get
            {
                return documentRevision;
            }
        }

        public int Sequence
        {
            get
            {
                return revisionSequence;
            }
        }

        public DocumentStatus Status
        {
            get
            {
                return (DocumentStatus)documentRevisionStatus;
            }
            set
            {
                SetProperty("documentRevisionStatus", value);
            }
        }

        public string StoragePath
        {
            get
            {
                return storagePath;
            }
        }

        public DateTime GetCreatedDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(documentRevisionCreatedDate);
        }

        public DateTime GetReleasedDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(documentRevisionReleasedDate);
        }

        public DocumentRevision(Core core, long documentId, string revision)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(DocumentRevision_ItemLoad);

            try
            {
                LoadItem(new FieldValuePair("document_id", documentId), new FieldValuePair("document_revision", revision));
            }
            catch (InvalidItemException)
            {
                throw new InvalidDocumentRevisionException();
            }
        }

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

        public static DocumentRevision Create(Core core, Document document, string newRevision, DocumentStatus newStatus)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            Item item = Item.Create(core, typeof(DocumentRevision), new FieldValuePair("document_id", document.Id),
                new FieldValuePair("document_revision", newRevision),
                new FieldValuePair("document_revision_status", (byte)newStatus),
                new FieldValuePair("document_storage_path", ""),
                new FieldValuePair("revision_created_date", UnixTime.UnixTimeStamp()),
                new FieldValuePair("revision_released_date", 0));

            return (DocumentRevision)item;
        }

        public bool Release()
        {
            try
            {
                Status = DocumentStatus.Current;
                this.Update();
                return true;
            }
            catch (UnauthorisedToUpdateItemException)
            {
                return false;
            }
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
