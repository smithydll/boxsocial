﻿/*
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
using System.IO;
using System.Security.Cryptography;
using System.Text;
using net.openstack.Core;
using net.openstack.Core.Domain;
using net.openstack.Core.Exceptions;
using net.openstack.Providers.Rackspace;

namespace BoxSocial.IO
{
    public class Rackspace : Storage
    {
        CloudFilesProvider provider;
        CloudIdentity identity;

        public Rackspace(string keyId, string username, Database db)
            : base (db)
        {
            identity = new CloudIdentity() { APIKey = keyId, Username = username };
            provider = new CloudFilesProvider(identity);
        }

        private string SanitiseBinName(string bin)
        {
            return bin.Replace('\\', '.').Replace('/', '.');
        }

        public override string PathSeparator
        {
            get
            {
                return "";
            }
        }

        public override string PathCombine(string path1, string path2)
        {
            return path1 + path2;
        }

        public override void CreateBin(string bin)
        {
            ObjectStore createContainerResponse = provider.CreateContainer(bin);
        }

        public override string SaveFile(string bin, MemoryStream file)
        {
            string fileName = HashFile(file);

            // Do not overwrite or double work files
            if (!FileExists(bin, fileName))
            {
                file.Position = 0;
                provider.CreateObject(bin, file, fileName);
            }
            return fileName;
        }

        public override string SaveFile(string bin, string fileName, MemoryStream file)
        {
            // Do not overwrite or double work files
            if (!FileExists(bin, fileName))
            {
                file.Position = 0;
                provider.CreateObject(bin, file, fileName);
            }

            return fileName;
        }

        public override string SaveFileWithReducedRedundancy(string bin, MemoryStream file)
        {
            string fileName = HashFile(file);

            file.Position = 0;
            provider.CreateObject(bin, file, fileName);

            return fileName;
        }

        public override string SaveFileWithReducedRedundancy(string bin, string fileName, MemoryStream file)
        {
            file.Position = 0;
            provider.CreateObject(bin, file, fileName);

            return fileName;
        }

        public override void DeleteFile(string bin, string fileName)
        {
            ObjectStore deleteObjectResponse = provider.DeleteObject(bin, fileName);
        }

        public override void TouchFile(string bin, string fileName)
        {
            throw new NotImplementedException();
        }

        public override Stream RetrieveFile(string bin, string fileName)
        {
            try
            {
                MemoryStream ms = new MemoryStream();
                provider.GetObject(bin, fileName, ms);
                return ms;
            }
            catch (net.openstack.Core.Exceptions.Response.ItemNotFoundException ex)
            {
                throw new Exception("Bin: " + bin + "<br />" + "File: " + fileName + "<br />" + ex.ToString());
            }
        }

        public override string RetrieveFileUri(string bin, string fileName)
        {
            ContainerCDN cdn = provider.GetContainerCDNHeader(bin);
            
            return cdn.CDNUri + "/" + fileName;

            // TODO: use tempurl instead when enabled in the API
        }

        public string RetrieveSecureFileUri(string bin, string fileName)
        {
            ContainerCDN cdn = provider.GetContainerCDNHeader(bin);

            return cdn.CDNSslUri + "/" + fileName;

            // TODO: use tempurl instead when enabled in the API
        }

        public override string RetrieveFileUri(string bin, string fileName, string contentType, string renderFileName)
        {
            return RetrieveFileUri(bin, fileName);
        }

        public string RetrieveSecureFileUri(string bin, string fileName, string contentType, string renderFileName)
        {
            return RetrieveSecureFileUri(bin, fileName);
        }

        public override void CopyFile(string fromBin, string toBin, string fileName)
        {
            ObjectStore copyObjectResponse = provider.CopyObject(fromBin, fileName, toBin, fileName);
        }

        public override bool FileExists(string bin, string fileName)
        {
            try
            {
                provider.GetObjectMetaData(bin, fileName);
                return true;
            }
            catch (net.openstack.Core.Exceptions.Response.ItemNotFoundException)
            {
                return false;
            }
        }

        public override bool IsCloudStorage
        {
            get
            {
                return true;
            }
        }

        public override string RetrieveFilePath(string bin, string fileName)
        {
            throw new NotImplementedException();
        }
    }
}