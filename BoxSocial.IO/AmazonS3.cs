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
using System.Collections.Generic;
using System.IO;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;

namespace BoxSocial.IO
{
    public class AmazonS3 : Storage
    {
        AmazonS3Client s3Client;

        public AmazonS3(string keyId, string secretKey, Database db)
            : base (db)
        {
            s3Client = new AmazonS3Client(keyId, secretKey);
        }

        public override void CreateBin()
        {
            throw new NotImplementedException();
        }

        public override void SaveFile(Stream file)
        {
            ListBucketsResponse response = s3Client.ListBuckets();
            foreach (S3Bucket bucket in response.Buckets)
            {
                if (bucket.BucketName == "")
                {
                    PutObjectRequest request = new PutObjectRequest();
                    request.GenerateMD5Digest = true;
                    request.InputStream = file;
                    request.StorageClass = S3StorageClass.Standard;
                    s3Client.PutObject(request);
                }
            }
        }

        public override void SaveFileWithReducedRedundancy(Stream file)
        {
        }

        public override void DeleteFile()
        {
            throw new NotImplementedException();
        }

        public override void TouchFile()
        {
            throw new NotImplementedException();
        }
    }
}
