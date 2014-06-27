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
using net.openstack.Core;
using net.openstack.Core.Domain;
using net.openstack.Core.Domain.Queues;
using net.openstack.Core.Exceptions;
using net.openstack.Providers.Rackspace;

namespace BoxSocial.IO
{
    public class RackspaceCloudQueues : JobQueue
    {
        CloudQueuesProvider provider;
        CloudIdentity identity;
        string location = null;

        public RackspaceCloudQueues(string keyId, string username)
        {
            identity = new CloudIdentity() { APIKey = keyId, Username = username };
            provider = new CloudQueuesProvider(identity, location, Guid.NewGuid(), false, null);
        }

        private string SanitiseQueueName(string queue)
        {
            return queue.Replace('\\', '.').Replace('/', '.');
        }

        public void SetLocation(string location)
        {
            this.location = location;
        }

        public override void CreateQueue(string queue)
        {
            Task<bool> createQueueTasks = provider.CreateQueueAsync(new QueueName(SanitiseQueueName(queue)), CancellationToken.None);
        }

        public override void DeleteQueue(string queue)
        {
            Task deleteQueueTask = provider.DeleteQueueAsync(new QueueName(SanitiseQueueName(queue)), CancellationToken.None);
        }

        public override bool QueueExists(string queue)
        {
            return provider.QueueExistsAsync(new QueueName(SanitiseQueueName(queue)), CancellationToken.None).Result;
        }

        public override void PushJob(string queue, TimeSpan ttl, string jobMessage)
        {
            provider.PostMessagesAsync(new QueueName(SanitiseQueueName(queue)), CancellationToken.None, new Message(ttl, new Newtonsoft.Json.Linq.JObject(jobMessage)));
        }

        public override List<Job> ClaimJobs(string queue, int count)
        {
            List<Job> claimedJobs = new List<Job>();

            Task<Claim> claims = provider.ClaimMessageAsync(new QueueName(SanitiseQueueName(queue)), count, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), CancellationToken.None);
            
            for (int i= 0; i < claims.Result.Messages.Count; i++)
            {
                claimedJobs.Add(new Job(queue, claims.Result.Messages[i].Id.ToString(), null, claims.Result.Messages[i].Body.ToString()));
            }

            return claimedJobs;
        }

        public override void DeleteJob(Job job)
        {
            provider.DeleteMessageAsync(new QueueName(SanitiseQueueName(job.QueueName)), new MessageId(job.JobId), null, CancellationToken.None);
        }
    }
}
