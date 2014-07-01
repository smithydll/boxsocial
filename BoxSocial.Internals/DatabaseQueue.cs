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
    public class DatabaseQueue : JobQueue
    {
        private Core core;

        public DatabaseQueue(Core core)
        {
            this.core = core;
        }

        public override void CreateQueue(string queue)
        {
        }

        public override void DeleteQueue(string queue)
        {
        }

        public override bool QueueExists(string queue)
        {
            return false;
        }

        public override void PushJob(Job jobMessage)
        {
        }

        public override void PushJob(TimeSpan ttl, Job jobMessage)
        {
        }

        public override List<Job> ClaimJobs(string queue, int count)
        {
            List<Job> claimedJobs = new List<Job>();

            return claimedJobs;
        }

        public override void DeleteJob(Job job)
        {
        }
    }
}
