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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("spam_reports")]
    public class SpamReport : NumberedItem
    {
        [DataField("report_id", DataFieldKeys.Primary)]
        private long reportId;
        [DataField("comment_id")]
        private long commentId;
        [DataField("user_id")]
        private long userId;
        [DataField("report_time_ut")]
        private long reportTimeRaw;

        public long ReportId
        {
            get
            {
                return reportId;
            }
        }

        public SpamReport(Core core, long reportId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(SpamReport_ItemLoad);

            try
            {
                LoadItem(reportId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidSpamReportException();
            }
        }

        public SpamReport(Core core, DataRow reportRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(SpamReport_ItemLoad);

            loadItemInfo(reportRow);
        }

        void SpamReport_ItemLoad()
        {
            
        }

        public override long Id
        {
            get
            {
                return reportId;
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

    public class InvalidSpamReportException : Exception
    {
    }
}
