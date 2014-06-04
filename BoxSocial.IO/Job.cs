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
    public class Job
    {
        string jobId;
        string handle;
        string queueName;
        string message;

        public string JobId
        {
            get
            {
                return jobId;
            }
        }

        public string Handle
        {
            get
            {
                return handle;
            }
        }

        public string QueueName
        {
            get
            {
                return queueName;
            }
        }

        public string Message
        {
            get
            {
                return message;
            }
        }

        public Job(string queueName, string jobId, string jobHandle, string message)
        {
            this.queueName = queueName;
            this.queueName = jobId;
            this.message = message;
            this.handle = jobHandle;
        }
    }
}
