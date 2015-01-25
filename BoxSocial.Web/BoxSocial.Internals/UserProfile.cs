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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using BoxSocial.IO;

namespace BoxSocial.Internals
{

    public enum Gender : byte
    {
        Undefined = 0x00,
        Male = 0x01,
        Female = 0x02,
        Intersex = 0x03,
    }

    public enum Sexuality : byte
    {
        Undefined = 0x00,
        Unsure = 0x01,
        Asexual = 0x02,
        Bisexual = 0x03,
        Hetrosexual = 0x04,
        Homosexual = 0x05,
        Pansexual = 0x06,
        Polysexual = 0x07,
    }

    public enum MaritialStatus : byte
    {
        Undefined = 0x00,
        Single = 0x01,
        MonogomousRelationship = 0x02,
        OpenRelationship = 0x03,
        Engaged = 0x04,
        Married = 0x05,
        Separated = 0x06,
        Divorced = 0x07,
        Widowed = 0x08,
    }

    [DataTable("user_profile", "USER")]
    [JsonObject("user_profile")]
    public sealed class UserProfile : NumberedItem
    {
        private User user;

        [DataField("user_id", DataFieldKeys.Unique)]
        private long userId;
        [DataField("profile_autobiography", MYSQL_TEXT)]
        private string autobiography;
        [DataField("profile_curriculum_vitae", MYSQL_TEXT)]
        private string curriculumVitae;
        [DataField("profile_gender")]
        private byte gender;
        // There is no way the labels I used would have made everyone happy
        [DataField("profile_interested_in_men")]
        private bool interestedInMen;
        [DataField("profile_interested_in_women")]
        private bool interestedInWomen;
        [DataField("profile_sexuality")]
        private byte sexuality;
        [DataField("profile_maritial_status")]
        private byte maritialStatus;
        [DataField("profile_maritial_with")]
        private long maritialWith;
        [DataField("profile_maritial_with_confirmed")]
        private bool maritialWithConfirmed;
        [DataField("profile_name_title", 8)]
        private string nameTitle;
        [DataField("profile_name_first", 36)]
        private string nameFirst;
        [DataField("profile_name_middle", 36)]
        private string nameMiddle;
        [DataField("profile_name_last", 36)]
        private string nameLast;
        [DataField("profile_name_suffix", 8)]
        private string nameSuffix;
        [DataField("profile_hometown")]
        private uint hometown;
        [DataField("profile_views")]
        private long profileViews;
        [DataField("profile_height")]
        private byte height;
        [DataField("profile_weight")]
        private uint weight;
        [DataField("profile_religion")]
        private short religionId;
        [DataField("profile_comments")]
        private long profileComments;
        [DataField("profile_date_of_birth_ut")]
        private long dateofBirthRaw;
        [DataField("profile_date_of_birth_month_cache")]
        private byte dateofBirthMonthRaw;
        [DataField("profile_date_of_birth_day_cache")]
        private byte dateofBirthDayRaw;
        /* Address */
        [DataField("profile_country", 2)]
        private string country;
        [DataField("profile_address_line_1", 127)]
        private string addressLine1;
        [DataField("profile_address_line_2", 127)]
        private string addressLine2;
        [DataField("profile_address_town", 63)]
        private string addressTown;
        [DataField("profile_address_state", 31)]
        private string addressState;
        [DataField("profile_address_post_code", 6)]
        private string addressPostCode;

        private string countryName;
        private string religionTitle;

        [JsonIgnore]
        public User User
        {
            get
            {
                if (user == null || user.Id != UserId)
                {
                    user = core.PrimitiveCache[UserId];
                }
                return user;
            }
        }

        [JsonProperty("id")]
        public long UserId
        {
            get
            {
                return userId;
            }
        }

        [JsonIgnore]
        public string Gender
        {
            get
            {
                switch (GenderRaw)
                {
                    case Internals.Gender.Male:
                        return core.Prose.GetString("MALE");
                    case Internals.Gender.Female:
                        return core.Prose.GetString("FEMALE");
                    case Internals.Gender.Intersex:
                        return core.Prose.GetString("INTERSEX");
                    default:
                        return "FALSE";
                }
            }
        }

