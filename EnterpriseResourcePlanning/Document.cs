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
    [DataTable("erp_documents")]
    [CreatePermission("CREATE_DOCUMENTS")]
    [EditPermission("EDIT_DOCUMENTS")]
    [DeletePermission(null)]
    public class Document : NumberedItem, IPermissibleSubItem
    {
        [DataField("document_id", DataFieldKeys.Primary)]
        private long documentId;
        [DataField("document_key", DataFieldKeys.Unique, "document_key")]
        private string documentKey;
        [DataField("document_item", DataFieldKeys.Unique, "document_key")]
        private ItemKey ownerKey;
        [DataField("document_title", 63)]
        private string documentTitle;
        [DataField("document_revision", 10)]
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
        private Project project;
        private DocumentRevision revision;
        private ErpSettings settings;

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

        public string RevisionString
        {
            get
            {
                return documentRevision;
            }
            set
            {
                SetProperty("documentRevision", value);
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
                SetProperty("documentTitle", value);
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
                if (owner == null || ownerKey.Id != owner.Id || ownerKey.TypeId != owner.TypeId)
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

        public Project Project
        {
            get
            {
                if (project == null || project.Id != projectId)
                {
                    project = (Project)core.ItemCache[new ItemKey(projectId, typeof(Project))];
                }
                return project;
            }
        }

        public DocumentRevision Revision
        {
            get
            {
                if (revision == null || revision.DocumentId != Id || revision.Revision != RevisionString)
                {
                    revision = new DocumentRevision(core, Id, RevisionString);
                }
                return revision;
            }
        }

        public ErpSettings Settings
        {
            get
            {
                if (settings == null || !settings.Owner.ItemKey.Equals(ownerKey))
                {
                    settings = new ErpSettings(core, Owner);
                }
                return settings;
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
            : this(core, documentDataRow, false)
        {
        }

        public Document(Core core, DataRow documentDataRow, bool hasRevisionDataJoined)
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

            if (hasRevisionDataJoined)
            {
                revision = new DocumentRevision(core, documentDataRow);
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
                return core.Hyperlink.AppendSid(string.Format("{0}document/{1}",
                        Owner.UriStub, DocumentKey));
            }
        }

        public BillOfMaterials GetBom()
        {
            BillOfMaterials bom = null;

            SelectQuery query = BillOfMaterials.GetSelectQueryStub(typeof(BillOfMaterials));
            query.AddCondition("document_id", Id);

            DataTable bomDataTable = db.Query(query);

            if (bomDataTable.Rows.Count == 1)
            {
                bom = new BillOfMaterials(core, bomDataTable.Rows[0]);
            }

            if (bom != null)
            {
                return bom;
            }

            throw new InvalidBillOfMaterialsException();
        }

        public List<DocumentRevision> GetRevisions()
        {
            List<DocumentRevision> revisions = new List<DocumentRevision>();

            SelectQuery query = DocumentRevision.GetSelectQueryStub(typeof(DocumentRevision));
            query.AddCondition("document_id", Id);

            DataTable revisionsDataTable = core.Db.Query(query);

            foreach (DataRow dr in revisionsDataTable.Rows)
            {
                revisions.Add(new DocumentRevision(core, dr));
            }

            return revisions;
        }

        public DocumentRevision Revise(string newRevision, bool autoIncrement, int incrementClass)
        {
            ErpSettings settings = new ErpSettings(core, Owner);

            if (settings.Access.Can("REVISE_DOCUMENTS"))
            {
                if (autoIncrement)
                {
                    switch (Template.RevisionType)
                    {
                        case RevisionTypes.Alphabetical:
                            if (RevisionString == "Z")
                            {
                                newRevision = "AB";
                            }
                            else
                            {
                                if (RevisionString.Length > 1)
                                {
                                    newRevision = RevisionString.Substring(0, RevisionString.Length - 1) + (RevisionString[RevisionString.Length - 1] + 1).ToString();
                                }
                                else
                                {
                                    newRevision = (RevisionString[0] + 1).ToString();
                                }
                            }
                            break;
                        case RevisionTypes.Numerical:
                            int revisionInt;
                            int.TryParse(RevisionString, out revisionInt);
                            revisionInt++;
                            newRevision = revisionInt.ToString();
                            break;
                    }
                }

                DocumentRevision newDocumentRevision = EnterpriseResourcePlanning.DocumentRevision.Create(core, this, newRevision, DocumentStatus.Unreleased);
                this.RevisionString = newDocumentRevision.Revision;
                this.Update();

                return newDocumentRevision;
            }
            else
            {
                throw new UnauthorisedToUpdateItemException();
            }
        }

        public static void ShowList(object sender, ShowPPageEventArgs e)
        {
            e.SetTemplate("viewdocuments");

            ErpSettings settings = new ErpSettings(e.Core, e.Page.Owner);

            if (!settings.Access.Can("VIEW_DOCUMENTS"))
            {
                e.Core.Functions.Generate403();
            }

        }

        public static void Show(object sender, ShowPPageEventArgs e)
        {
            e.SetTemplate("viewdocument");

            string[] parts = e.Slug.Split(new char[] { '/' });

            ErpSettings settings = new ErpSettings(e.Core, e.Page.Owner);

            if (!settings.Access.Can("VIEW_DOCUMENTS"))
            {
                e.Core.Functions.Generate403();
            }

            Document document = null;
            DocumentRevision revision = null;

            if (parts.Length >= 1)
            {
                try
                {
                    document = new Document(e.Core, e.Page.Owner, parts[0]);
                }
                catch (InvalidDocumentException)
                {
                    e.Core.Functions.Generate404();
                }
            }
            else
            {
                e.Core.Functions.Generate404();
            }

            if (parts.Length == 2)
            {
                try
                {
                    revision = new DocumentRevision(e.Core, document.Id, parts[1]);
                }
                catch (InvalidDocumentRevisionException)
                {
                    e.Core.Functions.Generate404();
                }
            }
            else
            {
                revision = document.Revision;
            }

            e.Template.Parse("DOCUMENT_KEY", document.DocumentKey);
            e.Template.Parse("DOCUMENT_TITLE", document.DocumentTitle);
            e.Template.Parse("DOCUMENT_CREATED", e.Core.Tz.DateTimeToString(document.GetCreatedDate(e.Core.Tz)));
            e.Template.Parse("DOCUMENT_RELEASED", e.Core.Tz.DateTimeToString(document.GetReleasedDate(e.Core.Tz)));
            e.Template.Parse("PROJECT_KEY", document.Project.ProjectKey);
            e.Template.Parse("U_PROJECT", document.Project.Uri);

            if (revision != null)
            {
                e.Template.Parse("DOCUMENT_REVISION", revision.Revision);
            }

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "*documents", "Documents" });
            breadCrumbParts.Add(new string[] { "document", document.DocumentTitle });

            if (revision != null)
            {
                breadCrumbParts.Add(new string[] { revision.Revision, revision.Revision });
            }

            e.Page.Owner.ParseBreadCrumbs(breadCrumbParts);
        }


        public IPermissibleItem PermissiveParent
        {
            get
            {
                return Settings;
            }
        }
    }

    public class InvalidDocumentException : Exception
    {
    }
}
