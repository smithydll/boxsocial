using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BoxSocial.IO
{
    public class LocalStorage : Storage
    {
        string path;

        public LocalStorage(string path, Database db)
            : base (db)
        {
            this.path = path;
        }

        public override void CreateBin(string bin)
        {
            // do nothing, bins are automatically created and destroyed in local storage mode
        }

        private void EnsureStoragePathExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private string RetrieveStoragePath(string bin, string fileName)
        {
            string topLevelDirectory = fileName.Substring(0, 1).ToLower();
            string secondLevelDirectory = fileName.Substring(0, 2).ToLower();

            string path = Path.Combine(Path.Combine(Path.Combine(this.path, topLevelDirectory), secondLevelDirectory), bin);

            return path;
        }

        public override string SaveFile(string bin, Stream file)
        {
            string fileName = HashFile(file);
            string path = RetrieveStoragePath(bin, fileName);
            EnsureStoragePathExists(path);
            FileStream fs = File.OpenWrite(Path.Combine(path, fileName));

            byte[] buffer = new byte[8192];
            int len;
            while ((len = file.Read(buffer, 0, buffer.Length)) > 0)
            {
                fs.Write(buffer, 0, len);
            }

            return fileName;
        }

        public override string SaveFileWithReducedRedundancy(string bin, Stream file)
        {
            string fileName = HashFile(file);
            string path = RetrieveStoragePath(bin, fileName);
            EnsureStoragePathExists(path);
            FileStream fs = File.OpenWrite(Path.Combine(path, fileName));

            byte[] buffer = new byte[8192];
            int len;
            while ((len = file.Read(buffer, 0, buffer.Length)) > 0)
            {
                fs.Write(buffer, 0, len);
            }

            return fileName;
        }

        public override void DeleteFile(string bin, string fileName)
        {
            string path = RetrieveStoragePath(bin, fileName);

            if (File.Exists(Path.Combine(path, fileName)))
            {
                File.Delete(Path.Combine(path, fileName));
            }
        }

        public override void TouchFile(string bin, string fileName)
        {
            throw new NotImplementedException();
        }

        public override Stream RetrieveFile(string bin, string fileName)
        {
            string path = RetrieveStoragePath(bin, fileName);
            FileStream fs = File.OpenRead(Path.Combine(path, fileName));

            return fs;
        }

        public override string RetrieveFileUri(string bin, string fileName)
        {
            throw new NotImplementedException();
        }

        public override bool IsCloudStorage
        {
            get
            {
                return false;
            }
        }

        public override string RetrieveFilePath(string bin, string fileName)
        {
            return Path.Combine(RetrieveStoragePath(bin, fileName), fileName);
        }
    }
}
