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

    public enum DocumentCustomFieldTypes : byte
    {
        FixedPoint = 0x01,
        FloatingPoint = 0x02,
        ShortText = 0x03,
        LongText = 0x04,
    }

    [DataTable("erp_custom_fields")]
    public class DocumentCustomField : NumberedItem
    {
        [DataField("custom_field_id", DataFieldKeys.Primary)]
        private long customFieldId;
        [DataField("template_id", typeof(DocumentTemplate))]
        private long templateId;
        [DataField("custom_field_name", 15)]
        private string fieldName;
        [DataField("custom_field_title", 15)]
        private string fieldTitle;
        [DataField("custom_field_type")]
        private byte fieldType;

        public string FieldName
        {
            get
            {
                return fieldName;
            }
        }

        public string FieldTitle
        {
            get
            {
                return fieldTitle;
            }
            set
            {
                SetProperty("fieldTitle", value);
            }
        }

        public DocumentCustomFieldTypes FieldType
        {
            get
            {
                return (DocumentCustomFieldTypes)fieldType;
            }
        }

        public DocumentCustomField(Core core, DataRow customFieldRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(DocumentCustomField_ItemLoad);

            try
            {
                loadItemInfo(customFieldRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidDocumentCustomFieldException();
            }
        }

        void DocumentCustomField_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return customFieldId;
            }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class InvalidDocumentCustomFieldException : Exception
    {
    }
}
