/*
 * Box Social™
 * http://boxsocial.net/
 * Copyright © 2007, David Lachlan Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class StatusMessage : NumberedItem
    {
        public const string STATUS_MESSAGE_FIELDS = "usm.status_id, usm.user_id, usm.status_message, usm.status_time_ut";

        private long statusId;
        private User owner;
        private long ownerId;
        private string statusMessage;
        private long timeRaw;

        public long StatusId
        {
            get
            {
                return statusId;
            }
        }

        public User Owner
        {
            get
            {
                return owner;
            }
        }

        public string Message
        {
            get
            {
                return statusMessage;
            }
        }

        public long TimeRaw
        {
            get
            {
                return timeRaw;
            }
        }

        public DateTime GetTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(timeRaw);
        }

        public StatusMessage(Core core, User owner, DataRow statusRow) : base(core)
        {
            this.owner = owner;

            loadStatusInfo(statusRow);
        }

        private StatusMessage(Core core, User owner, long statusId, string statusMessage) : base(core)
        {
            this.owner = owner;
            this.ownerId = owner.Id;
            this.statusId = statusId;
            this.statusMessage = statusMessage;
        }

        private void loadStatusInfo(DataRow statusRow)
        {
            statusId = (long)statusRow["status_id"];
            statusMessage = (string)statusRow["status_message"];
            ownerId = (long)statusRow["user_id"];
            timeRaw = (long)statusRow["status_time_ut"];
        }

        public static StatusMessage Create(Core core, User creator, string message)
        {
            InsertQuery iQuery = new InsertQuery("user_status_messages");
            iQuery.AddField("user_id", creator.Id);
            iQuery.AddField("status_message", message);
            iQuery.AddField("status_time_ut", UnixTime.UnixTimeStamp());

            long statusId = core.db.Query(iQuery);

            UpdateQuery uQuery = new UpdateQuery("user_info");
            uQuery.AddField("user_status_messages", new QueryOperation("user_status_messages", QueryOperations.Addition, 1));
            uQuery.AddCondition("user_id", creator.Id);

            core.db.Query(uQuery);

            return new StatusMessage(core, creator, statusId, message);
        }

        public override long Id
        {
            get
            {
                return statusId;
            }
        }

        public override string Namespace
        {
            get
            {
                return this.GetType().FullName;
            }
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
