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
    [DataTable("user_profile")]
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
        [DataField("profile_country", 2)]
        private string country;
        [DataField("profile_access")]
        private ushort permissions;
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

        private string countryName;
        private string religionTitle;

        private Access profileAccess;

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
                            return "In a Relationship with [user]" + MaritialWithId + "[/user]";
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
                SetProperty("nameMiddle", value);
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
                SetProperty("nameLast", value);
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

        public ushort Permissions
        {
            get
            {
                return permissions;
            }
        }

        public Access ProfileAccess
        {
            get
            {
                return profileAccess;
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

        public DateTime DateOfBirth
        {
            get
            {
                if (core.tz == null)
                {
                    if (user.Info != null)
                    {
                        return user.Info.GetTimeZone.DateTimeFromMysql(dateofBirthRaw);
                    }
                    else
                    {
                        UnixTime tz = new UnixTime(0);
                        return tz.DateTimeFromMysql(dateofBirthRaw);
                    }
                }
                else
                {
                    return core.tz.DateTimeFromMysql(dateofBirthRaw);
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
            profileAccess = new Access(core, permissions, user);

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
            ApplicationEntry ae = new ApplicationEntry(core, core.session.LoggedInMember, "Profile");

            if (HasPropertyUpdated("sexuality"))
            {
                if (!string.IsNullOrEmpty(Sexuality) && Sexuality != "FALSE")
                {
                    if (GenderRaw == "MALE")
                    {
                        ae.PublishToFeed(core.session.LoggedInMember, "changed his sexuality to " + Sexuality);
                    }
                    else if (GenderRaw == "FEMALE")
                    {
                        ae.PublishToFeed(core.session.LoggedInMember, "changed her sexuality to " + Sexuality);
                    }
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
                            if (GenderRaw == "MALE")
                            {
                                ae.PublishToFeed(core.session.LoggedInMember, "changed his relationship status to " + MaritialStatus.ToLower());
                            }
                            else if (GenderRaw == "FEMALE")
                            {
                                ae.PublishToFeed(core.session.LoggedInMember, "changed her relationship status to " + MaritialStatus.ToLower());
                            }
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
                            ApplicationEntry aem = new ApplicationEntry(core, core.UserProfiles[maritialWith], "Profile");
                            switch (maritialStatus)
                            {
                                case "RELATIONSHIP":
                                    ae.PublishToFeed(core.session.LoggedInMember, "[user]" + core.LoggedInMemberId.ToString() + "[/user] is now in a relationship with [user]" + core.UserProfiles[maritialWith].Id + "[/user]");
                                    aem.PublishToFeed(core.UserProfiles[maritialWith], "[user]" + maritialWith.ToString() + "[/user] is now in a relationship with [user]" + core.session.LoggedInMember.Id + "[/user]");
                                    break;
                                case "MARRIED":
                                    ae.PublishToFeed(core.session.LoggedInMember, "[user]" + core.LoggedInMemberId.ToString() + "[/user] is now married to [user]" + core.UserProfiles[maritialWith].Id + "[/user]");
                                    aem.PublishToFeed(core.UserProfiles[maritialWith], "[user]" + maritialWith.ToString() + "[/user] is now married to [user]" + core.session.LoggedInMember.Id + "[/user]");
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

        public override string Namespace
        {
            get
            {
                return "USER";
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
