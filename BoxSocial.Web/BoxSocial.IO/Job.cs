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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace BoxSocial.IO
{
    [JsonObject("job")]
    public class Job
    {
        string jobId;
        string handle;
        string queueName;
        long applicationId;
        long itemTypeId;
        long itemId;
        string function;
        string body;
        long userId;

        [JsonIgnore()]
        public string JobId
        {
            get
            {
                return jobId;
            }
        }

        [JsonIgnore()]
        public string Handle
        {
            get
            {
                return handle;
            }
        }

        [JsonIgnore()]
        public string QueueName
        {
            get
            {
                return queueName;
            }
        }

        [JsonProperty("application_id")]
        public long ApplicationId
        {
            get
            {
                return applicationId;
            }
        }

        [JsonProperty("user_id")]
        public long UserId
        {
            get
            {
                return userId;
            }
        }

        [JsonProperty("item_type_id")]
        public long ItemTypeId
        {
            get
            {
                return itemTypeId;
            }
        }

        [JsonProperty("item_id")]
        public long ItemId
        {
            get
            {
                return itemId;
            }
        }

        [JsonProperty("function")]
        public string Function
        {
            get
            {
                return function;
            }
        }

        [JsonProperty("body")]
        public string Body
        {
            get
            {
                return body;
            }
        }

        [JsonIgnore()]
        public string Message
        {
            get
            {
                return ToString();
            }
        }

        internal Job(string queueName, string jobId, string jobHandle, string message)
        {
            this.queueName = queueName;
            this.jobId = jobId;
            this.handle = jobHandle;
            Dictionary<string, string> strings = (Dictionary<string, string>)JsonConvert.DeserializeObject(message, typeof(Dictionary<string, string>));

            long.TryParse(strings["application_id"], out this.applicationId);
            long.TryParse(strings["user_id"], out this.userId);
            long.TryParse(strings["item_type_id"], out this.itemTypeId);
            long.TryParse(strings["item_id"], out this.itemId);
            this.function = strings["function"];
            this.body = strings["body"];
        }

        internal Job(string queueName, string jobId, string jobHandle, long applicationId, long userId, long itemTypeId, long itemId, string function)
            : this(queueName, jobId, jobHandle, applicationId, userId, itemTypeId, itemId, function, string.Empty)
        {
        }

        internal Job(string queueName, string jobId, string jobHandle, long applicationId, long userId, long itemTypeId, long itemId, string function, string body)
        {
            this.queueName = queueName;
            this.jobId = jobId;
            this.handle = jobHandle;
            this.applicationId = applicationId;
            this.userId = userId;
            this.itemTypeId = itemTypeId;
            this.itemId = itemId;
            this.function = function;
            this.body = body;
        }

        public Job(string queueName, long applicationId, long userId, long itemTypeId, long itemId, string function)
            : this(queueName, applicationId, userId, itemTypeId, itemId, function, string.Empty)
        {
        }

        public Job(string queueName, long applicationId, long userId, long itemTypeId, long itemId, string function, string body)
        {
            this.queueName = queueName;
            this.jobId = string.Empty;
            this.handle = string.Empty;
            this.applicationId = applicationId;
            this.userId = userId;
            this.itemTypeId = itemTypeId;
            this.itemId = itemId;
            this.function = function;
            this.body = body;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
