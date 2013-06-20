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
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Musician
{
    [DataTable("musician_members")]
    public class MusicianMember : User
    {
        [DataField("user_id", DataFieldKeys.Unique, "u_key")]
        private new long userId;
        [DataField("musician_id", DataFieldKeys.Unique, "u_key")]
        private long musicianId;
        [DataField("member_date_ut")]
        private long memberDateRaw;
        [DataField("member_lead")]
        private bool leadVocalist;
        [DataField("member_instruments")]
        private long instruments;
        [DataField("member_stage_name", 63)]
        private string stageName;
        [DataField("member_biography", MYSQL_TEXT)]
        private string biography;

        private Musician musician;

        public string StageName
        {
            get
            {
                return stageName;
            }
            set
            {
                SetProperty("stageName", value);
            }
        }

        public string Biography
        {
            get
            {
                return biography;
            }
            set
            {
                SetProperty("biography", value);
            }
        }

        public DateTime GetMemberDate(UnixTime tz)
        {
            return tz.DateTimeFromMysql(memberDateRaw);
        }

        public Musician Musician
        {
            get
            {
                ItemKey ownerKey = new ItemKey(musicianId, ItemKey.GetTypeId(typeof(Musician)));
                if (musician == null || ownerKey.Id != musician.Id || ownerKey.TypeId != musician.TypeId)
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

        public MusicianMember(Core core, Musician owner, User user)
            : base(core)
        {
            // load the info into a the new object being created
            this.userInfo = user.UserInfo;
            this.userProfile = user.Profile;
            this.userStyle = user.Style;
            this.userId = user.UserId;
            this.userName = user.UserName;
            this.domain = user.UserDomain;
            this.emailAddresses = user.EmailAddresses;

            SelectQuery sQuery = MusicianMember.GetSelectQueryStub(typeof(MusicianMember));
			sQuery.AddCondition("user_id", user.Id);
            sQuery.AddCondition("musician_id", owner.Id);

            try
            {
                System.Data.Common.DbDataReader reader = core.Db.ReaderQuery(sQuery);
                if (reader.HasRows)
                {
                    reader.Read();

                    loadItemInfo(typeof(MusicianMember), reader);

                    reader.Close();
                    reader.Dispose();
                }
                else
                {
                    reader.Close();
                    reader.Dispose();

                    throw new InvalidMusicianMemberException();
                }
            }
            catch (InvalidItemException)
            {
                throw new InvalidMusicianMemberException();
            }
        }

        public MusicianMember(Core core, DataRow memberRow, UserLoadOptions loadOptions)
            : base(core, memberRow, loadOptions)
        {
            loadItemInfo(typeof(MusicianMember), memberRow);
        }

        public MusicianMember(Core core, Musician owner, long userId, UserLoadOptions loadOptions)
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
                loadItemInfo(typeof(MusicianMember), memberTable.Rows[0]);
                /*loadUserInfo(memberTable.Rows[0]);
                loadUserIcon(memberTable.Rows[0]);*/
            }
            else
            {
                throw new InvalidUserException();
            }
        }

        public MusicianMember(Core core, Musician owner, string username, UserLoadOptions loadOptions)
            : base(core)
        {
            SelectQuery query = GetSelectQueryStub(UserLoadOptions.All);
            query.AddCondition("user_keys.username", username);
            query.AddCondition("musician_id", owner.Id);

            DataTable memberTable = db.Query(query);


            if (memberTable.Rows.Count == 1)
            {
                loadItemInfo(typeof(User), memberTable.Rows[0]);
                loadItemInfo(typeof(UserInfo), memberTable.Rows[0]);
                loadItemInfo(typeof(UserProfile), memberTable.Rows[0]);
                loadItemInfo(typeof(MusicianMember), memberTable.Rows[0]);
            }
            else
            {
                throw new InvalidUserException();
            }
        }

        public MusicianMember(Core core, DataRow memberRow)
            : base(core)
        {
            loadItemInfo(typeof(MusicianMember), memberRow);
            core.LoadUserProfile(userId);
            loadUserFromUser(core.PrimitiveCache[userId]);
        }

        public MusicianMember(Core core, Musician musician, long userId)
            : base(core)
        {
            this.db = db;

            SelectQuery query = GetSelectQueryStub(UserLoadOptions.All);
            query.AddCondition("user_keys.user_id", userId);
            query.AddCondition("musician_members.musician_id", musician.Id);

            DataTable memberTable = db.Query(query);

            if (memberTable.Rows.Count == 1)
            {
                loadItemInfo(typeof(MusicianMember), memberTable.Rows[0]);
                core.LoadUserProfile(userId);
                loadUserFromUser(core.PrimitiveCache[userId]);
            }
            else
            {
                throw new InvalidUserException();
            }
        }

        public static MusicianMember Create(Core core, Musician musician, User member)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            InsertQuery iQuery = new InsertQuery(typeof(MusicianMember));
            iQuery.AddField("user_id", member.Id);
            iQuery.AddField("musician_id", musician.Id);
            iQuery.AddField("member_date_ut", UnixTime.UnixTimeStamp());
            iQuery.AddField("member_lead", false);
            iQuery.AddField("member_instruments", 0);
            iQuery.AddField("member_stage_name", string.Empty);
            iQuery.AddField("member_biography", string.Empty);

            core.Db.Query(iQuery);

            MusicianMember newMember = new MusicianMember(core, musician, member);

            return newMember;
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

        public List<Instrument> GetInstruments()
        {
            List<Instrument> instruments = new List<Instrument>();

            SelectQuery query = Item.GetSelectQueryStub(typeof(MusicianInstruments));
            query.AddFields(Item.GetFieldsPrefixed(typeof(Instrument)));
            query.AddCondition("musician_id", musicianId);
            query.AddCondition("user_id", userId);
            query.AddJoin(JoinTypes.Inner, new DataField(Item.GetTable(typeof(MusicianInstruments)), "instrument_id"), new DataField(Item.GetTable(typeof(Instrument)), "instrument_id"));

            DataTable instrumentDataTable = core.Db.Query(query);

            foreach (DataRow dr in instrumentDataTable.Rows)
            {
                instruments.Add(new Instrument(core, dr));
            }

            return instruments;
        }

        public static void ShowAll(object sender, ShowMPageEventArgs e)
        {
            e.Template.SetTemplate("Musician", "viewmusician_members");

            List<MusicianMember> members = e.Page.Musician.GetMembers();

            foreach (MusicianMember member in members)
            {
                VariableCollection memberVariableCollection = e.Template.CreateChild("member_list");

                memberVariableCollection.Parse("STAGE_NAME", member.StageName);
            }
        }

        public static void Show(object sender, ShowMPageEventArgs e)
        {
            e.Template.SetTemplate("Musician", "viewmusician_member");

            MusicianMember member = null;

            try
            {
                member = new MusicianMember(e.Core, (Musician)e.Page.Owner, e.Slug, UserLoadOptions.All);
            }
            catch (InvalidMusicianMemberException)
            {
                e.Core.Functions.Generate404();
                return;
            }

            List<Instrument> instruments = member.GetInstruments();

            foreach (Instrument instrument in instruments)
            {
                VariableCollection instrumentVariableCollection = e.Template.CreateChild("instrument_list");

                instrumentVariableCollection.Parse("NAME", instrument.Name);
            }

            e.Core.Display.ParseBbcode("BIOGRAPHY", member.Biography);
        }
    }

    public class InvalidMusicianMemberException : Exception
    {
    }
}
