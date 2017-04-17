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
using System.Data;
using System.Linq;
using System.Text;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("queues")]
    public class Queue : NumberedItem
    {
        [DataField("queue_id", DataFieldKeys.Primary)]
        private long queueId;
        [DataField("queue_name", DataFieldKeys.Unique, 31)]
        private string queueName;

        public string Name
        {
            get
            {
                return queueName;
            }
        }

        public static bool Exists(Core core, string queueName)
        {
            SelectQuery q = new SelectQuery(typeof(Queue));
            q.AddField(new DataField(typeof(Queue), "queue_id"));
            q.AddCondition("queue_name", queueName);

            return (core.Db.Query(q).Rows.Count > 0);
        }

        public static List<QueueJob> ClaimJobs(Core core, string queueName, int limit, TimeSpan timeToLive, TimeSpan gracePeriod)
        {
            List<QueueJob> jobs = new List<QueueJob>();

            long claimId = QueueClaim.Create(core, timeToLive);
            long time = UnixTime.UnixTimeStamp();

            UpdateQuery uQuery = new UpdateQuery(typeof(QueueJob));
            uQuery.AddField("job_claim_id", claimId);
            uQuery.AddField("job_claimed", true);
            uQuery.AddField("job_claimed_time", time);
            uQuery.AddCondition("job_queue_name", queueName);
            QueryCondition qc1 = uQuery.AddCondition("job_claimed", false);
            QueryCondition qc2 = qc1.AddCondition(ConditionRelations.Or, "job_claimed", true);
            qc2.AddCondition(new QueryOperation("job_claimed_time", QueryOperations.Addition, timeToLive.TotalSeconds), ConditionEquality.GreaterThan, time);
            uQuery.AddCondition("job_processed", false);
            uQuery.AddSort(SortOrder.Ascending, "job_time"); // Oldest first, FIFO
            uQuery.LimitCount = limit;

            core.Db.Query(uQuery);

            SelectQuery q = QueueJob.GetSelectQueryStub(core, typeof(QueueJob));
            q.AddCondition("job_claim_id", claimId);

            DataTable jobsTable = core.Db.Query(q);

            foreach (DataRow jobRow in jobsTable.Rows)
            {
                jobs.Add(new QueueJob(core, jobRow));
            }

            // cleanup claims, once an hour to reduce table locking queries
            if (DateTime.Now.Minute == 0)
            {
                DeleteQuery dQuery = new DeleteQuery(typeof(QueueClaim));
                dQuery.AddCondition(new QueryOperation("claim_time", QueryOperations.Addition, new QueryField("claim_ttl")), ConditionEquality.GreaterThan, time);
                core.Db.Query(dQuery);
            }

            return jobs;
        }

        public static bool DeleteJob(Core core, string jobId)
        {
            long jobIdLong = 0;
            long.TryParse(jobId, out jobIdLong);

            if (jobIdLong > 0)
            {
                // It doesn't matter if the record exists, there is no
                // synchronisation, just delete the record if it exists
                DeleteQuery query = new DeleteQuery(typeof(QueueJob));
                query.AddCondition("job_id", jobIdLong);

                core.Db.Query(query);
            }

            return true;
        }

        public Queue(Core core, string queueName)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Queue_ItemLoad);

            this.queueName = queueName;

            try
            {
                LoadItem("queue_name", queueName);
            }
            catch (InvalidItemException)
            {
                throw new InvalidQueueException();
            }
        }

        public Queue(Core core, DataRow queueRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Queue_ItemLoad);

            loadItemInfo(queueRow);
        }

        private void Queue_ItemLoad()
        {
        }

        public static Queue Create(Core core, string queueName)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            InsertQuery iQuery = new InsertQuery(typeof(Queue));
            iQuery.AddField("queue_name", queueName);

            long queueId = core.Db.Query(iQuery);

            Queue newQueue = new Queue(core, queueName);

            return newQueue;
        }

        public override long Id
        {
            get
            {
                return queueId;
            }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class InvalidQueueException : Exception
    {
    }
}
