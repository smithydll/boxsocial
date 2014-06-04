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
        Dictionary<string, string> queueUrls;

        public AmazonSQS(string keyId, string secretKey, Database db)
            : base (db)
        {
            sqsConfig = new AmazonSQSConfig();
            sqsConfig.ServiceURL = "sqs.amazonaws.com";

            client = AWSClientFactory.CreateAmazonSQSClient(keyId, secretKey, sqsConfig);
            queueUrls = new Dictionary<string, string>();
        }

        public override void CreateQueue(string queue)
        {
            CreateQueueRequest request = new CreateQueueRequest();
            request.QueueName = queue;
            CreateQueueResponse response = client.CreateQueue(request);
        }

        private string GetQueueUrl(string queue)
        {
            if (!queueUrls.ContainsKey(queue))
            {
                GetQueueUrlRequest request = new GetQueueUrlRequest();
                request.QueueName = queue;
                GetQueueUrlResponse response = client.GetQueueUrl(request);

                queueUrls.Add(queue, response.GetQueueUrlResult.QueueUrl);
            }

            return queueUrls[queue];
        }

        public override void DeleteQueue(string queue)
        {
            DeleteQueueRequest request = new DeleteQueueRequest();
            request.QueueUrl = GetQueueUrl(queue);
            DeleteQueueResponse response = client.DeleteQueue(request);
        }

        public /*override*/ void PushJob(string queue, TimeSpan ttl, string jobMessage)
        {
            SendMessageRequest request = new SendMessageRequest();
            //request.DelaySeconds = (int)(ttl.Ticks / TimeSpan.TicksPerSecond);
            request.QueueUrl = GetQueueUrl(queue);
            request.MessageBody = jobMessage;
            SendMessageResponse response = client.SendMessage(request);
        }

        public /*override*/ Job ClaimJob(string queue)
        {
            ReceiveMessageRequest request = new ReceiveMessageRequest();
            request.MaxNumberOfMessages = 1;
            request.QueueUrl = GetQueueUrl(queue);
            ReceiveMessageResponse response = client.ReceiveMessage(request);
            Message message = response.ReceiveMessageResult.Message[0];
            return new Job(queue, message.MessageId, message.ReceiptHandle, message.Body);
        }

        public /*override*/ void DeleteJob(Job job)
        {
            DeleteMessageRequest request = new DeleteMessageRequest();
            request.QueueUrl = GetQueueUrl(job.QueueName);
            request.ReceiptHandle = job.Handle;
            DeleteMessageResponse response = client.DeleteMessage(request);
        }
    }
}
