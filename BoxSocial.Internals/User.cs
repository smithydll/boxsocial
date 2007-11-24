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
using System.ComponentModel;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using BoxSocial.IO;

namespace BoxSocial.Internals
{

    public enum Relation : byte
    {
        None = 0x00,
        Owner = 0x01,
        Friend = 0x02,
        Family = 0x04,
        Blocked = 0x08,
    }

    public class Member : Primitive
    {
        public const string USER_INFO_FIELDS = "ui.user_id, ui.user_name, ui.user_time_zone, ui.user_friends, ui.user_show_custom_styles, ui.user_show_bbcode, ui.user_reg_date_ut, ui.user_last_visit_ut, ui.user_alternate_email, ui.user_active, ui.user_activate_code, ui.user_name_display, ui.user_live_messenger, ui.user_yahoo_messenger, ui.user_jabber_address, ui.user_home_page, ui.user_blog_subscriptions, ui.user_email_notifications, ui.user_bytes";
        public const string USER_PROFILE_FIELDS = "up.profile_comments, up.profile_country, c.country_name, up.profile_religion, up.profile_name_title, up.profile_name_suffix, up.profile_name_first, up.profile_name_middle, up.profile_name_last, up.profile_access, up.profile_views, up.profile_date_of_birth+0, up.profile_maritial_status, up.profile_autobiography, up.profile_sexuality, up.profile_gender, up.profile_date_of_birth_ut";
        public const string USER_ICON_FIELDS = "gi.gallery_item_parent_path, gi.gallery_item_uri";

        public const int HOMEPAGE_PROFILE = -1;
        public const int HOMEPAGE_BLOG = -2;

        public static long lastEmailId;

        protected Mysql db;
        protected long userId;
        private string userName;
        private string userNameOwnership;
        private string displayName;
        private string sexuality;
        private string gender;
        private string maritialStatus;
        private string autobiography;
        private DateTime dateOfBirth;
        private DateTime registrationDate;
        private DateTime lastOnlineTime;
        private uint profileViews;
        private ulong profileComments;
        private uint blogSubscriptions;
        private string country;
        private string countryIso;
        private ushort permissions;
        private Access profileAccess;
        private string userIconUri;
        private string firstName;
        private string lastName;
        private string middleName;
        private string suffix;
        private string title;
        private short religion;
        private byte showBbcode;
        private bool showCustomStyles;
        private long profileHomepage = -1;
        private int friends;
        private string alternateEmail;
        private bool emailNotifications;
        private ulong bytesUsed;
        private ushort timeZoneCode;
        private TimeZone timeZone;

        private bool sessionRelationsSet = false;
        private Relation sessionRelations;

