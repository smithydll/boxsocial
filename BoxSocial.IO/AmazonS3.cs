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

        public override void CreateBin(string bin)
        {
            PutBucketRequest pbr = new PutBucketRequest();
            pbr.BucketName = bin;
            PutBucketResponse response = s3Client.PutBucket(pbr);
        }

        public override string SaveFile(string bin, Stream file)
        {
            string fileName = HashFile(file);
            PutObjectRequest request = new PutObjectRequest();
            request.BucketName = bin;
            request.GenerateMD5Digest = true;
            request.InputStream = file;
            request.Key = fileName;
            request.StorageClass = S3StorageClass.Standard;
            PutObjectResponse response = s3Client.PutObject(request);
            return fileName;
        }

        public override string SaveFileWithReducedRedundancy(string bin, Stream file)
        {
            string fileName = HashFile(file);
            PutObjectRequest request = new PutObjectRequest();
            request.BucketName = bin;
            request.GenerateMD5Digest = true;
            request.InputStream = file;
            request.Key = fileName;
            request.StorageClass = S3StorageClass.ReducedRedundancy;
            PutObjectResponse response = s3Client.PutObject(request);
            return fileName;
        }

        public override void DeleteFile(string bin, string fileName)
        {
            DeleteObjectRequest request = new DeleteObjectRequest();
            request.BucketName = bin;
            request.Key = fileName;
            DeleteObjectResponse response = s3Client.DeleteObject(request);
        }

        public override void TouchFile(string bin, string fileName)
        {
            throw new NotImplementedException();
        }

        public override Stream RetrieveFile(string bin, string fileName)
        {
            GetObjectRequest request = new GetObjectRequest();
            request.BucketName = bin;
            request.Key = fileName;
            GetObjectResponse response = s3Client.GetObject(request);

            return response.ResponseStream;
        }

        public override string RetrieveFileUri(string bin, string fileName)
        {
            GetPreSignedUrlRequest request = new GetPreSignedUrlRequest();
            request.BucketName = bin;
            request.Key = fileName;
            return s3Client.GetPreSignedURL(request);
        }
    }
}
