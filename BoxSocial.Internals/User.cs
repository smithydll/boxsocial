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
using System.Xml;

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

    public enum UserLoadOptions : byte
    {
        Key = 0x01,
        Info = Key | 0x02,
        Profile = Key | Country | Religion | 0x04,
        Icon = Key | 0x08,
        Country = Key | 0x0F,
        Religion = Key | 0x10,
        Common = Key | Info | Icon,
        All = Key | Info | Profile | Icon | Country | Religion,
    }

    [DataTable("user_keys")]
    [Primitive("USER", UserLoadOptions.All, "user_id", "user_name")]
    public class User : Primitive, ICommentableItem
    {
        public static long lastEmailId;

        [DataField("user_id", DataFieldKeys.Primary)]
        protected long userId;
        [DataField("user_name", DataFieldKeys.Unique, 64)]
        private string userName;
        [DataField("user_domain", DataFieldKeys.Unique, 63)]
        private string domain;

        private string userIconUri;

        private bool sessionRelationsSet = false;
        private Relation sessionRelations;

        protected UserInfo userInfo;
        protected UserProfile userProfile;
        
        protected bool iconLoaded = false;

        protected List<UserEmail> emailAddresses;

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

        public UserInfo Info
        {
            get
            {
                if (userInfo == null)
                {
                    userInfo = new UserInfo(core, userId);
                }
                return userInfo;
            }
        }

        public UserProfile Profile
        {
            get
            {
                if (userProfile == null)
                {
                    userProfile = new UserProfile(core, this);
                }
                return userProfile;
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

        public override AppPrimitives AppPrimitive
        {
            get
            {
                return AppPrimitives.Member;
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

        public override string DisplayNameOwnership
        {
            get
            {
                return userInfo.DisplayNameOwnership;
            }
        }

        public override string DisplayName
        {
            get
            {
                return userInfo.DisplayName;
            }
        }

        public override string TitleNameOwnership
        {
            get
            {
                return DisplayNameOwnership;
            }
        }

        public override string TitleName
        {
            get
            {
                return DisplayName;
            }
        }

        public string Sexuality
        {
            get
            {
                return userProfile.Sexuality;
            }
        }

        public string SexualityRaw
        {
            get
            {
                return userProfile.SexualityRaw;
            }
        }

        public string Gender
        {
            get
            {
                return userProfile.Gender;
            }
        }

        public string GenderRaw
        {
            get
            {
                return userProfile.GenderRaw;
            }
        }

        public string MaritialStatus
        {
            get
            {
                return userProfile.MaritialStatus;
            }
        }

        public string MaritialStatusRaw
        {
            get
            {
                return userProfile.MaritialStatusRaw;
            }
        }

        public string Autobiography
        {
            get
            {
                return userProfile.Autobiography;
            }
        }

        public int Age
        {
            get
            {
                return userProfile.Age;
            }
        }

        public string AgeString
        {
            get
            {
                return userProfile.AgeString;
            }
        }

        public DateTime DateOfBirth
        {
            get
            {
                return userProfile.DateOfBirth;
            }
        }

        public DateTime RegistrationDate
        {
            get
            {
                return userInfo.GetRegistrationDate(core.tz);
            }
        }

        public DateTime LastOnlineTime
        {
            get
            {
                return userInfo.GetLastOnlineDate(core.tz);
            }
        }

        public long ProfileViews
        {
            get
            {
                return userProfile.ProfileViews;
            }
        }

        public long ProfileComments
        {
            get
            {
                return userProfile.ProfileComments;
            }
        }

        public long BlogSubscriptions
        {
            get
            {
                return userInfo.BlogSubscriptions;
            }
        }

        public string Country
        {
            get
            {
                return userProfile.Country;
            }
        }

        public string CountryIso
        {
            get
            {
                return userProfile.CountryIso;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Access ProfileAccess
        {
            get
            {
                return userProfile.ProfileAccess;
            }
        }

        public string ProfileUri
        {
            get
            {
                return Uri;
            }
        }

        public string UserThumbnail
        {
            get
            {
                if (userIconUri != null)
                {
                    return string.Format("{0}images/_thumb{1}",
                        UriStub, userIconUri);
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
                    return string.Format("{0}images/_icon{1}",
                        UriStub, userIconUri);
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
                    return string.Format("{0}images/_tile{1}",
                        UriStub, userIconUri);
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
                return userProfile.Title;
            }
        }

        public string FirstName
        {
            get
            {
                return userProfile.FirstName;
            }
        }

        public string MiddleName
        {
            get
            {
                return userProfile.MiddleName;
            }
        }

        public string LastName
        {
            get
            {
                return userProfile.LastName;
            }
        }

        public string Suffix
        {
            get
            {
                return userProfile.Suffix;
            }
        }

        public short ReligionRaw
        {
            get
            {
                return userProfile.ReligionId;
            }
        }

        public ushort Permissions
        {
            get
            {
                return userProfile.Permissions;
            }
        }

        public bool ShowCustomStyles
        {
            get
            {
                return userInfo.ShowCustomStyles;
            }
        }

        public bool BbcodeShowImages
        {
            get
            {
                return userInfo.BbcodeShowImages;
            }
        }

        public bool BbcodeShowFlash
        {
            get
            {
                return userInfo.BbcodeShowFlash;
            }
        }

        public bool BbcodeShowVideos
        {
            get
            {
                return userInfo.BbcodeShowVideos;
            }
        }

        public BbcodeOptions GetUserBbcodeOptions
        {
            get
            {
                return userInfo.GetUserBbcodeOptions;
            }
        }

        public string ProfileHomepage
        {
            get
            {
                return userInfo.ProfileHomepage;
            }
            set
            {
                userInfo.ProfileHomepage = value;
            }
        }

        public long Friends
        {
            get
            {
                return userInfo.Friends;
            }
        }

        public string AlternateEmail
        {
            get
            {
                return userInfo.PrimaryEmail;
            }
        }

        public bool EmailNotifications
        {
            get
            {
                return userInfo.EmailNotifications;
            }
        }

        public ulong BytesUsed
        {
            get
            {
                return userInfo.BytesUsed;
            }
        }

        public long StatusMessages
        {
            get
            {
                return userInfo.StatusMessages;
            }
        }

        public ushort TimeZoneCode
        {
            get
            {
                return userInfo.TimeZoneCode;
            }
        }

        public UnixTime GetTimeZone
        {
            get
            {
                return userInfo.GetTimeZone;
            }
        }

        protected User(Core core)
            : base(core)
        {
        }

        public User(Core core, long userId)
            : this(core, userId, UserLoadOptions.Info | UserLoadOptions.Icon)
        {
        }

        public User(Core core, long userId, UserLoadOptions loadOptions)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(User_ItemLoad);

            bool containsInfoData = false;
            bool containsProfileData = false;
            bool containsIconData = false;

            if (loadOptions == UserLoadOptions.Key)
            {
                try
                {
                    LoadItem(userId);
                }
                catch (InvalidItemException)
                {
                    throw new InvalidUserException();
                }
            }
            else
            {
                SelectQuery query = new SelectQuery(User.GetTable(typeof(User)));
                query.AddFields(User.GetFieldsPrefixed(typeof(User)));
                query.AddCondition("`user_keys`.`user_id`", userId);

                if ((loadOptions & UserLoadOptions.Info) == UserLoadOptions.Info)
                {
                    containsInfoData = true;

                    query.AddJoin(JoinTypes.Inner, UserInfo.GetTable(typeof(UserInfo)), "user_id", "user_id");
                    query.AddFields(UserInfo.GetFieldsPrefixed(typeof(UserInfo)));

                    if ((loadOptions & UserLoadOptions.Icon) == UserLoadOptions.Icon)
                    {
                        containsIconData = true;

                        query.AddJoin(JoinTypes.Left, new DataField("user_info", "user_icon"), new DataField("gallery_items", "gallery_item_id"));
                        query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                        query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
                    }
                }

                if ((loadOptions & UserLoadOptions.Profile) == UserLoadOptions.Profile)
                {
                    containsProfileData = true;

                    query.AddJoin(JoinTypes.Inner, UserProfile.GetTable(typeof(UserProfile)), "user_id", "user_id");
                    query.AddFields(UserProfile.GetFieldsPrefixed(typeof(UserProfile)));

                    if ((loadOptions & UserLoadOptions.Country) == UserLoadOptions.Country)
                    {
                        query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_country"), new DataField("countries", "country_iso"));
                    }

                    if ((loadOptions & UserLoadOptions.Religion) == UserLoadOptions.Religion)
                    {
                        query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_religion"), new DataField("religions", "religion_id"));
                    }
                }

                DataTable memberTable = db.Query(query);

                if (memberTable.Rows.Count > 0)
                {
                    loadItemInfo(typeof(User), memberTable.Rows[0]);

                    if (containsInfoData)
                    {
                        userInfo = new UserInfo(core, memberTable.Rows[0]);
                    }

                    if (containsProfileData)
                    {
                        userProfile = new UserProfile(core, this, memberTable.Rows[0], loadOptions);
                    }

                    if (containsIconData)
                    {
                        loadUserIcon(memberTable.Rows[0]);
                    }
                }
                else
                {
                    throw new InvalidUserException();
                }
            }
        }

        public User(Core core, string userName)
            : this (core, userName, UserLoadOptions.Info | UserLoadOptions.Icon)
        {
        }

        public User(Core core, string userName, UserLoadOptions loadOptions)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(User_ItemLoad);

            bool containsInfoData = false;
            bool containsProfileData = false;
            bool containsIconData = false;

            if (loadOptions == UserLoadOptions.Key)
            {
                try
                {
                    LoadItem("user_name", userName);
                }
                catch (InvalidItemException)
                {
                    throw new InvalidUserException();
                }
            }
            else
            {
                SelectQuery query = new SelectQuery(User.GetTable(typeof(User)));
                query.AddFields(User.GetFieldsPrefixed(typeof(User)));
                query.AddCondition("`user_keys`.`user_name`", userName);

                if ((loadOptions & UserLoadOptions.Info) == UserLoadOptions.Info)
                {
                    containsInfoData = true;

                    query.AddJoin(JoinTypes.Inner, UserInfo.GetTable(typeof(UserInfo)), "user_id", "user_id");
                    query.AddFields(UserInfo.GetFieldsPrefixed(typeof(UserInfo)));

                    if ((loadOptions & UserLoadOptions.Icon) == UserLoadOptions.Icon)
                    {
                        containsIconData = true;

                        query.AddJoin(JoinTypes.Left, new DataField("user_info", "user_icon"), new DataField("gallery_items", "gallery_item_id"));
                        query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                        query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
                    }
                }

                if ((loadOptions & UserLoadOptions.Profile) == UserLoadOptions.Profile)
                {
                    containsProfileData = true;

                    query.AddJoin(JoinTypes.Inner, UserProfile.GetTable(typeof(UserProfile)), "user_id", "user_id");
                    query.AddFields(UserProfile.GetFieldsPrefixed(typeof(UserProfile)));

                    if ((loadOptions & UserLoadOptions.Country) == UserLoadOptions.Country)
                    {
                        query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_country"), new DataField("countries", "country_iso"));
                    }

                    if ((loadOptions & UserLoadOptions.Religion) == UserLoadOptions.Religion)
                    {
                        query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_religion"), new DataField("religions", "religion_id"));
                    }
                }

                DataTable memberTable = db.Query(query);

                if (memberTable.Rows.Count > 0)
                {
                    loadItemInfo(typeof(User), memberTable.Rows[0]);

                    if (containsInfoData)
                    {
                        userInfo = new UserInfo(core, memberTable.Rows[0]);
                    }

                    if (containsProfileData)
                    {
                        userProfile = new UserProfile(core, this, memberTable.Rows[0], loadOptions);
                    }

                    if (containsIconData)
                    {
                        loadUserIcon(memberTable.Rows[0]);
                    }
                }
                else
                {
                    throw new InvalidUserException();
                }
            }
        }

        public User(Core core, DataRow userRow, UserLoadOptions loadOptions)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(User_ItemLoad);

            if (userRow != null)
            {
                loadItemInfo(typeof(User), userRow);

                if ((loadOptions & UserLoadOptions.Info) == UserLoadOptions.Info)
                {
                    userInfo = new UserInfo(core, userRow);
                }

                if ((loadOptions & UserLoadOptions.Profile) == UserLoadOptions.Profile)
                {
                    userProfile = new UserProfile(core, this, userRow, loadOptions);
                }

                if ((loadOptions & UserLoadOptions.Icon) == UserLoadOptions.Icon)
                {
                    loadUserIcon(userRow);
                }
            }
            else
            {
                throw new InvalidUserException();
            }
        }

        void User_ItemLoad()
        {
        }

        public void LoadProfileInfo()
        {
            if (userProfile == null)
            {
                userProfile = new UserProfile(core, this);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userRow"></param>
        protected void loadUserInfo(DataRow userRow)
        {
            userInfo = new UserInfo(core, userRow);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userRow"></param>
        protected void loadUserProfile(DataRow userRow)
        {
            // use the user's timezone, after all it is _their_ birthday
            //dateOfBirth = GetTimeZone.DateTimeFromMysql(userRow["profile_date_of_birth_ut"]);

            userProfile = new UserProfile(core, this, userRow);
        }

        protected void loadUserIcon(DataRow userRow)
        {
            if (!(userRow["gallery_item_uri"] is DBNull))
            {
                userIconUri = string.Format("/{0}/{1}",
                    (string)userRow["gallery_item_parent_path"], (string)userRow["gallery_item_uri"]);
            }
        }

        protected void loadUserFromUser(User member)
        {
            this.userId = member.userId;
            this.userName = member.userName;

            this.userInfo = member.userInfo;
            this.userProfile = member.userProfile;

            this.userIconUri = member.userIconUri;
        }

        public string GetUserStyle()
        {
            DataTable userStyleTable = db.Query(string.Format("SELECT us.* FROM user_keys uk INNER JOIN user_style us ON uk.user_id = us.user_id WHERE uk.user_id = {0}",
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

        public List<long> GetFriendIds()
        {
            return GetFriendIds(255);
        }

        public List<long> GetFriendIds(int count)
        {
            List<long> friendIds = new List<long>();

            SelectQuery query = new SelectQuery("user_relations uf");
            query.AddFields("uf.relation_you");
            query.AddCondition("uf.relation_me", userId);
            query.AddCondition("uf.relation_type", "FRIEND");
            query.AddSort(SortOrder.Ascending, "(uf.relation_order - 1)");
            query.AddSort(SortOrder.Ascending, "relation_time_ut");
            query.LimitCount = count;

            DataTable friendsTable = db.Query(query);

            foreach (DataRow dr in friendsTable.Rows)
            {
                friendIds.Add((long)dr["relation_you"]);
            }

            return friendIds;
        }

        /// <summary>
        /// return a maximum of the first 255
        /// </summary>
        /// <returns></returns>
        public List<UserRelation> GetFriends()
        {
            return GetFriends(1, 255);
        }

        public List<UserRelation> GetFriends(int page, int perPage)
        {
            List<UserRelation> friends = new List<UserRelation>();

            SelectQuery query = new SelectQuery("user_relations");
            query.AddFields(User.GetFieldsPrefixed(typeof(User)));
            query.AddFields(UserInfo.GetFieldsPrefixed(typeof(UserInfo)));
            query.AddFields(UserProfile.GetFieldsPrefixed(typeof(UserProfile)));
            query.AddFields(UserRelation.GetFieldsPrefixed(typeof(UserRelation)));
            query.AddField(new DataField("gallery_items", "gallery_item_uri"));
            query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
            query.AddJoin(JoinTypes.Inner, User.GetTable(typeof(User)), "relation_you", "user_id");
            query.AddJoin(JoinTypes.Inner, UserInfo.GetTable(typeof(UserInfo)), "relation_you", "user_id");
            query.AddJoin(JoinTypes.Inner, UserProfile.GetTable(typeof(UserProfile)), "relation_you", "user_id");
            query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_country"), new DataField("countries", "country_iso"));
            query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_religion"), new DataField("religions", "religion_id"));
            query.AddJoin(JoinTypes.Left, new DataField("user_info", "user_icon"), new DataField("gallery_items", "gallery_item_id"));
            query.AddCondition("relation_me", userId);
            query.AddCondition("relation_type", "FRIEND");
            query.AddSort(SortOrder.Ascending, "(relation_order - 1)");
            query.LimitStart = (page - 1) * perPage;
            query.LimitCount = perPage;

            DataTable friendsTable = db.Query(query);

            foreach (DataRow dr in friendsTable.Rows)
            {
                friends.Add(new UserRelation(core, dr, UserLoadOptions.All));
            }

            return friends;
        }

        public List<UserRelation> GetFriendsBirthdays(long startTimeRaw, long endTimeRaw)
        {
            DateTime st = core.tz.DateTimeFromMysql(startTimeRaw - 24 * 60 * 60);
            DateTime et = core.tz.DateTimeFromMysql(endTimeRaw + 48 * 60 * 60);

            List<UserRelation> friends = new List<UserRelation>();

            SelectQuery query = UserRelation.GetSelectQueryStub(UserLoadOptions.All);
            query.AddCondition("relation_me", userId);
            query.AddCondition("relation_type", "FRIEND");
            query.AddCondition("profile_date_of_birth_month_cache * 31 + profile_date_of_birth_day_cache", ConditionEquality.GreaterThanEqual, st.Month * 31 + st.Day);
            query.AddCondition("profile_date_of_birth_month_cache * 31 + profile_date_of_birth_day_cache", ConditionEquality.LessThanEqual, et.Month * 31 + et.Day);

            DataTable friendsTable = db.Query(query);

            foreach (DataRow dr in friendsTable.Rows)
            {
                UserRelation friend = new UserRelation(core, dr, UserLoadOptions.All);
                UnixTime tz = new UnixTime(friend.TimeZoneCode);
                DateTime dob = new DateTime(st.Year, friend.Profile.DateOfBirth.Month, friend.Profile.DateOfBirth.Day);
                long dobUt = tz.GetUnixTimeStamp(dob);

                if ((dobUt >= startTimeRaw && dobUt <= endTimeRaw) ||
                    (dobUt + 24 * 60 * 60 - 1 >= startTimeRaw && dobUt + 24 * 60 * 60 - 1 <= endTimeRaw))
                {
                    friends.Add(friend);
                }
            }

            return friends;
        }

        public List<UserRelation> GetFriendsOnline()
        {
            return GetFriendsOnline(1, 255);
        }

        public List<UserRelation> GetFriendsOnline(int page, int perPage)
        {
            List<UserRelation> friends = new List<UserRelation>();

            SelectQuery query = UserRelation.GetSelectQueryStub(UserLoadOptions.All);
            query.AddCondition("relation_me", userId);
            query.AddCondition("relation_type", "FRIEND");
            // last 15 minutes
            query.AddCondition("UNIX_TIMSTAMP() - ui.user_last_visit_ut", ConditionEquality.LessThan, 900);
            query.AddSort(SortOrder.Ascending, "(uf.relation_order - 1)");
            query.LimitStart = (page - 1) * perPage;
            query.LimitCount = perPage;

            DataTable friendsTable = db.Query(query);

            foreach (DataRow dr in friendsTable.Rows)
            {
                friends.Add(new UserRelation(core, dr, UserLoadOptions.All));
            }

            return friends;
        }

        public List<UserEmail> GetEmailAddresses()
        {
            if (emailAddresses == null)
            {
                emailAddresses = getSubItems(typeof(UserEmail)).ConvertAll<UserEmail>(new Converter<Item, UserEmail>(convertToUserEmail));
            }

            return emailAddresses;
        }

        public UserEmail convertToUserEmail(Item input)
        {
            return (UserEmail)input;
        }

        public List<UserRelation> SearchFriendNames(string needle)
        {
            return SearchFriendNames(needle, 1, 255);
        }

        public List<UserRelation> SearchFriendNames(string needle, int page, int perPage)
        {
            List<UserRelation> friends = new List<UserRelation>();

            SelectQuery query = UserRelation.GetSelectQueryStub(UserLoadOptions.All);
            query.AddCondition("relation_me", userId);
            query.AddCondition("relation_type", "FRIEND");

            // here we are grouping the condition to do an OR between these two conditions only
            QueryCondition qc = query.AddCondition("user_info.user_name", ConditionEquality.Like, QueryCondition.EscapeLikeness(needle) + "%");
            qc.AddCondition(ConditionRelations.Or, "user_info.user_name_display", ConditionEquality.Like, QueryCondition.EscapeLikeness(needle) + "%");

            query.AddSort(SortOrder.Ascending, "(relation_order - 1)");
            query.LimitStart = (page - 1) * perPage;
            query.LimitCount = perPage;

            DataTable friendsTable = db.Query(query);

            foreach (DataRow dr in friendsTable.Rows)
            {
                friends.Add(new UserRelation(core, dr, UserLoadOptions.All));
            }

            return friends;
        }

        public Relation GetRelations(User member)
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

            DataTable relationMe = db.Query(string.Format("SELECT relation_type, relation_order FROM user_relations WHERE relation_me = {0} AND relation_you = {1};",
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

        public bool IsFriend(User member)
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

        public bool IsFamily(User member)
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

        public bool IsBlocked(User member)
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

        public static SelectQuery GetSelectQueryStub(UserLoadOptions loadOptions)
        {
            SelectQuery query = new SelectQuery(GetTable(typeof(User)));
            query.AddFields(User.GetFieldsPrefixed(typeof(User)));
            if ((loadOptions & UserLoadOptions.Info) == UserLoadOptions.Info)
            {
                query.AddJoin(JoinTypes.Inner, UserInfo.GetTable(typeof(UserInfo)), "user_id", "user_id");
                query.AddFields(UserInfo.GetFieldsPrefixed(typeof(UserInfo)));

                if ((loadOptions & UserLoadOptions.Icon) == UserLoadOptions.Icon)
                {
                    query.AddJoin(JoinTypes.Left, new DataField("user_info", "user_icon"), new DataField("gallery_items", "gallery_item_id"));
                    query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                    query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
                }
            }

            if ((loadOptions & UserLoadOptions.Profile) == UserLoadOptions.Profile)
            {
                query.AddJoin(JoinTypes.Inner, UserProfile.GetTable(typeof(UserProfile)), "user_id", "user_id");
                query.AddFields(UserProfile.GetFieldsPrefixed(typeof(UserProfile)));

                if ((loadOptions & UserLoadOptions.Country) == UserLoadOptions.Country)
                {
                    query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_country"), new DataField("countries", "country_iso"));
                }

                if ((loadOptions & UserLoadOptions.Religion) == UserLoadOptions.Religion)
                {
                    query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_religion"), new DataField("religions", "religion_id"));
                }
            }

            return query;
        }

        public static SelectQuery User_GetSelectQueryStub()
        {
            return GetSelectQueryStub(UserLoadOptions.All);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="db"></param>
        /// <param name="userName"></param>
        /// <param name="eMail"></param>
        /// <param name="password"></param>
        /// <param name="passwordConfirm"></param>
        /// <returns>Null if registration failed</returns>
        public static User Register(Core core, string userName, string eMail, string password, string passwordConfirm)
        {
            Mysql db = core.db;
            SessionState session = core.session;

            string passwordClearText = password;

            if (!CheckUserNameUnique(db, userName))
            {
                return null;
            }

            if (!CheckUserNameValid(userName))
            {
                return null;
            }

            password = VerifyPasswordMatch(password, passwordConfirm);

            if (password == "")
            {
                return null;
            }

            string activateKey = User.GenerateActivationSecurityToken();

            InsertQuery query = new InsertQuery("user_keys");
            query.AddField("user_name", userName);

            db.BeginTransaction();
            long userId = db.Query(query);

            if (userId < 0)
            {
                throw new InvalidUserException();
            }

            query = new InsertQuery("user_info");
            query.AddField("user_id", userId);
            query.AddField("user_name", userName);
            query.AddField("user_alternate_email", eMail);
            query.AddField("user_password", password);
            query.AddField("user_reg_date_ut", core.tz.GetUnixTimeStamp(core.tz.Now));
            query.AddField("user_activate_code", activateKey);
            query.AddField("user_reg_ip", session.IPAddress.ToString());
            query.AddField("user_home_page", "/profile");
            query.AddField("user_bytes", 0);
            query.AddField("user_status_messages", 0);
            query.AddField("user_last_visit_ut", 0);
            query.AddField("user_show_bbcode", 0x07);
            query.AddField("user_show_custom_styles", true);

            db.BeginTransaction();
            if (db.Query(query) < 0)
            {
                HttpContext.Current.Response.Write(query.ToString());
                HttpContext.Current.Response.End();

                throw new InvalidUserException();
            }

            query = new InsertQuery("user_profile");
            query.AddField("user_id", userId);
            query.AddField("profile_date_of_birth_ut", UnixTime.UnixTimeStamp(new DateTime(1000, 1, 1)));
            query.AddField("profile_access", 0x3331);

            db.Query(query);

            User newUser = new User(core, userId);
            UserEmail registrationEmail = UserEmail.Create(core, newUser, eMail, 0x0000, true);

            // Install a couple of applications
            try
            {
                ApplicationEntry profileAe = new ApplicationEntry(core, null, "Profile");
                profileAe.Install(core, newUser);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry galleryAe = new ApplicationEntry(core, null, "Gallery");
                galleryAe.Install(core, newUser);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry guestbookAe = new ApplicationEntry(core, null, "GuestBook");
                guestbookAe.Install(core, newUser);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry groupsAe = new ApplicationEntry(core, null, "Groups");
                groupsAe.Install(core, newUser);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry networksAe = new ApplicationEntry(core, null, "Networks");
                networksAe.Install(core, newUser);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry calendarAe = new ApplicationEntry(core, null, "Calendar");
                calendarAe.Install(core, newUser);
            }
            catch
            {
            }

            string activateUri = string.Format("http://zinzam.com/register/?mode=activate&id={0}&key={1}",
                userId, activateKey);

            RawTemplate emailTemplate = new RawTemplate(HttpContext.Current.Server.MapPath("./templates/emails/"), "registration_welcome.eml");

            emailTemplate.Parse("TO_NAME", userName);
            emailTemplate.Parse("U_ACTIVATE", activateUri);
            emailTemplate.Parse("USERNAME", userName);
            emailTemplate.Parse("PASSWORD", passwordClearText);

            Email.SendEmail(eMail, "Welcome to ZinZam", emailTemplate.ToString());

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

        public static bool CheckEmailUnique(Core core, string eMail)
        {
            try
            {
                UserEmail uMail = new UserEmail(core, eMail);
                return false; // not unique
            }
            catch (InvalidUserEmailException)
            {
                return true; // unique
            }

            // TODO: register all e-mail addresses into a new table, along with privacy controls
            /*DataTable userTable = db.Query(string.Format("SELECT user_id, user_alternate_email FROM user_info WHERE LCASE(user_alternate_email) = '{0}';",
                Mysql.Escape(eMail.ToLower())));
            if (userTable.Rows.Count > 0)
            {
                lastEmailId = (int)userTable.Rows[0]["user_id"];
                return false;
            }

            DataTable networkMemberTable = db.Query(string.Format("SELECT user_id, member_email FROM network_members WHERE LCASE(member_email) = '{0}';",
                Mysql.Escape(eMail.ToLower())));
            if (networkMemberTable.Rows.Count > 0)
            {
                lastEmailId = (int)networkMemberTable.Rows[0]["user_id"];
                return false;
            }

            SelectQuery query = new SelectQuery(UserEmail.GetTable(typeof(UserEmail)));
            query.AddCondition(new QueryFunction("email_email", QueryFunctions.ToLowerCase).ToString(), eMail.ToLower());

            DataTable emailsTable = db.Query(query);

            return true;*/
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
            disallowedNames.Add("error-handler");
            disallowedNames.Sort();

            if (disallowedNames.BinarySearch(userName.ToLower()) >= 0)
            {
                matches++;
            }

            if (!Regex.IsMatch(userName, @"^([A-Za-z0-9\-_\.\!~\*'&=\$].+)$"))
            {
                matches++;
            }

            if (userName.Contains(" "))
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
            if (db.Query(string.Format("SELECT user_name FROM user_keys WHERE LCASE(user_name) = '{0}';",
                Mysql.Escape(userName.ToLower()))).Rows.Count > 0)
            {
                return false;
            }
            return true;
        }

        public void ProfileViewed(User member)
        {
            // only view the profile if not the owner
            if (member == null || member.userId != userId)
            {
                db.UpdateQuery(string.Format("UPDATE user_profile SET profile_views = profile_views + 1 WHERE user_id = {0}",
                    UserId));
            }
        }

        public void BlogViewed(User member)
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

            return User.HashPassword(captchaString).Substring((int)(rand.NextDouble() * 20), 32);
        }

        public static string GenerateRandomPassword()
        {
            Random rand = new Random();
            string captchaString = "";

            char[] chars = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };

            for (int i = 0; i < 10; i++)
            {
                int j = (int)(rand.NextDouble() * chars.Length);
                captchaString += chars[j].ToString();
            }

            return captchaString;
        }

        public override bool CanModerateComments(User member)
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

        public override bool IsCommentOwner(User member)
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

        public override ushort GetAccessLevel(User viewer)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="accessBits"></param>
        /// <param name="viewer"></param>
        /// <param name="canRead"></param>
        /// <param name="canComment"></param>
        /// <param name="canCreate"></param>
        /// <param name="canChange"></param>
        public override void GetCan(ushort accessBits, User viewer, out bool canRead, out bool canComment, out bool canCreate, out bool canChange)
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
            // TODO: ACL
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
                    output += string.Format(" <strong>&#8249;</strong> <a href=\"{1}\">{0}</a>",
                        parts[i][1], path + "/" + parts[i][0].TrimStart(new char[] { '*' }));
                    if (!parts[i][0].StartsWith("*"))
                    {
                        path += "/" + parts[i][0];
                    }
                }
            }

            return output;
        }

        public override string UriStub
        {
            get
            {
                if (string.IsNullOrEmpty(domain))
                {
                    if (HttpContext.Current.Request.Url.Host.ToLower() != Linker.Domain)
                    {
                        return Linker.Uri + UserName + "/";
                    }
                    else
                    {
                        return Linker.AppendSid(string.Format("/{0}/",
                            UserName));
                    }
                }
                else
                {
                    if (domain == HttpContext.Current.Request.Url.Host.ToLower())
                    {
                        return "/";
                    }
                    else
                    {
                        return string.Format("http://{0}/",
                            domain);
                    }
                }
            }
        }

        public override string Uri
        {
            get
            {
                return UriStub;
            }
        }

        public static long GetMemberId(User member)
        {
            long loggedIdUid = 0;
            if (member != null)
            {
                loggedIdUid = member.UserId;
            }
            return loggedIdUid;
        }

        public static void ShowProfile(Core core, PPage page)
        {
            core.template.SetTemplate("viewprofile.html");
            page.Signature = PageSignature.viewprofile;

            bool hasProfileInfo = false;

            page.ProfileOwner.LoadProfileInfo();

            page.ProfileOwner.ProfileAccess.SetViewer(core.session.LoggedInMember);

            if (!page.ProfileOwner.ProfileAccess.CanRead)
            {
                Functions.Generate403();
                return;
            }

            string age;
            int ageInt = page.ProfileOwner.Age;
            if (ageInt == 0)
            {
                age = "FALSE";
            }
            else
            {
                age = ageInt.ToString() + " years old";
            }

            core.template.Parse("USER_SEXUALITY", page.ProfileOwner.Sexuality);
            core.template.Parse("USER_GENDER", page.ProfileOwner.Gender);
            //core.template.ParseRaw("USER_AUTOBIOGRAPHY", Bbcode.Parse(HttpUtility.HtmlEncode(page.ProfileOwner.Autobiography), core.session.LoggedInMember));
            Display.ParseBbcode("USER_AUTOBIOGRAPHY", page.ProfileOwner.Autobiography);
            Display.ParseBbcode("USER_MARITIAL_STATUS", page.ProfileOwner.MaritialStatus);
            core.template.Parse("USER_AGE", age);
            core.template.Parse("USER_JOINED", core.tz.DateTimeToString(page.ProfileOwner.RegistrationDate));
            core.template.Parse("USER_LAST_SEEN", core.tz.DateTimeToString(page.ProfileOwner.LastOnlineTime, true));
            core.template.Parse("USER_PROFILE_VIEWS", Functions.LargeIntegerToString(page.ProfileOwner.ProfileViews));
            core.template.Parse("USER_SUBSCRIPTIONS", Functions.LargeIntegerToString(page.ProfileOwner.BlogSubscriptions));
            core.template.Parse("USER_COUNTRY", page.ProfileOwner.Country);
            core.template.Parse("USER_ICON", page.ProfileOwner.UserThumbnail);

            core.template.Parse("U_PROFILE", page.ProfileOwner.Uri);
            core.template.Parse("U_BLOG", Linker.BuildBlogUri(page.ProfileOwner));
            core.template.Parse("U_GALLERY", Linker.BuildGalleryUri(page.ProfileOwner));
            core.template.Parse("U_FRIENDS", Linker.BuildFriendsUri(page.ProfileOwner));

            core.template.Parse("IS_PROFILE", "TRUE");

            if (page.ProfileOwner.MaritialStatusRaw != "UNDEF")
            {
                hasProfileInfo = true;
            }
            if (page.ProfileOwner.GenderRaw != "UNDEF")
            {
                hasProfileInfo = true;
            }
            if (page.ProfileOwner.SexualityRaw != "UNDEF")
            {
                hasProfileInfo = true;
            }

            if (hasProfileInfo)
            {
                core.template.Parse("HAS_PROFILE_INFO", "TRUE");
            }

            core.template.Parse("U_ADD_FRIEND", Linker.BuildAddFriendUri(page.ProfileOwner.UserId));
            core.template.Parse("U_BLOCK_USER", Linker.BuildBlockUserUri(page.ProfileOwner.UserId));

            string langFriends = (page.ProfileOwner.Friends != 1) ? "friends" : "friend";

            core.template.Parse("FRIENDS", page.ProfileOwner.Friends.ToString());
            core.template.Parse("L_FRIENDS", langFriends);

            List<UserRelation> friends = page.ProfileOwner.GetFriends(1, 8);
            foreach (UserRelation friend in friends)
            {
                VariableCollection friendVariableCollection = core.template.CreateChild("friend_list");

                friendVariableCollection.Parse("USER_DISPLAY_NAME", friend.DisplayName);
                friendVariableCollection.Parse("U_PROFILE", friend.Uri);
                friendVariableCollection.Parse("ICON", friend.UserIcon);
            }

            ushort readAccessLevel = page.ProfileOwner.GetAccessLevel(core.session.LoggedInMember);
            long loggedIdUid = User.GetMemberId(core.session.LoggedInMember);

            /* Show a list of lists */
            DataTable listTable = core.db.Query(string.Format("SELECT ul.list_path, ul.list_title FROM user_keys uk INNER JOIN user_lists ul ON ul.user_id = uk.user_id WHERE uk.user_id = {0} AND (list_access & {2:0} OR ul.user_id = {1})",
                page.ProfileOwner.UserId, loggedIdUid, readAccessLevel));

            for (int i = 0; i < listTable.Rows.Count; i++)
            {
                VariableCollection listVariableCollection = core.template.CreateChild("list_list");

                listVariableCollection.Parse("TITLE", (string)listTable.Rows[i]["list_title"]);
                listVariableCollection.Parse("URI", "/" + page.ProfileOwner.UserName + "/lists/" + Linker.AppendSid((string)listTable.Rows[i]["list_path"]));
            }

            core.template.Parse("LISTS", listTable.Rows.Count.ToString());

            /* pages */
            //core.template.Parse("PAGE_LIST", Display.GeneratePageList(page.ProfileOwner, core.session.LoggedInMember, true));
            Display.ParsePageList(page.ProfileOwner, true);

            /* status */
            StatusMessage statusMessage = StatusFeed.GetLatest(core, page.ProfileOwner);

            if (statusMessage != null)
            {
                core.template.Parse("STATUS_MESSAGE", statusMessage.Message);
                core.template.Parse("STATUS_UPDATED", core.tz.DateTimeToString(statusMessage.GetTime(core.tz)));
            }

            core.InvokeHooks(new HookEventArgs(core, AppPrimitives.Member, page.ProfileOwner));

            page.ProfileOwner.ProfileViewed(core.session.LoggedInMember);
        }

        public override string AccountUriStub
        {
            get
            {
                return "/account/";
            }
        }

        public override string Namespace
        {
            get
            {
                return Type;
            }
        }

        public long Comments
        {
            get
            {
                return userProfile.Comments;
            }
        }

        public SortOrder CommentSortOrder
        {
            get
            {
                return SortOrder.Descending;
            }
        }

        public byte CommentsPerPage
        {
            get
            {
                return 10;
            }
        }

        public new long Update()
        {
            throw new Exception("Cannot update user key table.");
        }
    }

    public class InvalidUserException : Exception
    {
    }

    public class UserNameInvalidException : Exception
    {
    }

    public class UserNameAlreadyRegisteredException : Exception
    {
    }

    public class EmailInvalidException : Exception
    {
    }

    public class EmailAlreadyRegisteredException : Exception
    {
    }
}