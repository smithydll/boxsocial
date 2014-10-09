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
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("applications_oauth")]
    public class OAuthApplication : ApplicationEntry
    {
        [DataFieldKey(DataFieldKeys.Unique, "u_member")]
        [DataField("application_id", DataFieldKeys.Index)]
        private new long applicationId;
        [DataField("application_website", 255)]
        private string website;
        [DataField("application_api_key", 127)]
        private string apiKey;
        [DataField("application_api_secret", 127)]
        private string apiSecret;
        [DataField("application_api_callback", 255)]
        private string apiCallback;

        public string ApiKey
        {
            get
            {
                return apiKey;
            }
            set
            {
                SetPropertyByRef(new { consumerKey = apiKey }, value);
            }
        }

        public string ApiSecret
        {
            get
            {
                return apiSecret;
            }
            set
            {
                SetPropertyByRef(new { consumerSecret = apiSecret }, value);
            }
        }

        public OAuthApplication(Core core, string apiKey)
            : base(core)
        {
            this.db = db;

            SelectQuery query = GetSelectQueryStub(core);
            query.AddCondition("applications_oauth.application_id", apiKey);

            System.Data.Common.DbDataReader applicationReader = db.ReaderQuery(query);

            if (applicationReader.HasRows)
            {
                applicationReader.Read();

                //loadItemInfo(applicationReader);

                applicationReader.Close();
                applicationReader.Dispose();
            }
            else
            {
                applicationReader.Close();
                applicationReader.Dispose();

                throw new InvalidApplicationException();
            }
        }

        public static new SelectQuery GetSelectQueryStub(Core core)
        {
            SelectQuery query = GetSelectQueryStub(core, typeof(OAuthApplication));
            query.AddFields(User.GetFieldsPrefixed(core, typeof(ApplicationEntry)));
            query.AddJoin(JoinTypes.Inner, User.GetTable(typeof(ApplicationEntry)), "application_id", "application_id");

            return query;
        }
    }
}
