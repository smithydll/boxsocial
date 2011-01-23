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
            throw new NotImplementedException();
        }

        public override string SaveFile(string bin, Stream file)
        {
            throw new NotImplementedException();
        }

        public override string SaveFileWithReducedRedundancy(string bin, Stream file)
        {
            throw new NotImplementedException();
        }

        public override void DeleteFile(string bin, string fileName)
        {
            throw new NotImplementedException();
        }

        public override void TouchFile(string bin, string fileName)
        {
            throw new NotImplementedException();
        }

        public override Stream RetrieveFile(string bin, string fileName)
        {
            throw new NotImplementedException();
        }

        public override string RetrieveFileUri(string bin, string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
