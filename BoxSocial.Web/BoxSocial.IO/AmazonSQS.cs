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
using Newtonsoft.Json;

namespace BoxSocial.IO
{
    public class AmazonSQS : JobQueue
    {
        AmazonSQSConfig sqsConfig;
        Amazon.SQS.AmazonSQS client;
        AmazonSQSClient sqsClient;
        Dictionary<string, string> queueUrls;

        public AmazonSQS(string keyId, string secretKey)
        {
            sqsConfig = new AmazonSQSConfig();
            //sqsConfig.ServiceURL = "https://sqs.amazonaws.com";

            client = AWSClientFactory.CreateAmazonSQSClient(keyId, secretKey, sqsConfig);
            queueUrls = new Dictionary<string, string>();
        }

        private string SanitiseQueueName(string queue)
        {
            return queue.Replace('\\', '_').Replace('/', '_').Replace('.', '_');
        }

        public override void CreateQueue(string queue)
        {
            CreateQueueRequest request = new CreateQueueRequest();
            request.QueueName = SanitiseQueueName(queue);
            CreateQueueResponse response = client.CreateQueue(request);
        }

        private string GetQueueUrl(string queue)
        {
            System.Net.WebException exception = null;

            int attempts = 0;
            while (attempts < 3)
            {
                try
                {
                    if (!queueUrls.ContainsKey(queue))
                    {
                        GetQueueUrlRequest request = new GetQueueUrlRequest();
                        request.QueueName = SanitiseQueueName(queue);
                        GetQueueUrlResponse response = client.GetQueueUrl(request);

                        queueUrls.Add(queue, response.GetQueueUrlResult.QueueUrl);
                    }

                    return queueUrls[queue];
                }
                catch (System.Net.WebException ex)
                {
                    exception = ex;
                }

                attempts++;
                System.Threading.Thread.Sleep(15); // give the remote server some time to recover
            }

            throw exception;
        }

        public override void DeleteQueue(string queue)
        {
            DeleteQueueRequest request = new DeleteQueueRequest();
            request.QueueUrl = GetQueueUrl(SanitiseQueueName(queue));
            DeleteQueueResponse response = client.DeleteQueue(request);
        }

        public override bool QueueExists(string queue)
        {
            try
            {
                GetQueueUrlRequest request = new GetQueueUrlRequest();
                request.QueueName = SanitiseQueueName(queue);
                GetQueueUrlResponse response = client.GetQueueUrl(request);

                return true;
            }
            catch (AmazonSQSException)
            {
                return false;
            }
        }

        public override void PushJob(Job jobMessage)
        {
            PushJob(TimeSpan.FromDays(7), jobMessage);
        }

        public override void PushJob(TimeSpan ttl, Job jobMessage)
        {
            try
            {
                SendMessageRequest request = new SendMessageRequest();
                //request.DelaySeconds = (int)(ttl.Ticks / TimeSpan.TicksPerSecond);
                request.QueueUrl = GetQueueUrl(SanitiseQueueName(jobMessage.QueueName));
                request.MessageBody = JsonConvert.SerializeObject(jobMessage);
                SendMessageResponse response = client.SendMessage(request);
            }
            catch (System.Net.WebException)
            {
            }
        }

        public override List<Job> ClaimJobs(string queue, int count)
        {
            List<Job> claimedJobs = new List<Job>();

            try
            {
                ReceiveMessageRequest request = new ReceiveMessageRequest();
                request.MaxNumberOfMessages = count;
                request.QueueUrl = GetQueueUrl(SanitiseQueueName(queue));
                ReceiveMessageResponse response = client.ReceiveMessage(request);

                for (int i = 0; i < response.ReceiveMessageResult.Message.Count; i++)
                {
                    Message message = response.ReceiveMessageResult.Message[i];
                    claimedJobs.Add(new Job(queue, message.MessageId, message.ReceiptHandle, message.Body));
                }
            }
            catch (System.Net.WebException)
            {
            }

            return claimedJobs;
        }

        public override void DeleteJob(Job job)
        {
            try
            {
                DeleteMessageRequest request = new DeleteMessageRequest();
                request.QueueUrl = GetQueueUrl(SanitiseQueueName(job.QueueName));
                request.ReceiptHandle = job.Handle;
                DeleteMessageResponse response = client.DeleteMessage(request);
            }
            catch (System.Net.WebException)
            {
                // some jobs won't execute if complete even if in the queue
            }
        }

        public override void CloseConnection()
        {
            if (client != null)
            {
                client.Dispose();
                client = null;
            }
        }
    }
}