        [JsonIgnore]
        public Gender GenderRaw
        {
            get
            {
                return (Gender)gender;
            }
            set
            {
                SetPropertyByRef(new { gender }, (byte)value);
            }
        }

        [JsonIgnore]
        public bool InterestedInMen
        {
            get
            {
                return interestedInMen;
            }
            set
            {
                SetPropertyByRef(new { interestedInMen }, value);
            }
        }

        [JsonIgnore]
        public bool InterestedInWomen
        {
            get
            {
                return interestedInWomen;
            }
            set
            {
                SetPropertyByRef(new { interestedInWomen }, value);
            }
        }

        [JsonIgnore]
        public string Sexuality
        {
            get
            {
                switch (SexualityRaw)
                {
                    case Internals.Sexuality.Unsure:
                        return core.Prose.GetString("NOT_SURE");
                    case Internals.Sexuality.Asexual:
                        return core.Prose.GetString("ASEXUAL");
                    case Internals.Sexuality.Hetrosexual:
                        return core.Prose.GetString("STRAIGHT");
                    case Internals.Sexuality.Homosexual:
                        if (GenderRaw == Internals.Gender.Female)
                        {
                            return core.Prose.GetString("LESBIAN");
                        }
                        else
                        {
                            return core.Prose.GetString("GAY");
                        }
                    case Internals.Sexuality.Bisexual:
                        return core.Prose.GetString("BISEXUAL");
                    case Internals.Sexuality.Pansexual:
                        return core.Prose.GetString("PANSEXUAL");
                    case Internals.Sexuality.Polysexual:
                        return core.Prose.GetString("POLYSEXUAL");
                    default:
                        return "FALSE";
                }
            }
        }

        [JsonIgnore]
        public Sexuality SexualityRaw
        {
            get
            {
                return (Sexuality)sexuality;
            }
            set
            {
                SetPropertyByRef(new { sexuality }, (byte)value);
            }
        }

        [JsonIgnore]
        public string MaritialStatus
        {
            get
            {
                switch (MaritialStatusRaw)
                {
                    case Internals.MaritialStatus.Single:
                        return core.Prose.GetString("SINGLE");
                    case Internals.MaritialStatus.MonogomousRelationship:
                        if (MaritialWithConfirmed && MaritialWithId > 0)
                        {
                            return string.Format(core.Prose.GetString("IN_A_RELATIONSHIP_WITH"), MaritialWithId.ToString());
                        }
                        else
                        {
                            return core.Prose.GetString("IN_A_RELATIONSHIP");
                        }
                    case Internals.MaritialStatus.OpenRelationship:
                        if (MaritialWithConfirmed && MaritialWithId > 0)
                        {
                            return string.Format(core.Prose.GetString("IN_A_OPEN_RELATIONSHIP_WITH"), MaritialWithId.ToString());
                        }
                        else
                        {
                            return core.Prose.GetString("IN_A_OPEN_RELATIONSHIP");
                        }
                    case Internals.MaritialStatus.Engaged:
                        if (MaritialWithConfirmed && MaritialWithId > 0)
                        {
                            return string.Format(core.Prose.GetString("ENGAGED_TO"), MaritialWithId.ToString());
                        }
                        else
                        {
                            return core.Prose.GetString("ENGAGED");
                        }
                    case Internals.MaritialStatus.Married:
                        if (MaritialWithConfirmed && MaritialWithId > 0)
                        {
                            return string.Format(core.Prose.GetString("MARRIED_TO"), MaritialWithId.ToString());
                        }
                        else
                        {
                            return core.Prose.GetString("MARRIED");
                        }
                    case Internals.MaritialStatus.Separated:
                        return core.Prose.GetString("SEPARATED");
                    case Internals.MaritialStatus.Divorced:
                        return core.Prose.GetString("DIVORCED");
                    case Internals.MaritialStatus.Widowed:
                        return core.Prose.GetString("WIDOWED");
                    default:
                        return "FALSE";
                }
            }
        }

