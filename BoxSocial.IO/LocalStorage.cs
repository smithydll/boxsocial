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

            fs.Close();

            return fileName;
        }

        public override string SaveFile(string bin, string fileName, Stream file)
        {
            string path = RetrieveStoragePath(bin, fileName);
            EnsureStoragePathExists(path);
            FileStream fs = File.OpenWrite(Path.Combine(path, fileName));

            byte[] buffer = new byte[8192];
            int len;
            while ((len = file.Read(buffer, 0, buffer.Length)) > 0)
            {
                fs.Write(buffer, 0, len);
            }

            fs.Close();

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

            fs.Close();

            return fileName;
        }

        public override string SaveFileWithReducedRedundancy(string bin, string fileName, Stream file)
        {
            string path = RetrieveStoragePath(bin, fileName);
            EnsureStoragePathExists(path);
            FileStream fs = File.OpenWrite(Path.Combine(path, fileName));

            byte[] buffer = new byte[8192];
            int len;
            while ((len = file.Read(buffer, 0, buffer.Length)) > 0)
            {
                fs.Write(buffer, 0, len);
            }

            fs.Close();

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

        public override void CopyFile(string fromBin, string toBin, string fileName)
        {
            string fromPath = RetrieveStoragePath(fromBin, fileName);
            string toPath = RetrieveStoragePath(fromBin, fileName);
            EnsureStoragePathExists(fromPath);
            EnsureStoragePathExists(toPath);
            File.Copy(fromPath, toPath);
        }

        public override bool FileExists(string bin, string fileName)
        {
            string path = RetrieveStoragePath(bin, fileName);

            return File.Exists(Path.Combine(path, fileName));
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
