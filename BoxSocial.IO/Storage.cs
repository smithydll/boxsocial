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
using System.Security.Cryptography;
using System.Data;
using System.IO;
using System.Text;
using System.Web;

namespace BoxSocial.IO
{
    public abstract class Storage
    {
        private Database db;

        public Storage(Database db)
        {
            this.db = db;
        }

        public abstract bool IsCloudStorage
        {
            get;
        }

        public abstract void CreateBin(string bin);

        public abstract string SaveFile(string bin, Stream file);

        public abstract string SaveFile(string bin, string fileName, Stream file);

        public abstract string SaveFileWithReducedRedundancy(string bin, Stream file);

        public abstract string SaveFileWithReducedRedundancy(string bin, string fileName, Stream file);

        public abstract void DeleteFile(string bin, string fileName);

        public abstract void TouchFile(string bin, string fileName);

        public abstract Stream RetrieveFile(string bin, string fileName);

        public abstract string RetrieveFileUri(string bin, string fileName);

        public abstract string RetrieveFileUri(string bin, string fileName, string contentType, string renderFileName);

        public abstract string RetrieveFilePath(string bin, string fileName);

        public abstract void CopyFile(string fromBin, string toBin, string fileName);

        public abstract bool FileExists(string bin, string fileName);

        public abstract string PathSeparator
        {
            get;
        }

        public abstract string PathCombine(string path1, string path2);

        public static string HashFile(Stream fileStream)
        {
            HashAlgorithm hash = new SHA512Managed();

            byte[] fileBytes = new byte[fileStream.Length];
            fileStream.Read(fileBytes, 0, (int)fileStream.Length);

            byte[] fileHash = hash.ComputeHash(fileBytes);

            string fileHashString = "";
            foreach (byte fileHashByte in fileHash)
            {
                fileHashString += string.Format("{0:x2}", fileHashByte);
            }

            return fileHashString;
        }
    }
}
