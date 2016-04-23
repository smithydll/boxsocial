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
using System.Configuration;
using System.Data;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("application_error_log")]
    public class ApplicationError : NumberedItem
    {
        [DataField("error_id", DataFieldKeys.Primary)]
        private long errorId;
        [DataField("error_title", 127)]
        private string errorTitle;
        [DataField("error_body", MYSQL_TEXT)]
        private string errorBody;

        public ApplicationError(Core core, DataRow errorRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ApplicationError_ItemLoad);

            loadItemInfo(errorRow);
        }

        public ApplicationError(Core core, System.Data.Common.DbDataReader errorRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ApplicationError_ItemLoad);

            loadItemInfo(errorRow);
        }

        void ApplicationError_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return errorId;
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
