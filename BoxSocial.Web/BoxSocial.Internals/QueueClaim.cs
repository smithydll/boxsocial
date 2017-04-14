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
    [DataTable("queue_claims")]
    public class QueueClaim : NumberedItem
    {
        [DataField("claim_id", DataFieldKeys.Primary)]
        private long claimId;
        [DataField("claim_time")]
        private long claimTime;
        [DataField("claim_ttl")]
        private long claimTimeToLive;

        public QueueClaim(Core core, long claimId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(QueueClaim_ItemLoad);

            try
            {
                LoadItem(claimId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidQueueClaimException();
            }
        }

        public QueueClaim(Core core, DataRow claimRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(QueueClaim_ItemLoad);

            loadItemInfo(claimRow);
        }

        private void QueueClaim_ItemLoad()
        {
        }

        public static long Create(Core core, TimeSpan ttl)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            InsertQuery iQuery = new InsertQuery(typeof(QueueJob));
            iQuery.AddField("claim_time", UnixTime.UnixTimeStamp());
            iQuery.AddField("claim_ttl", ttl.TotalSeconds);

            long claimId = core.Db.Query(iQuery);

            return claimId;
        }

        public override long Id
        {
            get
            {
                return claimId;
            }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class InvalidQueueClaimException : Exception
    {
    }
}
