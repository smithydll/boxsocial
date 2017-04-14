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
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class DatabaseQueueProvider : JobQueue
    {
        private Core core;

        public DatabaseQueueProvider(Core core)
        {
            this.core = core;
        }

        public override void CreateQueue(string queue)
        {
            Queue.Create(core, queue);
        }

        public override void DeleteQueue(string queue)
        {
            Queue q = new Queue(core, queue);
            q.Delete();
        }

        public override bool QueueExists(string queue)
        {
            return Queue.Exists(core, queue);
        }

        public override void PushJob(Job jobMessage)
        {
            PushJob(TimeSpan.FromDays(7), jobMessage);
        }

        public override void PushJob(TimeSpan ttl, Job jobMessage)
        {
            QueueJob.Create(core, ttl, jobMessage);
        }

        public override List<Job> ClaimJobs(string queue, int count)
        {
            List<Job> claimedJobs = new List<Job>();

            foreach (QueueJob job in Queue.ClaimJobs(core, queue, count, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5)))
            {
                claimedJobs.Add(new Job(queue, job.Id.ToString(), null, job.Body.ToString()));
            }

            return claimedJobs;
        }

        public override void DeleteJob(Job job)
        {
            Queue.DeleteJob(core, job.JobId);
        }

        public override void CloseConnection()
        {
            // Do nothing, we are using the database from the core object
            // BoxSocial will close the database connection
        }
    }
}
