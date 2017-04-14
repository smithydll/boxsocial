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
    [DataTable("queue_jobs")]
    public class QueueJob : NumberedItem
    {
        [DataField("job_id", DataFieldKeys.Primary)]
        private long jobId;
        [DataField("job_queue_id", typeof(Queue))]
        private long jobQueueId;
        [DataField("job_queue_name", 31)]
        private string jobQueueName;
        [DataField("job_body", MYSQL_TEXT)]
        private string jobBody;
        [DataField("job_time")]
        private long jobTime;
        [DataField("job_ttl")]
        private long jobTimeToLive;
        [DataField("job_claim_id")]
        private long jobClaimId;
        [DataField("job_claimed")]
        private bool jobClaimed;
        [DataField("job_claimed_time")]
        private long jobClaimedTime;
        [DataField("job_claimed_grace")]
        private long jobClaimedGrace;
        [DataField("job_processed")]
        private bool jobProcessed;
        [DataField("job_processed_time")]
        private long jobProcessedTime;

        public string Body
        {
            get
            {
                return jobBody;
            }
        }

        public QueueJob(Core core, long jobId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(QueueJob_ItemLoad);

            try
            {
                LoadItem(jobId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidQueueJobException();
            }
        }

        public QueueJob(Core core, DataRow jobRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(QueueJob_ItemLoad);

            loadItemInfo(jobRow);
        }

        private void QueueJob_ItemLoad()
        {
        }

        public static QueueJob Create(Core core, TimeSpan ttl, Job jobMessage)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            Queue queue = new Queue(core, jobMessage.QueueName);

            InsertQuery iQuery = new InsertQuery(typeof(QueueJob));
            iQuery.AddField("job_queue_id", queue.Id);
            iQuery.AddField("job_queue_name", jobMessage.QueueName);
            iQuery.AddField("job_body", jobMessage.ToString());
            iQuery.AddField("job_time", UnixTime.UnixTimeStamp());
            iQuery.AddField("job_ttl", ttl.TotalSeconds);
            iQuery.AddField("job_claimed", false);
            iQuery.AddField("job_claimed_time", 0);
            iQuery.AddField("job_claimed_grace", 0);
            iQuery.AddField("job_processed", false);
            iQuery.AddField("job_processed_time", 0);

            long jobId = core.Db.Query(iQuery);

            QueueJob newQueue = new QueueJob(core, jobId);

            return newQueue;
        }

        public override long Id
        {
            get
            {
                return jobId;
            }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class InvalidQueueJobException : Exception
    {
    }
}