        [JsonIgnore]
        public long MaritialWithId
        {
            get
            {
                return maritialWith;
            }
            set
            {
                SetProperty("maritialWith", value);
            }
        }

        [JsonIgnore]
        public bool MaritialWithConfirmed
        {
            get
            {
                return maritialWithConfirmed;
            }
            set
            {
                SetProperty("maritialWithConfirmed", value);
            }
        }

        [JsonIgnore]
        public MaritialStatus MaritialStatusRaw
        {
            get
            {
                return (MaritialStatus)maritialStatus;
            }
            set
            {
                SetPropertyByRef(new { maritialStatus }, (byte)value);
            }
        }

        [JsonProperty("description")]
        public string Autobiography
        {
            get
            {
                return autobiography;
            }
            set
            {
                SetProperty("autobiography", value);
            }
        }

        /// <summary>
        /// User's height in cm.
        /// </summary>
        [JsonIgnore]
        public byte Height
        {
            get
            {
                return height;
            }
            set
            {
                SetProperty("height", value);
            }
        }

        [JsonIgnore]
        public uint Weight
        {
            get
            {
                return weight;
            }
            set
            {
                SetProperty("weight", value);
            }
        }

        [JsonIgnore]
        public long ProfileViews
        {
            get
            {
                return profileViews;
            }
        }

        [JsonIgnore]
        public long ProfileComments
        {
            get
            {
                return profileComments;
            }
        }

        [JsonIgnore]
        public int Age
        {
            get
            {
                DateTime dateOfBirth = DateOfBirth;
                if (dateOfBirth.Year == 1000) return 0;
                if (DateTime.UtcNow.DayOfYear < dateOfBirth.DayOfYear)
                {
                    return (int)(DateTime.UtcNow.Year - dateOfBirth.Year - 1);
                }
                else
                {
                    return (int)(DateTime.UtcNow.Year - dateOfBirth.Year);
                }
            }
        }

        [JsonIgnore]
        public string AgeString
        {
            get
            {
                string age;
                int ageInt = Age;
                if (ageInt == 0)
                {
                    age = "FALSE";
                }
                else
                {
                    if (ageInt == 1)
                    {
                        age = ageInt.ToString() + " year old";
                    }
                    else
                    {
                        age = ageInt.ToString() + " years old";
                    }
                }

                return age;
            }
        }

        [JsonIgnore]
        public string Title
        {
            get
            {
                return nameTitle;
            }
            set
            {
                SetProperty("nameTitle", value);
            }
        }

        [JsonIgnore]
        public string FirstName
        {
            get
            {
                return nameFirst;
            }
            set
            {
                SetProperty("nameFirst", value);
            }
        }

        [JsonIgnore]
        public string MiddleName
        {
            get
            {
                return nameMiddle;
            }
            set
            {
                SetPropertyByRef(new { nameMiddle }, value);
            }
        }

        [JsonIgnore]
        public string LastName
        {
            get
            {
                return nameLast;
            }
            set
            {
                SetPropertyByRef(new { nameLast }, value);
            }
        }

        [JsonIgnore]
        public string Suffix
        {
            get
            {
                return nameSuffix;
            }
            set
            {
                SetProperty("nameSuffix", value);
            }
        }

        [JsonIgnore]
        private long Comments
        {
            get
            {
                return profileComments;
            }
        }

        [JsonIgnore]
        public string Religion
        {
            get
            {
                if (!string.IsNullOrEmpty(religionTitle))
                {
                    return religionTitle;
                }
                else
                {
                    return "FALSE";
                }
            }
        }

        [JsonIgnore]
        public short ReligionId
        {
            get
            {
                return religionId;
            }
            set
            {
                SetProperty("religionId", value);
            }
        }

        [JsonIgnore]
        public string Country
        {
            get
            {
                if (!string.IsNullOrEmpty(countryName))
                {
                    return countryName;
                }
                else
                {
                    return "FALSE";
                }
            }
        }

        [JsonIgnore]
        public string CountryIso
        {
            get
            {
                return country;
            }
            set
            {
                SetProperty("country", value);
            }
        }

        [JsonIgnore]
        public string AddressLine1
        {
            get
            {
                return addressLine1;
            }
            set
            {
                SetProperty("addressLine1", value);
            }
        }

