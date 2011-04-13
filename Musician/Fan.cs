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

namespace BoxSocial.Musician
{
    [DataTable("fans")]
    public class Fan : User
    {
        [DataField("user_id")]
        private new long userId;
        [DataField("musician_id")]
        private long musicianId;
        [DataField("fan_date_ut")]
        private long fanDateRaw;

        private Musician musician;

        public DateTime GetFanDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(fanDateRaw);
        }

        public Musician Musician
        {
            get
            {
                ItemKey ownerKey = new ItemKey(musicianId, ItemKey.GetTypeId(typeof(Musician)));
                if (musician == null || ownerKey.Id != musician.Id || ownerKey.TypeString != musician.Type)
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(ownerKey);
                    musician = (Musician)core.PrimitiveCache[ownerKey];
                    return musician;
                }
                else
                {
                    return musician;
                }
            }
        }

        public Fan(Core core, Musician owner, User user)
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

            SelectQuery sQuery = Fan.GetSelectQueryStub(typeof(Fan));
			sQuery.AddCondition("user_id", user.Id);
            sQuery.AddCondition("musician_id", owner.Id);

            try
            {
                loadItemInfo(typeof(Fan), core.Db.ReaderQuery(sQuery));
            }
            catch (InvalidItemException)
            {
                throw new InvalidFanException();
            }
        }

        public Fan(Core core, DataRow memberRow, UserLoadOptions loadOptions)
            : base(core, memberRow, loadOptions)
        {
            loadItemInfo(typeof(Fan), memberRow);
        }

        public Fan(Core core, Musician owner, long userId, UserLoadOptions loadOptions)
            : base(core)
        {
            SelectQuery query = GetSelectQueryStub(UserLoadOptions.All);
            query.AddCondition("user_keys.user_id", userId);
            query.AddCondition("musician_id", owner.Id);
			
            DataTable memberTable = db.Query(query);

            if (memberTable.Rows.Count == 1)
            {
				loadItemInfo(typeof(User), memberTable.Rows[0]);
				loadItemInfo(typeof(UserInfo), memberTable.Rows[0]);
				loadItemInfo(typeof(UserProfile), memberTable.Rows[0]);
                loadItemInfo(typeof(Fan), memberTable.Rows[0]);
                /*loadUserInfo(memberTable.Rows[0]);
                loadUserIcon(memberTable.Rows[0]);*/
            }
            else
            {
                throw new InvalidUserException();
            }
        }

        public Fan(Core core, DataRow memberRow)
            : base(core)
        {
            loadItemInfo(typeof(Fan), memberRow);
            core.LoadUserProfile(userId);
            loadUserFromUser(core.PrimitiveCache[userId]);
        }

        public static Fan Create(Core core)
        {
            throw new NotImplementedException();
        }

        public static void ShowAll(object sender, ShowMPageEventArgs e)
        {
            e.Template.SetTemplate("Musician", "viewfans");

            Musician musician = e.Page.Musician;

            e.Template.Parse("U_FILTER_ALL", musician.FansUri);
            e.Template.Parse("U_FILTER_BEGINS_A", musician.GetFansUri("a"));
            e.Template.Parse("U_FILTER_BEGINS_B", musician.GetFansUri("b"));
            e.Template.Parse("U_FILTER_BEGINS_C", musician.GetFansUri("c"));
            e.Template.Parse("U_FILTER_BEGINS_D", musician.GetFansUri("d"));
            e.Template.Parse("U_FILTER_BEGINS_E", musician.GetFansUri("e"));
            e.Template.Parse("U_FILTER_BEGINS_F", musician.GetFansUri("f"));
            e.Template.Parse("U_FILTER_BEGINS_G", musician.GetFansUri("g"));
            e.Template.Parse("U_FILTER_BEGINS_H", musician.GetFansUri("h"));
            e.Template.Parse("U_FILTER_BEGINS_I", musician.GetFansUri("i"));
            e.Template.Parse("U_FILTER_BEGINS_J", musician.GetFansUri("j"));
            e.Template.Parse("U_FILTER_BEGINS_K", musician.GetFansUri("k"));
            e.Template.Parse("U_FILTER_BEGINS_L", musician.GetFansUri("l"));
            e.Template.Parse("U_FILTER_BEGINS_M", musician.GetFansUri("m"));
            e.Template.Parse("U_FILTER_BEGINS_N", musician.GetFansUri("n"));
            e.Template.Parse("U_FILTER_BEGINS_O", musician.GetFansUri("o"));
            e.Template.Parse("U_FILTER_BEGINS_P", musician.GetFansUri("p"));
            e.Template.Parse("U_FILTER_BEGINS_Q", musician.GetFansUri("q"));
            e.Template.Parse("U_FILTER_BEGINS_R", musician.GetFansUri("r"));
            e.Template.Parse("U_FILTER_BEGINS_S", musician.GetFansUri("s"));
            e.Template.Parse("U_FILTER_BEGINS_T", musician.GetFansUri("t"));
            e.Template.Parse("U_FILTER_BEGINS_U", musician.GetFansUri("u"));
            e.Template.Parse("U_FILTER_BEGINS_V", musician.GetFansUri("v"));
            e.Template.Parse("U_FILTER_BEGINS_W", musician.GetFansUri("w"));
            e.Template.Parse("U_FILTER_BEGINS_X", musician.GetFansUri("x"));
            e.Template.Parse("U_FILTER_BEGINS_Y", musician.GetFansUri("y"));
            e.Template.Parse("U_FILTER_BEGINS_Z", musician.GetFansUri("z"));

            List<Fan> fans = musician.GetFans(e.Page.TopLevelPageNumber, 20, e.Core.Functions.GetFilter());

            foreach (Fan fan in fans)
            {
                VariableCollection fanVariableCollection = e.Template.CreateChild("fan_list");

                fanVariableCollection.Parse("DISPLAY_NAME", fan.DisplayName);
            }
        }
    }

    public class InvalidFanException : Exception
    {
    }
}
