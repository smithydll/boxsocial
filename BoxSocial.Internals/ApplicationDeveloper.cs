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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("application_developers")]
    public class ApplicationDeveloper : User
    {
        [DataField("user_id", DataFieldKeys.Unique, "u_key")]
        private new long userId;
        [DataField("application_id", DataFieldKeys.Unique, "u_key")]
        private long applicationId;

        private ApplicationEntry application;

        public ApplicationEntry Application
        {
            get
            {
                ItemKey ownerKey = new ItemKey(applicationId, ItemKey.GetTypeId(typeof(ApplicationEntry)));
                if (application == null || ownerKey.Id != application.Id || ownerKey.TypeId != application.TypeId)
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(ownerKey);
                    application = (ApplicationEntry)core.PrimitiveCache[ownerKey];
                    return application;
                }
                else
                {
                    return application;
                }
            }
        }

        public ApplicationDeveloper(Core core, ApplicationEntry owner, User user)
            : base(core)
        {
            // load the info into a the new object being created
            this.userInfo = user.Info;
            this.userProfile = user.Profile;
            this.userStyle = user.Style;
            this.userId = user.UserId;
            this.userName = user.UserName;
            this.domain = user.UserDomain;
            this.emailAddresses = user.EmailAddresses;

            SelectQuery sQuery = ApplicationDeveloper.GetSelectQueryStub(typeof(ApplicationDeveloper));
			sQuery.AddCondition("user_id", user.Id);
            sQuery.AddCondition("application_id", owner.Id);

            try
            {
                loadItemInfo(typeof(ApplicationDeveloper), core.Db.ReaderQuery(sQuery));
            }
            catch (InvalidItemException)
            {
                throw new InvalidApplicationDeveloperException();
            }
        }

        public ApplicationDeveloper(Core core, DataRow memberRow, UserLoadOptions loadOptions)
            : base(core, memberRow, loadOptions)
        {
            loadItemInfo(typeof(ApplicationDeveloper), memberRow);
        }

        public ApplicationDeveloper(Core core, ApplicationEntry owner, long userId, UserLoadOptions loadOptions)
            : base(core)
        {
            SelectQuery query = GetSelectQueryStub(UserLoadOptions.All);
            query.AddCondition("user_keys.user_id", userId);
            query.AddCondition("application_id", owner.Id);
			
            DataTable memberTable = db.Query(query);
			

            if (memberTable.Rows.Count == 1)
            {
				loadItemInfo(typeof(User), memberTable.Rows[0]);
				loadItemInfo(typeof(UserInfo), memberTable.Rows[0]);
				loadItemInfo(typeof(UserProfile), memberTable.Rows[0]);
                loadItemInfo(typeof(ApplicationDeveloper), memberTable.Rows[0]);
                /*loadUserInfo(memberTable.Rows[0]);
                loadUserIcon(memberTable.Rows[0]);*/
            }
            else
            {
                throw new InvalidUserException();
            }
        }

        public ApplicationDeveloper(Core core, ApplicationEntry owner, string username, UserLoadOptions loadOptions)
            : base(core)
        {
            SelectQuery query = GetSelectQueryStub(UserLoadOptions.All);
            query.AddCondition("user_keys.username", username);
            query.AddCondition("application_id", owner.Id);

            DataTable memberTable = db.Query(query);


            if (memberTable.Rows.Count == 1)
            {
                loadItemInfo(typeof(User), memberTable.Rows[0]);
                loadItemInfo(typeof(UserInfo), memberTable.Rows[0]);
                loadItemInfo(typeof(UserProfile), memberTable.Rows[0]);
                loadItemInfo(typeof(ApplicationDeveloper), memberTable.Rows[0]);
            }
            else
            {
                throw new InvalidUserException();
            }
        }

        public ApplicationDeveloper(Core core, DataRow memberRow)
            : base(core)
        {
            loadItemInfo(typeof(ApplicationDeveloper), memberRow);
            core.LoadUserProfile(userId);
            loadUserFromUser(core.PrimitiveCache[userId]);
        }

        public static ApplicationDeveloper Create(Core core, ApplicationEntry application, User developer)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            InsertQuery iQuery = new InsertQuery(typeof(ApplicationDeveloper));
            iQuery.AddField("user_id", developer.Id);
            iQuery.AddField("application_id", application.Id);

            core.Db.Query(iQuery);

            ApplicationDeveloper newDeveloper = new ApplicationDeveloper(core, application, developer);

            return newDeveloper;
        }

        public override bool CanEditItem()
        {
            if (userId == core.LoggedInMemberId)
            {
                return true;
            }
            if (Owner != null && Owner.CanEditItem())
            {
                return true;
            }

            return false;
        }

        public static void Show(object sender, ShowAPageEventArgs e)
        {
        }
    }

    public class InvalidApplicationDeveloperException : Exception
    {
    }
}
