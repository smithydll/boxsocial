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
        [DataFieldKey(DataFieldKeys.Unique, "u_application")]
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
            : base(core, false)
        {
            this.db = db;

            SelectQuery query = GetSelectQueryStub(core);
            query.AddCondition(new DataField("applications_oauth", "application_api_key"), apiKey);

            System.Data.Common.DbDataReader applicationReader = db.ReaderQuery(query);

            if (applicationReader.HasRows)
            {
                applicationReader.Read();

                loadItemInfo(applicationReader);
                loadApplication(applicationReader);

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

        public OAuthApplication(Core core, ApplicationEntry ae)
            : base(core, false)
        {
            this.db = db;

            SelectQuery query = GetSelectQueryStub(core);
            query.AddCondition(new DataField("applications_oauth", "application_id"), ae.Id);

            System.Data.Common.DbDataReader applicationReader = db.ReaderQuery(query);

            if (applicationReader.HasRows)
            {
                applicationReader.Read();

                loadItemInfo(applicationReader);
                loadApplication(applicationReader);

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

        protected override void loadItemInfo(System.Data.Common.DbDataReader applicationRow)
        {
            loadOAuthApplication(applicationRow);
        }

        protected void loadOAuthApplication(System.Data.Common.DbDataReader applicationRow)
        {
            try
            {
                loadValue(applicationRow, "application_id", out applicationId);
                loadValue(applicationRow, "application_website", out website);
                loadValue(applicationRow, "application_api_key", out apiKey);
                loadValue(applicationRow, "application_api_secret", out apiSecret);
                loadValue(applicationRow, "application_api_callback", out apiCallback);

                itemLoaded(applicationRow);
                core.ItemCache.RegisterItem((NumberedItem)this);
            }
            catch
            {
                throw new InvalidItemException();
            }
        }

        public static new SelectQuery GetSelectQueryStub(Core core)
        {
            SelectQuery query = GetSelectQueryStub(core, typeof(OAuthApplication));
            query.AddFields(ApplicationEntry.GetFieldsPrefixed(core, typeof(ApplicationEntry)));
            query.AddJoin(JoinTypes.Inner, ApplicationEntry.GetTable(typeof(ApplicationEntry)), "application_id", "application_id");

            return query;
        }

        public static OAuthApplication Create(Core core, string title, string slug, string description)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            core.Db.BeginTransaction();

            InsertQuery iQuery = new InsertQuery(typeof(ApplicationEntry));
            iQuery.AddField("application_name", slug);
            iQuery.AddField("user_id", core.LoggedInMemberId);
            iQuery.AddField("application_date_ut", UnixTime.UnixTimeStamp());
            iQuery.AddField("application_title", title);
            iQuery.AddField("application_description", description);
            iQuery.AddField("application_primitive", false);
            iQuery.AddField("application_primitives", (byte)AppPrimitives.None);
            iQuery.AddField("application_comment", false);
            iQuery.AddField("application_rating", false);
            iQuery.AddField("application_style", false);
            iQuery.AddField("application_script", false);
            iQuery.AddField("application_type", (byte)ApplicationType.OAuth);

            long applicationId = core.Db.Query(iQuery);

            ApplicationEntry newApplication = new ApplicationEntry(core, applicationId);

            iQuery = new InsertQuery(typeof(OAuthApplication));
            iQuery.AddField("application_id", applicationId);
            iQuery.AddField("application_website", string.Empty);
            iQuery.AddField("application_api_key", OAuth.GeneratePublic());
            iQuery.AddField("application_api_secret", OAuth.GenerateSecret());
            iQuery.AddField("application_api_callback", string.Empty);

            core.Db.Query(iQuery);

            OAuthApplication newApp = new OAuthApplication(core, newApplication);

            ApplicationDeveloper developer = ApplicationDeveloper.Create(core, newApplication, core.Session.LoggedInMember);

            try
            {
                ApplicationEntry profileAe = core.GetApplication("Profile");
                profileAe.Install(core, newApplication);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry guestbookAe = core.GetApplication("GuestBook");
                guestbookAe.Install(core, newApplication);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry galleryAe = core.GetApplication("Gallery");
                galleryAe.Install(core, newApplication);
            }
            catch
            {
            }

            return newApp;
        }
    }
}
