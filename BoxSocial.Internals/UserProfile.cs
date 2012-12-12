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
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("user_profile", "USER")]
    public sealed class UserProfile : NumberedItem
    {
        private User user;

        [DataField("user_id", DataFieldKeys.Unique)]
        private long userId;
        [DataField("profile_autobiography", MYSQL_TEXT)]
        private string autobiography;
        [DataField("profile_curriculum_vitae", MYSQL_TEXT)]
        private string curriculumVitae;
        [DataField("profile_gender", 15)]
        private string gender;
        [DataField("profile_sexuality", 15)]
        private string sexuality;
        [DataField("profile_maritial_status", 15)]
        private string maritialStatus;
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

        public long UserId
        {
            get
            {
                return userId;
            }
        }

        public string Gender
        {
            get
            {
                switch (gender)
                {
                    case "MALE":
                        return "Male";
                    case "FEMALE":
                        return "Female";
                    default:
                        return "FALSE";
                }
            }
        }

        public string GenderRaw
        {
            get
            {
                return gender;
            }
            set
            {
                SetProperty("gender", value);
            }
        }

        public string Sexuality
        {
            get
            {
                switch (sexuality)
                {
                    case "UNSURE":
                        return "Not Sure";
                    case "STRAIGHT":
                        return "Straight";
                    case "HOMOSEXUAL":
                        if (gender == "FEMALE")
                        {
                            return "Lesbian";
                        }
                        else
                        {
                            return "Gay";
                        }
                    case "BISEXUAL":
                        return "Bisexual";
                    case "TRANSEXUAL":
                        return "Transexual";
                    default:
                        return "FALSE";
                }
            }
        }

        public string SexualityRaw
        {
            get
            {
                return sexuality;
            }
            set
            {
                SetProperty("sexuality", value);
            }
        }

        public string MaritialStatus
        {
            get
            {
                switch (maritialStatus)
                {
                    case "SINGLE":
                        return "Single";
                    case "RELATIONSHIP":
                        if (MaritialWithConfirmed && MaritialWithId > 0)
                        {
                            return "In a Relationship with [user]" + MaritialWithId.ToString() + "[/user]";
                        }
                        else
                        {
                            return "In a Relationship";
                        }
                    case "MARRIED":
                        return "Married";
                    case "SWINGER":
                        return "Swinger";
                    case "DIVORCED":
                        return "Divorced";
                    case "WIDOWED":
                        return "Widowed";
                    default:
                        return "FALSE";
                }
            }
        }

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

        public string MaritialStatusRaw
        {
            get
            {
                return maritialStatus;
            }
            set
            {
                SetProperty("maritialStatus", value);
            }
        }

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

        public long ProfileViews
        {
            get
            {
                return profileViews;
            }
        }

        public long ProfileComments
        {
            get
            {
                return profileComments;
            }
        }


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

        public long Comments
        {
            get
            {
                return profileComments;
            }
        }
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

        public DateTime DateOfBirth
        {
            get
            {
                if (core.Tz == null)
                {
                    if (user.Info != null)
                    {
                        return user.Info.GetTimeZone.DateTimeFromMysql(dateofBirthRaw);
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

        internal UserProfile(Core core, User user, DataRow memberRow)
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

        internal UserProfile(Core core, User user, DataRow memberRow, UserLoadOptions loadOptions)
            : this(core, user, memberRow)
        {
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
            ApplicationEntry ae = new ApplicationEntry(core, core.Session.LoggedInMember, "Profile");

            if (HasPropertyUpdated("sexuality"))
            {
                if (!string.IsNullOrEmpty(Sexuality) && Sexuality != "FALSE")
                {
                    ae.PublishToFeed(core.Session.LoggedInMember, core.Session.LoggedInMember.ItemKey, "changed " + core.Session.LoggedInMember.Preposition + " sexuality to " + Sexuality);
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
                    switch (maritialStatus.ToUpper())
                    {
                        case null:
                        case "":
                        case "FALSE":
                        case "UNDEF":
                            // Ignore if empty or null
                            break;
                        default:
                            ae.PublishToFeed(core.Session.LoggedInMember, core.Session.LoggedInMember.ItemKey, "changed " + core.Session.LoggedInMember.Preposition + " relationship status to " + MaritialStatus.ToLower());
                            break;
                    }
                }
            }

            if (HasPropertyUpdated("maritialWithConfirmed"))
            {
                switch (maritialStatus.ToUpper())
                {
                    case null:
                    case "":
                    case "FALSE":
                    case "UNDEF":
                        // Ignore if empty or null
                        break;
                    default:
                        if (maritialWith > 0)
                        {
                            core.LoadUserProfile(maritialWith);
                            ApplicationEntry aem = new ApplicationEntry(core, core.PrimitiveCache[maritialWith], "Profile");
                            switch (maritialStatus)
                            {
                                case "RELATIONSHIP":
                                    ae.PublishToFeed(core.Session.LoggedInMember, core.Session.LoggedInMember.ItemKey, "[user]" + core.LoggedInMemberId.ToString() + "[/user] is now in a relationship with [user]" + core.PrimitiveCache[maritialWith].Id + "[/user]");
                                    aem.PublishToFeed(core.PrimitiveCache[maritialWith], core.Session.LoggedInMember.ItemKey, "[user]" + maritialWith.ToString() + "[/user] is now in a relationship with [user]" + core.Session.LoggedInMember.Id + "[/user]");
                                    break;
                                case "MARRIED":
                                    ae.PublishToFeed(core.Session.LoggedInMember, core.Session.LoggedInMember.ItemKey, "[user]" + core.LoggedInMemberId.ToString() + "[/user] is now married to [user]" + core.PrimitiveCache[maritialWith].Id + "[/user]");
                                    aem.PublishToFeed(core.PrimitiveCache[maritialWith], core.Session.LoggedInMember.ItemKey, "[user]" + maritialWith.ToString() + "[/user] is now married to [user]" + core.Session.LoggedInMember.Id + "[/user]");
                                    break;
                            }
                            break;
                        }
                        break;
                }
            }
        }

        public override long Id
        {
            get
            {
                return userId;
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
