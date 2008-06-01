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
    public sealed class UserProfile : Item
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

        private string countryName;

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
                        return "In a Relationship";
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

        public string MaritialStatusRaw
        {
            get
            {
                return maritialStatus;
            }
        }

        public string Autobiography
        {
            get
            {
                return autobiography;
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
        }

        public string FirstName
        {
            get
            {
                return nameFirst;
            }
        }

        public string MiddleName
        {
            get
            {
                return nameMiddle;
            }
        }

        public string LastName
        {
            get
            {
                return nameLast;
            }
        }

        public string Suffix
        {
            get
            {
                return nameSuffix;
            }
        }

        public long Comments
        {
            get
            {
                return profileComments;
            }
        }

        public short ReligionId
        {
            get
            {
                return religionId;
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
                if (countryName != "")
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
        }

        internal UserProfile(Core core, User user)
            : base(core)
        {
            this.user = user;
            ItemLoad += new ItemLoadHandler(UserProfile_ItemLoad);

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
            profileAccess = new Access(db, permissions, user);
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
