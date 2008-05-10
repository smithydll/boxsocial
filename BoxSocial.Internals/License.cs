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
    public class ContentLicense
    {
        public const string LICENSE_FIELDS = "li.license_id, li.license_title, li.license_icon, li.license_link";

        private Mysql db;

        private byte licenseId;
        private string title;
        private string icon;
        private string link;

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

        public ContentLicense(Mysql db, byte licenseId)
        {
            this.db = db;

            DataTable licenseTable = db.Query(string.Format("SELECT {0} FROM licenses li WHERE license_id = {1}",
                ContentLicense.LICENSE_FIELDS, licenseId));

            if (licenseTable.Rows.Count == 1)
            {
                loadLicenseInfo(licenseTable.Rows[0]);
            }
            else
            {
                throw new InvalidLicenseException();
            }
        }

        public ContentLicense(Mysql db, DataRow licenseRow)
        {
            this.db = db;

            loadLicenseInfo(licenseRow);
        }

        private void loadLicenseInfo(DataRow licenseRow)
        {
            if (!(licenseRow["license_id"] is DBNull))
            {
                licenseId = (byte)licenseRow["license_id"];
            }
            else
            {
                throw new NonexistantLicenseException();
            }
            if (!(licenseRow["license_title"] is DBNull))
            {
                title = (string)licenseRow["license_title"];
            }
            if (!(licenseRow["license_icon"] is DBNull))
            {
                icon = (string)licenseRow["license_icon"];
            }
            if (!(licenseRow["license_link"] is DBNull))
            {
                link = (string)licenseRow["license_link"];
            }
        }

        public static string BuildLicenseSelectBox(Mysql db, byte selectedLicense)
        {
            List<string> permissions = new List<string>();
            permissions.Add("Can Read");

            Dictionary<string, string> licenses = new Dictionary<string, string>();
            DataTable licensesTable = db.Query("SELECT li.license_id, li.license_title FROM licenses li");

            licenses.Add("0", "Default License (All Rights Reserved)");
            foreach (DataRow licenseRow in licensesTable.Rows)
            {
                licenses.Add(((byte)licenseRow["license_id"]).ToString(), (string)licenseRow["license_title"]);
            }

            return Functions.BuildSelectBox("license", licenses, selectedLicense.ToString());
        }
    }

    public class InvalidLicenseException : Exception
    {
    }

    public class NonexistantLicenseException : Exception
    {
    }
}
