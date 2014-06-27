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

namespace BoxSocial.IO
{
    public abstract class JobQueue
    {
        public JobQueue()
        {
        }

        public abstract void CreateQueue(string queue);

        public abstract void DeleteQueue(string queue);

        public abstract bool QueueExists(string queue);

        public abstract void PushJob(string queue, TimeSpan ttl, string jobMessage);

        public abstract void DeleteJob(Job job);

        public abstract List<Job> ClaimJobs(string queue, int count);
    }
}