        [JsonIgnore]
        public string AddressLine2
        {
            get
            {
                return addressLine2;
            }
            set
            {
                SetProperty("addressLine2", value);
            }
        }

        [JsonIgnore]
        public string AddressTown
        {
            get
            {
                return addressTown;
            }
            set
            {
                SetProperty("addressTown", value);
            }
        }

        [JsonIgnore]
        public string AddressState
        {
            get
            {
                return addressState;
            }
            set
            {
                SetProperty("addressState", value);
            }
        }

        [JsonIgnore]
        public string AddressPostCode
        {
            get
            {
                return addressPostCode;
            }
            set
            {
                SetProperty("addressPostCode", value);
            }
        }

        [JsonIgnore]
        public DateTime DateOfBirth
        {
            get
            {
                if (core.Tz == null)
                {
                    if (user.UserInfo != null)
                    {
                        return user.UserInfo.GetTimeZone.DateTimeFromMysql(dateofBirthRaw);
                    }
                    else
                    {
                        UnixTime tz = new UnixTime(core, 0);
                        return tz.DateTimeFromMysql(dateofBirthRaw);
                    }
                }
                else
                {
                    return core.Tz.DateTimeFromMysql(dateofBirthRaw);
                }
            }
            set
            {
                SetProperty("dateofBirthRaw", UnixTime.UnixTimeStamp(value));
                SetProperty("dateofBirthMonthRaw", (byte)value.Month);
                SetProperty("dateofBirthDayRaw", (byte)value.Day);
            }
        }

