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
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace BoxSocial.Install
{
    [XmlRoot("settings")]
    public sealed class InstallSettings
    {
        [XmlElement("database-type")]
        public string DatabaseType;
        [XmlElement("database-host")]
        public string DatabaseHost;
        [XmlElement("database-name")]
        public string DatabaseName;
        [XmlElement("database-root-user")]
        public string DatabaseRootUser;
        [XmlElement("database-root-password")]
        public string DatabaseRootPassword;
        [XmlElement("database-web-user")]
        public string DatabaseWebUser;
        [XmlElement("database-web-password")]
        public string DatabaseWebPassword;
        [XmlElement("domain")]
        public string Domain;
        [XmlElement("root-directory")]
        public string RootDirectory;
        [XmlArray("applications")]
        public string[] Applications;
    }
}
