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
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;

namespace BoxSocial.IO
{
    public class AmazonS3 : Storage
    {
        AmazonS3Config s3Config;
        Amazon.S3.AmazonS3 client;
        AmazonS3Client s3Client;

        public AmazonS3(string keyId, string secretKey, Database db)
            : base (db)
        {
            s3Config = new AmazonS3Config();
            s3Config.ServiceURL = "s3.amazonaws.com";
            s3Config.CommunicationProtocol = Protocol.HTTPS;

            client = AWSClientFactory.CreateAmazonS3Client(keyId, secretKey, s3Config);
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
            PutBucketRequest pbr = new PutBucketRequest();
            pbr.BucketName = bin;
            PutBucketResponse response = client.PutBucket(pbr);
        }

        public override string SaveFile(string bin, Stream file, string contentType)
        {
            string fileName = HashFile(file);

            // Do not overwrite or double work files
            if (!FileExists(bin, fileName))
            {
                PutObjectRequest request = new PutObjectRequest();
                request.BucketName = bin;
                //request.GenerateMD5Digest = true;
                request.InputStream = file;
                request.Key = fileName;
                request.StorageClass = S3StorageClass.Standard;
                request.ContentType = contentType;
                PutObjectResponse response = client.PutObject(request);
            }
            return fileName;
        }

        public override string SaveFile(string bin, string fileName, Stream file, string contentType)
        {
            // Do not overwrite or double work files
            if (!FileExists(bin, fileName))
            {
                PutObjectRequest request = new PutObjectRequest();
                request.BucketName = bin;
                //request.GenerateMD5Digest = true;
                request.InputStream = file;
                request.Key = fileName;
                request.StorageClass = S3StorageClass.Standard;
                request.ContentType = contentType;
                PutObjectResponse response = client.PutObject(request);
            }

            return fileName;
        }

        public override string SaveFileWithReducedRedundancy(string bin, Stream file, string contentType)
        {
            string fileName = HashFile(file);
            PutObjectRequest request = new PutObjectRequest();
            request.BucketName = bin;
            //request.GenerateMD5Digest = true;
            request.InputStream = file;
            request.Key = fileName;
            request.StorageClass = S3StorageClass.ReducedRedundancy;
            request.ContentType = contentType;
            PutObjectResponse response = client.PutObject(request);
            return fileName;
        }

        public override string SaveFileWithReducedRedundancy(string bin, string fileName, Stream file, string contentType)
        {
            PutObjectRequest request = new PutObjectRequest();
            request.BucketName = bin;
            //request.GenerateMD5Digest = true;
            request.InputStream = file;
            request.Key = fileName;
            request.StorageClass = S3StorageClass.ReducedRedundancy;
            request.ContentType = contentType;
            PutObjectResponse response = client.PutObject(request);
            return fileName;
        }

        public override void DeleteFile(string bin, string fileName)
        {
            DeleteObjectRequest request = new DeleteObjectRequest();
            request.BucketName = bin;
            request.Key = fileName;
            DeleteObjectResponse response = client.DeleteObject(request);
        }

        public override void TouchFile(string bin, string fileName)
        {
            throw new NotImplementedException();
        }

        public override Stream RetrieveFile(string bin, string fileName)
        {
            try
            {
                GetObjectRequest request = new GetObjectRequest();
                request.BucketName = bin;
                request.Key = fileName;
                GetObjectResponse response = client.GetObject(request);

                MemoryStream ms = new MemoryStream();
                response.ResponseStream.CopyTo(ms);
                return ms;
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                throw new Exception("Bin: " + bin + "<br />" + "File: " + fileName + "<br />" + ex.ToString());
            }
        }

        public override string RetrieveFileUri(string bin, string fileName)
        {
            GetPreSignedUrlRequest request = new GetPreSignedUrlRequest();
            request.BucketName = bin;
            request.Key = fileName;
            request.ResponseHeaderOverrides.Expires = DateTime.Now.AddDays(1).ToUniversalTime().ToString();
            request.ResponseHeaderOverrides.CacheControl = "private, max-age=864000;";
            request.Expires = DateTime.Now.AddHours(1);
            return client.GetPreSignedURL(request);
        }

        public string RetrieveSecureFileUri(string bin, string fileName)
        {
            // Secure not supported by amazon
            GetPreSignedUrlRequest request = new GetPreSignedUrlRequest();
            request.BucketName = bin;
            request.Key = fileName;
            request.ResponseHeaderOverrides.Expires = DateTime.Now.AddDays(1).ToUniversalTime().ToString();
            request.ResponseHeaderOverrides.CacheControl = "private, max-age=864000;";
            request.Expires = DateTime.Now.AddHours(1);
            return client.GetPreSignedURL(request);
        }

        public override string RetrieveFileUri(string bin, string fileName, string contentType, string renderFileName)
        {
            GetPreSignedUrlRequest request = new GetPreSignedUrlRequest();
            request.BucketName = bin;
            request.Key = fileName;
            request.ResponseHeaderOverrides.ContentType = contentType;
            request.ResponseHeaderOverrides.ContentDisposition = "inline; filename=" + renderFileName;
            request.ResponseHeaderOverrides.Expires = DateTime.Now.AddDays(1).ToUniversalTime().ToString();
            request.ResponseHeaderOverrides.CacheControl = "private, max-age=864000;";
            request.Expires = DateTime.Now.AddHours(1);
            return client.GetPreSignedURL(request);
        }

        public override void CopyFile(string fromBin, string toBin, string fileName)
        {
            CopyObjectRequest request = new CopyObjectRequest();
            request.SourceBucket = fromBin;
            request.DestinationBucket = toBin;
            request.SourceKey = fileName;
            request.DestinationKey = fileName;
            CopyObjectResponse response = client.CopyObject(request);
        }

        public override bool FileExists(string bin, string fileName)
        {
            GetObjectMetadataRequest request = new GetObjectMetadataRequest();
            request.BucketName = bin;
            request.Key = fileName;

            try
            {
                GetObjectMetadataResponse response = client.GetObjectMetadata(request);
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }
            catch (System.Xml.XmlException)
            {
                return false;
            }

            return true;
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