        internal UserProfile(Core core, User user)
            : base(core)
        {
            this.user = user;
            ItemLoad += new ItemLoadHandler(UserProfile_ItemLoad);
            ItemUpdated += new EventHandler(UserProfile_ItemUpdated);

            try
            {
                LoadItem("user_id", user.UserId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidUserException();
            }
        }

        public UserProfile(Core core, User user, DataRow memberRow)
            : base(core)
        {
            this.user = user;

            ItemLoad += new ItemLoadHandler(UserProfile_ItemLoad);
            ItemUpdated += new EventHandler(UserProfile_ItemUpdated);

            try
            {
                loadItemInfo(memberRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidUserException();
            }
        }

        public UserProfile(Core core, User user, System.Data.Common.DbDataReader memberRow)
            : base(core)
        {
            this.user = user;

            ItemLoad += new ItemLoadHandler(UserProfile_ItemLoad);
            ItemUpdated += new EventHandler(UserProfile_ItemUpdated);

            try
            {
                loadItemInfo(memberRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidUserException();
            }
        }

        public UserProfile(Core core, DataRow memberRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserProfile_ItemLoad);
            ItemUpdated += new EventHandler(UserProfile_ItemUpdated);

            try
            {
                loadItemInfo(memberRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidUserException();
            }
        }

        public UserProfile(Core core, System.Data.Common.DbDataReader memberRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserProfile_ItemLoad);
            ItemUpdated += new EventHandler(UserProfile_ItemUpdated);

            try
            {
                loadItemInfo(memberRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidUserException();
            }
        }

        public UserProfile(Core core, User user, DataRow memberRow, UserLoadOptions loadOptions)
            : this(core, user, memberRow)
        {
        }

        public UserProfile(Core core, User user, System.Data.Common.DbDataReader memberRow, UserLoadOptions loadOptions)
            : this(core, user, memberRow)
        {
        }

        private new void loadItemInfo(Type type, DataRow userRow)
        {
            if (type == typeof(UserProfile))
            {
                loadUserProfile(userRow);
            }
            else
            {
                base.loadItemInfo(type, userRow);
            }
        }

        protected override void loadItemInfo(DataRow userRow)
        {
            loadUserProfile(userRow);
        }

        private new void loadItemInfo(Type type, System.Data.Common.DbDataReader userRow)
        {
            if (type == typeof(UserProfile))
            {
                loadUserProfile(userRow);
            }
            else
            {
                base.loadItemInfo(type, userRow);
            }
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader userRow)
        {
            loadUserProfile(userRow);
        }

        protected void loadUserProfile(DataRow userRow)
        {
            loadValue(userRow, "user_id", out userId);
            loadValue(userRow, "profile_autobiography", out autobiography);
            loadValue(userRow, "profile_curriculum_vitae", out curriculumVitae);
            loadValue(userRow, "profile_gender", out gender);
            loadValue(userRow, "profile_interested_in_men", out interestedInMen);
            loadValue(userRow, "profile_interested_in_women", out interestedInWomen);
            loadValue(userRow, "profile_sexuality", out sexuality);
            loadValue(userRow, "profile_maritial_status", out maritialStatus);
            loadValue(userRow, "profile_maritial_with", out maritialWith);
            loadValue(userRow, "profile_maritial_with_confirmed", out maritialWithConfirmed);
            loadValue(userRow, "profile_name_title", out nameTitle);
            loadValue(userRow, "profile_name_first", out nameFirst);
            loadValue(userRow, "profile_name_middle", out nameMiddle);
            loadValue(userRow, "profile_name_last", out nameLast);
            loadValue(userRow, "profile_name_suffix", out nameSuffix);
            loadValue(userRow, "profile_hometown", out hometown);
            loadValue(userRow, "profile_views", out profileViews);
            loadValue(userRow, "profile_height", out height);
            loadValue(userRow, "profile_weight", out weight);
            loadValue(userRow, "profile_religion", out religionId);
            loadValue(userRow, "profile_comments", out profileComments);
            loadValue(userRow, "profile_date_of_birth_ut", out dateofBirthRaw);
            loadValue(userRow, "profile_date_of_birth_month_cache", out dateofBirthMonthRaw);
            loadValue(userRow, "profile_date_of_birth_day_cache", out dateofBirthDayRaw);
            loadValue(userRow, "profile_country", out country);
            loadValue(userRow, "profile_address_line_1", out addressLine1);
            loadValue(userRow, "profile_address_line_2", out addressLine2);
            loadValue(userRow, "profile_address_town", out addressTown);
            loadValue(userRow, "profile_address_state", out addressState);
            loadValue(userRow, "profile_address_post_code", out addressPostCode);

            itemLoaded(userRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected void loadUserProfile(System.Data.Common.DbDataReader userRow)
        {
            loadValue(userRow, "user_id", out userId);
            loadValue(userRow, "profile_autobiography", out autobiography);
            loadValue(userRow, "profile_curriculum_vitae", out curriculumVitae);
            loadValue(userRow, "profile_gender", out gender);
            loadValue(userRow, "profile_interested_in_men", out interestedInMen);
            loadValue(userRow, "profile_interested_in_women", out interestedInWomen);
            loadValue(userRow, "profile_sexuality", out sexuality);
            loadValue(userRow, "profile_maritial_status", out maritialStatus);
            loadValue(userRow, "profile_maritial_with", out maritialWith);
            loadValue(userRow, "profile_maritial_with_confirmed", out maritialWithConfirmed);
            loadValue(userRow, "profile_name_title", out nameTitle);
            loadValue(userRow, "profile_name_first", out nameFirst);
            loadValue(userRow, "profile_name_middle", out nameMiddle);
            loadValue(userRow, "profile_name_last", out nameLast);
            loadValue(userRow, "profile_name_suffix", out nameSuffix);
            loadValue(userRow, "profile_hometown", out hometown);
            loadValue(userRow, "profile_views", out profileViews);
            loadValue(userRow, "profile_height", out height);
            loadValue(userRow, "profile_weight", out weight);
            loadValue(userRow, "profile_religion", out religionId);
            loadValue(userRow, "profile_comments", out profileComments);
            loadValue(userRow, "profile_date_of_birth_ut", out dateofBirthRaw);
            loadValue(userRow, "profile_date_of_birth_month_cache", out dateofBirthMonthRaw);
            loadValue(userRow, "profile_date_of_birth_day_cache", out dateofBirthDayRaw);
            loadValue(userRow, "profile_country", out country);
            loadValue(userRow, "profile_address_line_1", out addressLine1);
            loadValue(userRow, "profile_address_line_2", out addressLine2);
            loadValue(userRow, "profile_address_town", out addressTown);
            loadValue(userRow, "profile_address_state", out addressState);
            loadValue(userRow, "profile_address_post_code", out addressPostCode);

            itemLoaded(userRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        void UserProfile_ItemLoad()
        {
            if (!string.IsNullOrEmpty(CountryIso))
            {
                Country c = new Country(core, CountryIso);
                countryName = c.Name;
            }

            if (ReligionId > 0)
            {
                Religion r = new Religion(core, ReligionId);
                religionTitle = r.Title;
            }
        }

        void UserProfile_ItemUpdated(object sender, EventArgs e)
        {
            core.Search.UpdateIndex(User);

            ApplicationEntry ae = core.GetApplication("Profile");

            if (HasPropertyUpdated("sexuality"))
            {
                if (!string.IsNullOrEmpty(Sexuality) && Sexuality != "FALSE")
                {
                    //ae.PublishToFeed(core.Session.LoggedInMember, core.Session.LoggedInMember.ItemKey, "changed " + core.Session.LoggedInMember.Preposition + " sexuality to " + Sexuality);
                }
            }

            if (HasPropertyUpdated("religionId"))
            {
                // TODO: religion
                /*if (GenderRaw == "MALE")
                {
                    ae.PublishToFeed(core.session.LoggedInMember, "changed his religion to " + Religion);
                }
                else if (GenderRaw == "FEMALE")
                {
                    ae.PublishToFeed(core.session.LoggedInMember, "changed her religion to " + Religion);
                }*/
            }

            if (HasPropertyUpdated("maritialStatus"))
            {
                if (maritialWith == 0)
                {
                    switch (MaritialStatusRaw)
                    {
                        case Internals.MaritialStatus.Undefined:
                            // Ignore if empty or null
                            break;
                        default:
                            //ae.PublishToFeed(core.Session.LoggedInMember, core.Session.LoggedInMember.ItemKey, "changed " + core.Session.LoggedInMember.Preposition + " relationship status to " + MaritialStatus.ToLower());
                            break;
                    }
                }
            }

            if (HasPropertyUpdated("maritialWithConfirmed"))
            {
                switch (MaritialStatusRaw)
                {
                    case  Internals.MaritialStatus.Undefined:
                        // Ignore if empty or null
                        break;
                    default:
                        if (maritialWith > 0)
                        {
                            core.LoadUserProfile(maritialWith);
                            ApplicationEntry aem = core.GetApplication("Profile");
                            switch (MaritialStatusRaw)
                            {
                                case Internals.MaritialStatus.MonogomousRelationship:
                                    //ae.PublishToFeed(core.Session.LoggedInMember, core.Session.LoggedInMember.ItemKey, "[user]" + core.LoggedInMemberId.ToString() + "[/user] is now in a relationship with [user]" + core.PrimitiveCache[maritialWith].Id + "[/user]");
                                    //aem.PublishToFeed(core.PrimitiveCache[maritialWith], core.Session.LoggedInMember.ItemKey, "[user]" + maritialWith.ToString() + "[/user] is now in a relationship with [user]" + core.Session.LoggedInMember.Id + "[/user]");
                                    break;
                                case Internals.MaritialStatus.OpenRelationship:
                                    break;
                                case Internals.MaritialStatus.Married:
                                    //ae.PublishToFeed(core.Session.LoggedInMember, core.Session.LoggedInMember.ItemKey, "[user]" + core.LoggedInMemberId.ToString() + "[/user] is now married to [user]" + core.PrimitiveCache[maritialWith].Id + "[/user]");
                                    //aem.PublishToFeed(core.PrimitiveCache[maritialWith], core.Session.LoggedInMember.ItemKey, "[user]" + maritialWith.ToString() + "[/user] is now married to [user]" + core.Session.LoggedInMember.Id + "[/user]");
                                    break;
                            }
                            break;
                        }
                        break;
                }
            }
        }

        [JsonIgnore]
        public override long Id
        {
            get
            {
                return userId;
            }
        }

        [JsonIgnore]
        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
