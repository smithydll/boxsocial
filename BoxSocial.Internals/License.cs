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
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("licenses")]
    public class ContentLicense : NumberedItem
    {
        [DataField("license_id", DataFieldKeys.Primary)]
        private byte licenseId;
        [DataField("license_title", 63)]
        private string title;
        [DataField("license_icon", 63)]
        private string icon;
        [DataField("license_link", 255)]
        private string link;
        [DataField("license_text", MYSQL_TEXT)]
        private string text;

        public byte LicenseId
        {
            get
            {
                return licenseId;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
        }

        public string Icon
        {
            get
            {
                return icon;
            }
        }

        public string Link
        {
            get
            {
                return link;
            }
        }

        public string Text
        {
            get
            {
                return text;
            }
        }

        public ContentLicense(Core core, byte licenseId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ContentLicense_ItemLoad);

            try
            {
                LoadItem(licenseId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidLicenseException();
            }
        }

        public ContentLicense(Core core, DataRow licenseRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ContentLicense_ItemLoad);

            try
            {
                loadItemInfo(licenseRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidLicenseException();
            }
        }

        protected override void loadItemInfo(DataRow licenseRow)
        {
            try
            {
                loadValue(licenseRow, "license_id", out licenseId);
                loadValue(licenseRow, "license_title", out title);
                loadValue(licenseRow, "license_icon", out icon);
                loadValue(licenseRow, "license_link", out link);
                loadValue(licenseRow, "license_text", out text);

                itemLoaded(licenseRow);
                core.ItemCache.RegisterItem((NumberedItem)this);
            }
            catch
            {
                throw new InvalidItemException();
            }
        }

        private void ContentLicense_ItemLoad()
        {
        }

        public static string BuildLicenseSelectBox(Mysql db, byte selectedLicense)
        {
            List<string> permissions = new List<string>();
            permissions.Add("Can Read");

            Dictionary<string, string> licenses = new Dictionary<string, string>(StringComparer.Ordinal);
            DataTable licensesTable = db.Query("SELECT li.license_id, li.license_title FROM licenses li");

            licenses.Add("0", "Default License (All Rights Reserved)");
            foreach (DataRow licenseRow in licensesTable.Rows)
            {
                licenses.Add(((byte)licenseRow["license_id"]).ToString(), (string)licenseRow["license_title"]);
            }

            return Functions.BuildSelectBox("license", licenses, selectedLicense.ToString());
        }

        public override long Id
        {
            get
            {
                return licenseId;
            }
        }

        public override string Uri
        {
            get
            {
                return link;
            }
        }
    }

    public class InvalidLicenseException : Exception
    {
    }
}
