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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SQS.Util;

namespace BoxSocial.IO
{
    public class AmazonSQS : JobQueue
    {
        AmazonSQSConfig sqsConfig;
        Amazon.SQS.AmazonSQS client;
        AmazonSQSClient sqsClient;

        public AmazonSQS(string keyId, string secretKey, Database db)
            : base (db)
        {
            sqsConfig = new AmazonSQSConfig();
            sqsConfig.ServiceURL = "sqs.amazonaws.com";

            client = AWSClientFactory.CreateAmazonSQSClient(keyId, secretKey, sqsConfig);
        }

        public override void CreateQueue(string queue)
        {
            CreateQueueRequest request = new CreateQueueRequest();
            request.QueueName = queue;
            CreateQueueResponse response = client.CreateQueue(request);
        }

        private string GetQueueUrl(string queue)
        {
            GetQueueUrlRequest request = new GetQueueUrlRequest();
            request.QueueName = queue;
            GetQueueUrlResponse response = client.GetQueueUrl(request);

            return response.GetQueueUrlResult.QueueUrl;
        }

        public override void DeleteQueue(string queue)
        {
            DeleteQueueRequest request = new DeleteQueueRequest();
            request.QueueUrl = GetQueueUrl(queue);
            DeleteQueueResponse response = client.DeleteQueue(request);
        }
    }
}
