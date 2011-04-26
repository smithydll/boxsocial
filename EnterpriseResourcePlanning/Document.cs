﻿/*
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
    [DataTable("erp_documents")]
    public class Document : NumberedItem
    {
        [DataField("document_id", DataFieldKeys.Primary)]
        private long documentId;
        [DataField("document_key", DataFieldKeys.Unique, "document_key")]
        private string documentKey;
        [DataField("document_item", DataFieldKeys.Unique, "document_key")]
        private ItemKey ownerKey;
        [DataField("document_title", 63)]
        private string documentTitle;
        [DataField("document_revision", 3)]
        private string documentRevision;
        [DataField("document_superseded_by")]
        private long supersededById;
        [DataField("project_id", typeof(Project))]
        private long projectId;
        [DataField("template_id", typeof(DocumentTemplate))]
        private long documentTemplateId;
        [DataField("document_created_date")]
        private long documentCreatedDate;
        [DataField("document_released_date")]
        private long documentReleasedDate;

        private Primitive owner;
        private DocumentTemplate template;

        private List<DocumentCustomFieldFixedPointValue> customFieldsFixedPoint = new List<DocumentCustomFieldFixedPointValue>();
        private List<DocumentCustomFieldFloatingPointValue> customFieldsFloatingPoint = new List<DocumentCustomFieldFloatingPointValue>();
        private List<DocumentCustomFieldLongTextValue> customFieldsLongText = new List<DocumentCustomFieldLongTextValue>();
        private List<DocumentCustomFieldShortTextValue> customFieldsShortText = new List<DocumentCustomFieldShortTextValue>();

        public string DocumentKey
        {
            get
            {
                return documentKey;
            }
        }

        public string DocumentTitle
        {
            get
            {
                return documentTitle;
            }
            set
            {
                SetProperty(ref ((object)documentTitle), value);
            }
        }

        public DateTime GetCreatedDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(documentCreatedDate);
        }

        public DateTime GetReleasedDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(documentReleasedDate);
        }

        public Primitive Owner
        {
            get
            {
                if (owner == null || ownerKey.Id != owner.Id || ownerKey.TypeString != owner.Type)
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(ownerKey);
                    owner = core.PrimitiveCache[ownerKey];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

        public DocumentTemplate Template
        {
            get
            {
                if (template == null || template.Id != documentTemplateId)
                {
                    //template = new DocumentTemplate(core, documentTemplateId);
                    template = (DocumentTemplate)core.ItemCache[new ItemKey(documentTemplateId, typeof(DocumentTemplate))];
                }
                return template;
            }
        }

        public Document(Core core, Primitive owner, string key)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Document_ItemLoad);

            try
            {
                LoadItem("document_item_id", "document_item_type_id", owner, new FieldValuePair("document_key", key));
            }
            catch (InvalidItemException)
            {
                throw new InvalidDocumentException();
            }
        }

        public Document(Core core, long documentId)
            : base (core)
        {
            ItemLoad += new ItemLoadHandler(Document_ItemLoad);

            try
            {
                LoadItem(documentId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidDocumentException();
            }
        }

        public Document(Core core, DataRow documentDataRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Document_ItemLoad);

            try
            {
                loadItemInfo(documentDataRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidDocumentException();
            }
        }

        void Document_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return documentId;
            }
        }

        public override string Uri
        {
            get
            {
                return core.Uri.AppendSid(string.Format("{0}document/{1}",
                        Owner.UriStub, DocumentKey));
            }
        }

        public void Show(object sender, ShowPPageEventArgs e)
        {
        }
    }

    public class InvalidDocumentException : Exception
    {
    }
}
