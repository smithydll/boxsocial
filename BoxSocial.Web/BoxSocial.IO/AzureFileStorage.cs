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
using System.IO;
using System.Text;

namespace BoxSocial.IO
{
    public class AzureFileStorage : Storage
    {

        public AzureFileStorage(string keyId, string secretKey, Database db)
            : base (db)
        {
            
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

        }

        public override string SaveFile(string bin, Stream file, string contentType)
        {
            return string.Empty;
        }

        public override string SaveFile(string bin, string fileName, Stream file, string contentType)
        {
            return string.Empty;
        }

        public override string SaveFileWithReducedRedundancy(string bin, Stream file, string contentType)
        {
            return string.Empty;
        }

        public override string SaveFileWithReducedRedundancy(string bin, string fileName, Stream file, string contentType)
        {
            return string.Empty;
        }

        public override void DeleteFile(string bin, string fileName)
        {
        }

        public override void TouchFile(string bin, string fileName)
        {
            throw new NotImplementedException();
        }

        public override Stream RetrieveFile(string bin, string fileName)
        {
            return null;
        }

        public override string RetrieveFileUri(string bin, string fileName)
        {
            return string.Empty;
        }

        public string RetrieveSecureFileUri(string bin, string fileName)
        {
            return string.Empty;
        }

        public override string RetrieveFileUri(string bin, string fileName, string contentType, string renderFileName)
        {
            return string.Empty;
        }

        public override void CopyFile(string fromBin, string toBin, string fileName)
        {
        }

        public override bool FileExists(string bin, string fileName)
        {
            return false;
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