        /// <summary>
        /// user ID (read only)
        /// </summary>
        public long UserId
        {
            get
            {
                return userId;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override long Id
        {
            get
            {
                return UserId;
            }
        }

        public override string Type
        {
            get
            {
                return "USER";
            }
        }

        /// <summary>
        /// user name (read only)
        /// </summary>
        public string UserName
        {
            get
            {
                return userName;
            }
        }

        public override string Key
        {
            get
            {
                return userName;
            }
        }

        public string UserNameOwnership
        {
            get
            {
                if (userNameOwnership == null)
                {
                    userNameOwnership = (displayName != "") ? displayName : userName;

                    if (userNameOwnership.EndsWith("s"))
                    {
                        userNameOwnership = userNameOwnership + "'";
                    }
                    else
                    {
                        userNameOwnership = userNameOwnership + "'s";
                    }
                }
                return userNameOwnership;
            }
        }

        public string DisplayName
        {
            get
            {
                if (displayName == "")
                {
                    return userName;
                }
                return displayName;
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

        public int Age
        {
            get
            {
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

        public string GetAgeString()
        {
            string age;
            int ageInt = Age;
            if (ageInt == 0)
            {
                age = "FALSE";
            }
            else
            {
                age = ageInt.ToString() + " years old";
            }

            return age;
        }

        public DateTime DateOfBirth
        {
            get
            {
                return dateOfBirth;
            }
        }

        public DateTime RegistrationDate
        {
            get
            {
                return registrationDate;
            }
        }

        public DateTime LastOnlineTime
        {
            get
            {
                return lastOnlineTime;
            }
        }

        public uint ProfileViews
        {
            get
            {
                return profileViews;
            }
        }

        public ulong ProfileComments
        {
            get
            {
                return profileComments;
            }
        }

        public uint BlogSubscriptions
        {
            get
            {
                return blogSubscriptions;
            }
        }

        public string Country
        {
            get
            {
                if (country != "")
                {
                    return country;
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
                return countryIso;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Access ProfileAccess
        {
            get
            {
                return profileAccess;
            }
        }

        public string ProfileUri
        {
            get
            {
                return string.Format("/{0}/",
                    userName);
            }
        }

        public string UserThumbnail
        {
            get
            {
                if (userIconUri != null)
                {
                    return string.Format("/{0}/images/_thumb{1}",
                        userName, userIconUri);
                }
                else
                {
                    return "FALSE";
                }
            }
        }

        /// <summary>
        /// 100x100 display tile
        /// </summary>
        public string UserIcon
        {
            get
            {
                if (userIconUri != null)
                {
                    return string.Format("/{0}/images/_icon{1}",
                        userName, userIconUri);
                }
                else
                {
                    return "FALSE";
                }
            }
        }

        /// <summary>
        /// 50x50 display tile
        /// </summary>
        public string UserTile
        {
            get
            {
                if (userIconUri != null)
                {
                    return string.Format("/{0}/images/_tile{1}",
                        userName, userIconUri);
                }
                else
                {
                    return "FALSE";
                }
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
        }

        public string FirstName
        {
            get
            {
                return firstName;
            }
        }

        public string MiddleName
        {
            get
            {
                return middleName;
            }
        }

        public string LastName
        {
            get
            {
                return lastName;
            }
        }

        public string Suffix
        {
            get
            {
                return suffix;
            }
        }

        public short ReligionRaw
        {
            get
            {
                return religion;
            }
        }

        public ushort Permissions
        {
            get
            {
                return permissions;
            }
        }

        public bool ShowCustomStyles
        {
            get
            {
                return showCustomStyles;
            }
        }

        public bool BbcodeShowImages
        {
            get
            {
                return (showBbcode & (1 << 0)) > 0;
            }
        }

        public bool BbcodeShowFlash
        {
            get
            {
                return (showBbcode & (1 << 1)) > 0;
            }
        }

        public bool BbcodeShowVideos
        {
            get
            {
                return (showBbcode & (1 << 2)) > 0;
            }
        }

        public List<BbcodeOptions> GetUserBbcodeOptions
        {
            get
            {
                List<BbcodeOptions> bbcodeOptions = new List<BbcodeOptions>();
                if (!BbcodeShowImages)
                {
                    bbcodeOptions.Add(BbcodeOptions.DisableImages);
                }
                if (!BbcodeShowFlash)
                {
                    bbcodeOptions.Add(BbcodeOptions.DisableFlash);
                }
                if (!BbcodeShowVideos)
                {
                    bbcodeOptions.Add(BbcodeOptions.DisableVideo);
                }
                return bbcodeOptions;
            }
        }

        public long ProfileHomepage
        {
            get
            {
                return profileHomepage;
            }
            set
            {
                profileHomepage = value;
            }
        }

        public int Friends
        {
            get
            {
                return friends;
            }
        }

        public string AlternateEmail
        {
            get
            {
                return alternateEmail;
            }
        }

        public bool EmailNotifications
        {
            get
            {
                return emailNotifications;
            }
        }

        public ulong BytesUsed
        {
            get
            {
                return bytesUsed;
            }
        }

        public ushort TimeZoneCode
        {
            get
            {
                return timeZoneCode;
            }
        }

        public TimeZone GetTimeZone
        {
            get
            {
                return timeZone;
            }
        }

        protected Member()
        {
        }

        public Member(Mysql db, DataRow userRow, bool containsProfileInfo)
            : this(db, userRow, containsProfileInfo, false)
        {
        }

        public Member(Mysql db, DataRow userRow, bool containsProfileInfo, bool containsIcon)
        {
            this.db = db;
            loadUserInfo(userRow);

            if (containsProfileInfo)
            {
                loadUserProfile(userRow);
            }
            if (containsIcon)
            {
                loadUserIcon(userRow);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="db"></param>
        /// <param name="userId"></param>
        public Member(Mysql db, long userId)
            : this(db, userId, false)
        {
        }

        public Member(Mysql db, long userId, bool loadProfileInfo)
        {
            this.db = db;


            if (loadProfileInfo)
            {
                DataTable userTable = db.SelectQuery(string.Format("SELECT {1}, {2}, {3} FROM user_keys uk INNER JOIN user_info ui ON uk.user_id = ui.user_id INNER JOIN user_profile up ON uk.user_id = up.user_id LEFT JOIN countries c ON c.country_iso = up.profile_country LEFT JOIN gallery_items gi ON ui.user_icon = gi.gallery_item_id WHERE uk.user_id = {0}",
                    userId, USER_INFO_FIELDS, USER_PROFILE_FIELDS, USER_ICON_FIELDS));

                if (userTable.Rows.Count == 1)
                {
                    loadUserInfo(userTable.Rows[0]);
                    loadUserProfile(userTable.Rows[0]);
                    loadUserIcon(userTable.Rows[0]);
                }
                else
                {
                    throw new Exception("Invalid User Exception");
                }
            }
            else
            {
                DataTable userTable = db.SelectQuery(string.Format("SELECT {1}, {2} FROM user_keys uk INNER JOIN user_info ui ON uk.user_id = ui.user_id LEFT JOIN gallery_items gi ON ui.user_icon = gi.gallery_item_id WHERE uk.user_id = {0}",
                userId, USER_INFO_FIELDS, USER_ICON_FIELDS));

                if (userTable.Rows.Count == 1)
                {
                    loadUserInfo(userTable.Rows[0]);
                    loadUserIcon(userTable.Rows[0]);
                }
                else
                {
                    throw new Exception("Invalid User Exception");
                }
            }
        }

        public Member(Mysql db, string userName)
            : this(db, userName, true)
        {
        }

        public Member(Mysql db, string userName, bool loadIcon)
        {
            this.db = db;

            // TODO: filter username for bad characters, casing etc....

            DataTable userTable;
            if (loadIcon)
            {
                userTable = db.SelectQuery(string.Format("SELECT {1}, {2} FROM user_keys uk INNER JOIN user_info ui ON uk.user_id = ui.user_id LEFT JOIN gallery_items gi ON ui.user_icon = gi.gallery_item_id WHERE uk.user_name = '{0}'",
                    Mysql.Escape(userName), USER_INFO_FIELDS, USER_ICON_FIELDS));
            }
            else
            {
                userTable = db.SelectQuery(string.Format("SELECT {1} FROM user_keys uk INNER JOIN user_info ui ON uk.user_id = ui.user_id WHERE uk.user_name = '{0}'",
                    Mysql.Escape(userName), USER_INFO_FIELDS));
            }

            if (userTable.Rows.Count == 1)
            {
                loadUserInfo(userTable.Rows[0]);
                if (loadIcon)
                {
                    loadUserIcon(userTable.Rows[0]);
                }
            }
            else
            {
                throw new Exception("Invalid User Exception");
            }
        }

        public void LoadProfileInfo()
        {
            DataTable userTable = db.SelectQuery(string.Format("SELECT {1} FROM user_keys uk INNER JOIN user_profile up ON uk.user_id = up.user_id LEFT JOIN countries c ON c.country_iso = up.profile_country WHERE uk.user_id = {0}",
                userId, USER_PROFILE_FIELDS));

            if (userTable.Rows.Count == 1)
            {
                loadUserProfile(userTable.Rows[0]);
            }
            else
            {
                throw new Exception("Invalid User Exception");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userRow"></param>
        protected void loadUserInfo(DataRow userRow)
        {
            userId = (int)userRow["user_id"];
            userName = (string)userRow["user_name"];
            displayName = (string)userRow["user_name_display"];
            timeZoneCode = (ushort)userRow["user_time_zone"];
            timeZone = new TimeZone(timeZoneCode);
            registrationDate = timeZone.DateTimeFromMysql(userRow["user_reg_date_ut"]);
            lastOnlineTime = timeZone.DateTimeFromMysql(userRow["user_last_visit_ut"]);
            blogSubscriptions = (uint)userRow["user_blog_subscriptions"];
            showBbcode = (byte)userRow["user_show_bbcode"];
            showCustomStyles = ((byte)userRow["user_show_custom_styles"] > 0) ? true : false;
            profileHomepage = (long)userRow["user_home_page"];
            friends = (int)userRow["user_friends"];
            alternateEmail = (string)userRow["user_alternate_email"];
            emailNotifications = ((byte)userRow["user_email_notifications"] > 0) ? true : false;
            bytesUsed = (ulong)userRow["user_bytes"];
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userRow"></param>
        protected void loadUserProfile(DataRow userRow)
        {
            sexuality = (string)userRow["profile_sexuality"];
            gender = (string)userRow["profile_gender"];
            if (!(userRow["profile_autobiography"] is System.DBNull))
            {
                autobiography = (string)userRow["profile_autobiography"];
            }
            maritialStatus = (string)userRow["profile_maritial_status"];
            profileViews = (uint)userRow["profile_views"];
            profileComments = (ulong)userRow["profile_comments"];
            if (!(userRow["country_name"] is System.DBNull))
            {
                country = (string)userRow["country_name"];
            }

            firstName = (string)userRow["profile_name_first"];
            lastName = (string)userRow["profile_name_last"];
            middleName = (string)userRow["profile_name_middle"];
            suffix = (string)userRow["profile_name_suffix"];
            title = (string)userRow["profile_name_title"];
            religion = (short)userRow["profile_religion"];
            countryIso = (string)userRow["profile_country"];

            permissions = (ushort)userRow["profile_access"];
            profileAccess = new Access(db, (ushort)userRow["profile_access"], this);

            // use the user's timezone, after all it is _their_ birthday
            dateOfBirth = timeZone.DateTimeFromMysql(userRow["profile_date_of_birth_ut"]);
        }

        protected void loadUserIcon(DataRow userRow)
        {
            if (!(userRow["gallery_item_uri"] is DBNull))
            {
                userIconUri = string.Format("/{0}/{1}",
                    (string)userRow["gallery_item_parent_path"], (string)userRow["gallery_item_uri"]);
            }
        }

        protected void loadUserFromUser(Member member)
        {
            this.userId = member.userId;
            this.userName = member.userName;
            this.userNameOwnership = member.userNameOwnership;
            this.alternateEmail = member.alternateEmail;
            this.autobiography = member.autobiography;
            this.blogSubscriptions = member.blogSubscriptions;
            this.bytesUsed = member.bytesUsed;
            this.country = member.country;
            this.countryIso = member.countryIso;
            this.dateOfBirth = member.dateOfBirth;
            this.db = member.db;
            this.displayName = member.displayName;
            this.emailNotifications = member.emailNotifications;
            this.firstName = member.firstName;
            this.friends = member.friends;
            this.gender = member.gender;
            this.lastName = member.lastName;
            this.lastOnlineTime = member.lastOnlineTime;
            this.maritialStatus = member.maritialStatus;
            this.middleName = member.middleName;
            //this.networks = member.networks;
            this.permissions = member.permissions;
            this.profileAccess = member.profileAccess;
            this.profileComments = member.profileComments;
            this.profileHomepage = member.profileHomepage;
            this.profileViews = member.profileViews;
            this.registrationDate = member.registrationDate;
            this.religion = member.religion;
            this.sexuality = member.sexuality;
            this.showBbcode = member.showBbcode;
            this.showCustomStyles = member.showCustomStyles;
            this.suffix = member.suffix;
            this.timeZone = member.timeZone;
            this.timeZoneCode = member.timeZoneCode;
            this.title = member.title;
            this.userIconUri = member.userIconUri;
        }

        public string GetUserStyle()
        {
            DataTable userStyleTable = db.SelectQuery(string.Format("SELECT us.* FROM user_keys uk INNER JOIN user_style us ON uk.user_id = us.user_id WHERE uk.user_id = {0}",
                userId));

            if (userStyleTable.Rows.Count == 1)
            {
                return (string)userStyleTable.Rows[0]["style_css"];
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// return a maximum of the first 255
        /// </summary>
        /// <returns></returns>
        public List<Member> GetFriends()
        {
            return GetFriends(1, 255);
        }

        public List<Member> GetFriends(int page, int perPage)
        {
            List<Member> friends = new List<Member>();

            DataTable friendsTable = db.SelectQuery(string.Format("SELECT {1}, {2}, {3} FROM user_relations uf INNER JOIN user_info ui ON uf.relation_you = ui.user_id INNER JOIN user_profile up ON uf.relation_you = up.user_id LEFT JOIN (countries c, gallery_items gi) ON (c.country_iso = up.profile_country AND gi.gallery_item_id = ui.user_icon) WHERE uf.relation_me = {0} and uf.relation_type = 'FRIEND' ORDER BY (uf.relation_order - 1) LIMIT {4}, {5}",
                userId, Member.USER_PROFILE_FIELDS, Member.USER_INFO_FIELDS, Member.USER_ICON_FIELDS, (page - 1) * perPage, perPage));

            foreach (DataRow dr in friendsTable.Rows)
            {
                friends.Add(new Member(db, dr, true, true));
            }

            return friends;
        }

        public List<Member> GetFriendsOnline()
        {
            List<Member> friends = new List<Member>();

            // last 15 minutes
            DataTable friendsTable = db.SelectQuery(string.Format("SELECT {1}, {2}, {3} FROM user_relations uf INNER JOIN user_info ui ON uf.relation_you = ui.user_id INNER JOIN user_profile up ON uf.relation_you = up.user_id LEFT JOIN (countries c, gallery_items gi) ON (c.country_iso = up.profile_country AND gi.gallery_item_id = ui.user_icon) WHERE uf.relation_me = {0} and uf.relation_type = 'FRIEND' AND UNIX_TIMSTAMP() - ui.user_last_visit_ut < 900 ORDER BY (uf.relation_order - 1) LIMIT {4}, {5}",
                userId, Member.USER_PROFILE_FIELDS, Member.USER_INFO_FIELDS, Member.USER_ICON_FIELDS, 1, 14));

            foreach (DataRow dr in friendsTable.Rows)
            {
                friends.Add(new Member(db, dr, true, true));
            }

            return friends;
        }

        public Relation GetRelations(Member member)
        {
            Relation returnValue = Relation.None;

            if (member == null)
            {
                return Relation.None;
            }

            if (member.UserId == userId)
            {
                return Relation.Owner;
            }

            DataTable relationMe = db.SelectQuery(string.Format("SELECT relation_type, relation_order FROM user_relations WHERE relation_me = {0} AND relation_you = {1};",
                    userId, member.userId));

            for (int i = 0; i < relationMe.Rows.Count; i++)
            {
                if ((string)relationMe.Rows[i]["relation_type"] == "FRIEND")
                {
                    returnValue |= Relation.Friend;
                }

                if ((string)relationMe.Rows[i]["relation_type"] == "FAMILY")
                {
                    returnValue |= Relation.Family;
                }

                if ((string)relationMe.Rows[i]["relation_type"] == "BLOCKED")
                {
                    returnValue |= Relation.Blocked;
                }
            }

            return returnValue;
        }

        public bool IsFriend(Member member)
        {
            if ((GetRelations(member) & Relation.Friend) == Relation.Friend)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsFamily(Member member)
        {
            if ((GetRelations(member) & Relation.Family) == Relation.Family)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsBlocked(Member member)
        {
            if ((GetRelations(member) & Relation.Blocked) == Relation.Blocked)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // TODO: move to NetworkMember
        /*public List<Network> GetNetworks()
        {
            List<Network> networks = new List<Network>();

            DataTable networksTable = db.SelectQuery(string.Format("SELECT {1}, nk.network_network FROM network_members mm INNER JOIN network_keys nk ON nm.network_id = nk.network_id INNER JOIN network_info ni ON nk.network_id = ni.network_id WHERE nm.user_id = {0} ORDER BY nk.network_network ASC;",
                userId, Network.NETWORK_INFO_FIELDS));

            foreach (DataRow dr in networksTable.Rows)
            {
                networks.Add(new Network(db, dr));
            }

            return networks;
        }*/

        /// <summary>
        /// 
        /// </summary>
        /// <param name="db"></param>
        /// <param name="userName"></param>
        /// <param name="eMail"></param>
        /// <param name="password"></param>
        /// <param name="passwordConfirm"></param>
        /// <returns>Null if registration failed</returns>
        public static Member Register(Core core, string userName, string eMail, string password, string passwordConfirm)
        {
            Mysql db = core.db;
            SessionState session = core.session;

            string passwordClearText = password;

            if (!CheckUserNameUnique(db, userName))
            {
                return null;
            }

            password = VerifyPasswordMatch(password, passwordConfirm);

            if (password == "")
            {
                return null;
            }

            string activateKey = Member.GenerateActivationSecurityToken();

            long userId = db.UpdateQuery(string.Format("INSERT INTO user_keys (user_name) VALUES ('{0}')",
                Mysql.Escape(userName)), true);

            db.UpdateQuery(string.Format("INSERT INTO user_info (user_id, user_name, user_alternate_email, user_password, user_reg_date_ut, user_activate_code, user_reg_ip) VALUES ({0}, '{1}', '{2}', '{3}', UNIX_TIMESTAMP(), '{4}', '{5}');",
                userId, Mysql.Escape(userName), Mysql.Escape(eMail), password, Mysql.Escape(activateKey), Mysql.Escape(session.IPAddress.ToString())), true);

            db.UpdateQuery(string.Format("INSERT INTO user_profile (user_id) VALUES ({0});",
                userId), false);

            Member newUser = new Member(db, userId);
            string activateUri = string.Format("http://zinzam.com/register/?mode=activate&id={0}&key={1}",
                userId, activateKey);

            Template emailTemplate = new Template(HttpContext.Current.Server.MapPath("./templates/emails/"), "registration_welcome.eml");

            emailTemplate.ParseVariables("TO_NAME", userName);
            emailTemplate.ParseVariables("U_ACTIVATE", activateUri);
            emailTemplate.ParseVariables("USERNAME", userName);
            emailTemplate.ParseVariables("PASSWORD", passwordClearText);

            Email.SendEmail(core, eMail, "Welcome to ZinZam", emailTemplate.ToString());

            return newUser;
        }

        /// <summary>
        /// If passwords match, it hashes them and returns the hash, otherwise returns an empty string.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="passwordConfirm"></param>
        /// <returns></returns>
        public static string VerifyPasswordMatch(string password, string passwordConfirm)
        {
            if (password != passwordConfirm)
            {
                return "";
            }

            return HashPassword(password);
        }

        /// <summary>
        /// Returns true if passwords match.
        /// </summary>
        /// <param name="password">Raw password from input form.</param>
        /// <param name="passwordHash">Hashes password from the database.</param>
        /// <returns></returns>
        public static bool VerifyPasswordForLogin(string password, string passwordHash)
        {
            if (HashPassword(password) != passwordHash)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Hashes a password
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string HashPassword(string password)
        {
            HashAlgorithm hash = new SHA512Managed();

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] passwordHash = hash.ComputeHash(passwordBytes);

            password = "";
            foreach (byte passwordHashByte in passwordHash)
            {
                password += string.Format("{0:x2}", passwordHashByte);
            }

            return password;
        }

        public static bool CheckEmailValid(string eMail)
        {
            if (!Regex.IsMatch(eMail, @"^[a-z0-9&\'\.\-_\+]+@[a-z0-9\-]+\.([a-z0-9\-]+\.)*?[a-z]+$", RegexOptions.IgnoreCase))
            {
                return false;
            }
            return true;
        }

        public static bool CheckEmailUnique(Mysql db, string eMail)
        {
            // TODO: register all e-mail addresses into a new table, along with privacy controls
            DataTable userTable = db.SelectQuery(string.Format("SELECT user_id, user_alternate_email FROM user_info WHERE LCASE(user_alternate_email) = '{0}';",
                Mysql.Escape(eMail.ToLower())));
            if (userTable.Rows.Count > 0)
            {
                lastEmailId = (int)userTable.Rows[0]["user_id"];
                return false;
            }

            DataTable networkMemberTable = db.SelectQuery(string.Format("SELECT user_id, member_email FROM network_members WHERE LCASE(member_email) = '{0}';",
                Mysql.Escape(eMail.ToLower())));
            if (networkMemberTable.Rows.Count > 0)
            {
                lastEmailId = (int)networkMemberTable.Rows[0]["user_id"];
                return false;
            }

            return true;
        }

        public static bool CheckUserNameValid(string userName)
        {
            int matches = 0;

            List<string> disallowedNames = new List<string>();
            disallowedNames.Add("about");
            disallowedNames.Add("copyright");
            disallowedNames.Add("register");
            disallowedNames.Add("sign-in");
            disallowedNames.Add("log-in");
            disallowedNames.Add("help");
            disallowedNames.Add("safety");
            disallowedNames.Add("privacy");
            disallowedNames.Add("terms-of-service");
            disallowedNames.Add("site-map");
            disallowedNames.Add("zinzam");
            disallowedNames.Add("blogs");
            disallowedNames.Add("profiles");
            disallowedNames.Add("search");
            disallowedNames.Add("communities");
            disallowedNames.Add("community");
            disallowedNames.Add("constitution");
            disallowedNames.Add("profile");
            disallowedNames.Add("my-profile");
            disallowedNames.Add("history");
            disallowedNames.Add("get-active");
            disallowedNames.Add("statistics");
            disallowedNames.Add("blog");
            disallowedNames.Add("categories");
            disallowedNames.Add("members");
            disallowedNames.Add("users");
            disallowedNames.Add("upload");
            disallowedNames.Add("support");
            disallowedNames.Add("account");
            disallowedNames.Add("history");
            disallowedNames.Add("browse");
            disallowedNames.Add("feature");
            disallowedNames.Add("featured");
            disallowedNames.Add("favourites");
            disallowedNames.Add("dev");
            disallowedNames.Add("dcma");
            disallowedNames.Add("coppa");
            disallowedNames.Add("guidelines");
            disallowedNames.Add("press");
            disallowedNames.Add("jobs");
            disallowedNames.Add("careers");
            disallowedNames.Add("feedback");
            disallowedNames.Add("create");
            disallowedNames.Add("subscribe");
            disallowedNames.Add("subscriptions");
            disallowedNames.Add("rate");
            disallowedNames.Add("comment");
            disallowedNames.Add("mail");
            disallowedNames.Add("video");
            disallowedNames.Add("videos");
            disallowedNames.Add("music");
            disallowedNames.Add("podcast");
            disallowedNames.Add("podcasts");
            disallowedNames.Add("security");
            disallowedNames.Add("bugs");
            disallowedNames.Add("beta");
            disallowedNames.Add("friend");
            disallowedNames.Add("friends");
            disallowedNames.Add("family");
            disallowedNames.Add("promotion");
            disallowedNames.Add("birthday");
            disallowedNames.Add("account");
            disallowedNames.Add("settings");
            disallowedNames.Add("admin");
            disallowedNames.Add("administrator");
            disallowedNames.Add("administrators");
            disallowedNames.Add("root");
            disallowedNames.Add("my-account");
            disallowedNames.Add("member");
            disallowedNames.Add("anonymous");
            disallowedNames.Add("legal");
            disallowedNames.Add("contact");
            disallowedNames.Add("aonlinesite");
            disallowedNames.Add("images");
            disallowedNames.Add("image");
            disallowedNames.Add("styles");
            disallowedNames.Add("style");
            disallowedNames.Add("theme");
            disallowedNames.Add("header");
            disallowedNames.Add("footer");
            disallowedNames.Add("head");
            disallowedNames.Add("foot");
            disallowedNames.Add("bin");
            disallowedNames.Add("images");
            disallowedNames.Add("templates");
            disallowedNames.Add("cgi-bin");
            disallowedNames.Add("cgi");
            disallowedNames.Add("web.config");
            disallowedNames.Add("report");
            disallowedNames.Add("rules");
            disallowedNames.Add("script");
            disallowedNames.Add("scripts");
            disallowedNames.Add("css");
            disallowedNames.Add("img");
            disallowedNames.Add("App_Data");
            disallowedNames.Add("test");
            disallowedNames.Add("sitepreview");
            disallowedNames.Add("plesk-stat");
            disallowedNames.Add("jakarta");
            disallowedNames.Add("storage");
            disallowedNames.Add("netalert");
            disallowedNames.Add("group");
            disallowedNames.Add("groups");
            disallowedNames.Add("network");
            disallowedNames.Add("networks");
            disallowedNames.Add("open");
            disallowedNames.Add("opensource");
            disallowedNames.Add("calendar");
            disallowedNames.Add("events");
            disallowedNames.Add("feed");
            disallowedNames.Add("tasks");
            disallowedNames.Add("application");
            disallowedNames.Add("applications");
            disallowedNames.Sort();

            if (disallowedNames.BinarySearch(userName.ToLower()) >= 0)
            {
                matches++;
            }

            if (!Regex.IsMatch(userName, @"^([A-Za-z0-9\-_\.\!~\*'&=\$].+)$"))
            {
                matches++;
            }

            userName = userName.Normalize().ToLower();

            if (userName.Length < 2)
            {
                matches++;
            }

            if (userName.Length > 64)
            {
                matches++;
            }

            if (userName.EndsWith(".aspx"))
            {
                matches++;
            }

            if (userName.EndsWith(".asax"))
            {
                matches++;
            }

            if (userName.EndsWith(".php"))
            {
                matches++;
            }

            if (userName.EndsWith(".html"))
            {
                matches++;
            }

            if (userName.EndsWith(".gif"))
            {
                matches++;
            }

            if (userName.EndsWith(".png"))
            {
                matches++;
            }

            if (userName.EndsWith(".js"))
            {
                matches++;
            }

            if (userName.EndsWith(".bmp"))
            {
                matches++;
            }

            if (userName.EndsWith(".jpg"))
            {
                matches++;
            }

            if (userName.EndsWith(".jpeg"))
            {
                matches++;
            }

            if (userName.EndsWith(".zip"))
            {
                matches++;
            }

            if (userName.EndsWith(".jsp"))
            {
                matches++;
            }

            if (userName.EndsWith(".cfm"))
            {
                matches++;
            }

            if (userName.EndsWith(".exe"))
            {
                matches++;
            }

            if (userName.StartsWith("."))
            {
                matches++;
            }

            if (userName.EndsWith("."))
            {
                matches++;
            }

            if (matches > 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="db"></param>
        /// <param name="userName"></param>
        /// <param name="eMail"></param>
        /// <returns></returns>
        public static bool CheckUserNameUnique(Mysql db, string userName)
        {
            if (db.SelectQuery(string.Format("SELECT user_name FROM user_keys WHERE LCASE(user_name) = '{0}';",
                Mysql.Escape(userName.ToLower()))).Rows.Count > 0)
            {
                return false;
            }
            return true;
        }

        public void ProfileViewed(Member member)
        {
            // only view the profile if not the owner
            if (member == null || member.userId != userId)
            {
                db.UpdateQuery(string.Format("UPDATE user_profile SET profile_views = profile_views + 1 WHERE user_id = {0}",
                    UserId));
            }
        }

        public void BlogViewed(Member member)
        {
            // only view the profile if not the owner
            if (member == null || member.userId != userId)
            {
                db.UpdateQuery(string.Format("UPDATE user_blog SET blog_visits = blog_visits + 1 WHERE user_id = {0}",
                    UserId));
            }
        }

        public static string GenerateActivationSecurityToken()
        {
            Random rand = new Random();
            string captchaString = "";

            char[] chars = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };

            for (int i = 0; i < 20; i++)
            {
                int j = (int)(rand.NextDouble() * chars.Length);
                captchaString += chars[j].ToString();
            }

            return Member.HashPassword(captchaString).Substring((int)(rand.NextDouble() * 20), 32);
        }

        public override bool CanModerateComments(Member member)
        {
            if (member.userId == userId)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool IsCommentOwner(Member member)
        {
            if (member.userId == userId)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override ushort GetAccessLevel(Member viewer)
        {
            Relation viewerRelation;
            if (!sessionRelationsSet)
            {
                viewerRelation = GetRelations(viewer);
                sessionRelations = viewerRelation;
                sessionRelationsSet = true;
            }
            else
            {
                viewerRelation = sessionRelations;
            }

            if (viewer == null)
            {
                return 0x0001;
            }
            else if ((viewerRelation & Relation.Blocked) == Relation.Blocked)
            {
                return 0x0000;
            }
            else if ((viewerRelation & Relation.Family) == Relation.Family)
            {
                return 0x0100;
            }
            else if ((viewerRelation & Relation.Friend) == Relation.Friend)
            {
                return 0x1000;
            }
            else // registered, but not a relation
            {
                return 0x0001;
            }
        }

        public override void GetCan(ushort accessBits, Member viewer, out bool canRead, out bool canComment, out bool canCreate, out bool canChange)
        {
            Relation viewerRelation;
            if (!sessionRelationsSet)
            {
                viewerRelation = GetRelations(viewer);
                sessionRelations = viewerRelation;
                sessionRelationsSet = true;
            }
            else
            {
                viewerRelation = sessionRelations;
            }

            byte accessBitsEveryone = (byte)(accessBits & 0x000F);
            byte accessBitsGroup = (byte)((accessBits & 0x00F0) >> 4);
            byte accessBitsFamily = (byte)((accessBits & 0x0F00) >> 8);
            byte accessBitsFriends = (byte)((accessBits & 0xF000) >> 12);

            if (viewer == null)
            {
                canRead = ((accessBitsEveryone & 0x1) == 0x1);
                canComment = false;
                canCreate = false;
                canChange = false;
            }
            else if ((viewerRelation & Relation.Owner) == Relation.Owner)
            {
                canRead = true;
                canComment = true;
                canCreate = true;
                canChange = true;
            }
            else if ((viewerRelation & Relation.Blocked) == Relation.Blocked)
            {
                canRead = false;
                canComment = false;
                canCreate = false;
                canChange = false;
            }
            else if ((viewerRelation & Relation.Family) == Relation.Family)
            {
                canRead = ((accessBitsFamily & 0x1) == 0x1);
                canComment = ((accessBitsFamily & 0x2) == 0x2);
                canCreate = ((accessBitsFamily & 0x4) == 0x4);
                canChange = ((accessBitsFamily & 0x8) == 0x8);
            }
            else if ((viewerRelation & Relation.Friend) == Relation.Friend)
            {
                canRead = ((accessBitsFriends & 0x1) == 0x1);
                canComment = ((accessBitsFriends & 0x2) == 0x2);
                canCreate = ((accessBitsFriends & 0x4) == 0x4);
                canChange = ((accessBitsFriends & 0x8) == 0x8);
            }
            else
            {
                canRead = ((accessBitsEveryone & 0x1) == 0x1);
                canComment = ((accessBitsEveryone & 0x2) == 0x2);
                canCreate = ((accessBitsEveryone & 0x4) == 0x4);
                canChange = ((accessBitsEveryone & 0x8) == 0x8);
            }
        }

        public override string GenerateBreadCrumbs(List<string[]> parts)
        {
            string output = "";
            string path = string.Format("/{0}", UserName);
            output = string.Format("<a href=\"{1}\">{0}</a>",
                    DisplayName, path);

            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i][0] != "")
                {
                    path += "/" + parts[i][0];
                    output += string.Format(" <strong>&#8249;</strong> <a href=\"{1}\">{0}</a>",
                        parts[i][1], path);
                }
            }

            return output;
        }

        public override string Uri
        {
            get
            {
                return ZzUri.AppendSid(string.Format("/{0}",
                    UserName));
            }
        }

        public static long GetMemberId(Member member)
        {
            long loggedIdUid = 0;
            if (member != null)
            {
                loggedIdUid = member.UserId;
            }
            return loggedIdUid;
        }
    }
}