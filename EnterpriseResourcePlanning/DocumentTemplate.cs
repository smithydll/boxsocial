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
    public enum RevisionTypes : byte
    {
        Alphabetical = 0x01,
        Numerical = 0x02,
        TwoClassVersion = 0x03,
        ThreeClassVersion = 0x04,
        FourClassVersion = 0x05,
    }

    [DataTable("erp_document_template")]
    public class DocumentTemplate : NumberedItem, IPermissibleSubItem
    {
        [DataField("template_id", DataFieldKeys.Primary)]
        private long templateId;
        [DataField("document_template_title", 31)]
        private string title;
        [DataField("document_template_description", MYSQL_TEXT)]
        private string description;
        [DataField("document_template_item", DataFieldKeys.Unique, "document_key")]
        private ItemKey ownerKey;
        [DataField("document_key_prefix", 8)]
        private string documentKeyPrefix;
        [DataField("document_last_key_id")]
        private long lastDocumentKeyId;
        [DataField("document_key_padded")]
        private bool keyPadded;
        [DataField("document_key_padding_length")]
        private int keyPaddingLength;
        [DataField("document_count")]
        private long documentCount;
        [DataField("revision_type")]
        private byte revisionType;

        private Primitive owner;
        private ErpSettings settings;

        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                SetProperty("title", value);
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                SetProperty("description", value);
            }
        }

        public string KeyPrefix
        {
            get
            {
                return documentKeyPrefix;
            }
        }

        public long LastKeyId
        {
            get
            {
                return LastKeyId;
            }
        }

        public bool IsKeyPadded
        {
            get
            {
                return keyPadded;
            }
        }

        public int KeyPaddingLength
        {
            get
            {
                return keyPaddingLength;
            }
        }

        public long DocumentCount
        {
            get
            {
                return documentCount;
            }
        }

        public RevisionTypes RevisionType
        {
            get
            {
                return (RevisionTypes)revisionType;
            }
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

        public DocumentTemplate(Core core, long templateId)
            : base (core)
        {
            ItemLoad += new ItemLoadHandler(DocumentTemplate_ItemLoad);

            try
            {
                LoadItem(templateId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidTemplateException();
            }
        }

        void DocumentTemplate_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return templateId;
            }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }

        public string EditUri
        {
            get
            {
                return core.Hyperlink.AppendSid(Owner.AccountUriStub + "templates/edit?id=" + Id, true);
            }
        }

        public string DeleteUri
        {
            get
            {
                return core.Hyperlink.AppendSid(Owner.AccountUriStub + "templates/delete?id=" + Id, true);
            }
        }


        public IPermissibleItem PermissiveParent
        {
            get
            {
                return Settings;
            }
        }
    }

    public class InvalidTemplateException : Exception
    {
    }
}
