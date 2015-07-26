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
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
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

    public enum UserLoadOptions : byte
    {
        Key = 0x01,
        Info = Key | 0x02,
        Profile = Key | Country | Religion | 0x04,
        /*Icon = Key | 0x08,*/
        Country = Key | 0x0F,
        Religion = Key | 0x10,
        Common = Key | Info /*| Icon*/,
        All = Key | Info | Profile /*| Icon*/ | Country | Religion,
    }

    public enum SubscriberLevel : byte
    {
        Free = 0x00,
        Paid1 = 0x01,
        Paid10 = 0x02,
        Paid100 = 0x03,
    }

    [DataTable("user_keys", "USER")]
    [Primitive("USER", UserLoadOptions.All, "user_id", "user_name")]
    [Permission("VIEW", "Can view user profile", PermissionTypes.View)]
    [Permission("VIEW_STATUS", "Can view your status", PermissionTypes.View)]
    [Permission("COMMENT", "Can write on the guest book", PermissionTypes.Interact)]
    [Permission("SEND_MESSAGE", "Can send private messages", PermissionTypes.Interact)]
    [Permission("DELETE_COMMENTS", "Can delete comments from the guest book", PermissionTypes.Delete)]
    [Permission("DELETE_STATUS", "Can status messages", PermissionTypes.Delete)]
    [Permission("VIEW_NAME", "Can see your real name", PermissionTypes.View)]
    [Permission("VIEW_SEXUALITY", "Can see your sexuality", PermissionTypes.View)]
    [Permission("VIEW_CONTACT_INFO", "Can see your contact information (does not include e-mail addresses and phone numbers)", PermissionTypes.View)]
    [Permission("VIEW_BIOGRAPHY", "Can see your biography", PermissionTypes.View)]
    [Permission("VIEW_HOMEPAGE", "Can see the link to your homepage", PermissionTypes.View)]
    [Permission("VIEW_GROUPS", "Can see your group memberships", PermissionTypes.View)]
    [Permission("VIEW_NETWORKS", "Can see your network memberships", PermissionTypes.View)]
    [Permission("VIEW_FRIENDS", "Can see your friends", PermissionTypes.View)]
    [Permission("VIEW_FAMILY", "Can see your family", PermissionTypes.View)]
    [Permission("VIEW_COLLEAGUES", "Can see your colleagues", PermissionTypes.View)]
    [PermissionGroup]
    [JsonObject("user")]
    public class User : Primitive, ICommentableItem, IPermissibleItem, ISearchableItem, ISubscribeableItem, INotifiableItem
    {
        [DataField("user_id", DataFieldKeys.Primary)]
        protected long userId;
        [DataField("user_name", DataFieldKeys.Unique, 64)]
        protected string userName;
		[DataField("user_name_lower", DataFieldKeys.Unique, 64)]
        private string userNameLower;
        [DataField("user_domain", DataFieldKeys.Index, 63)]
        protected string domain;
        [DataField("user_name_first", DataFieldKeys.Index, 1)]
        protected string userNameFirstCharacter;
        [DataField("user_simple_permissions")]
        private bool simplePermissions;

        private string userIconUri;
        private string userCoverPhotoUri;

        private bool sessionRelationsSet = false;
        private Relation sessionRelations;

        protected UserInfo userInfo;
        protected UserProfile userProfile;
        protected UserStyle userStyle;
        protected UserSettings userSettings;
        
        protected bool iconLoaded = false;

        protected List<UserEmail> emailAddresses;

        private Access access;

        public event CommentHandler OnCommentPosted;

        /// <summary>
        /// user ID (read only)
        /// </summary>
        [JsonProperty("id")]
        public long UserId
        {
            get
            {
                return userId;
            }
        }

        [JsonIgnore]
        public string Domain
        {
            get
            {
                return domain;
            }
        }

        [JsonIgnore]
        public UserInfo UserInfo
        {
            get
            {
                if (userInfo == null)
                {
                    ItemKey userInfoKey = new ItemKey(Id, ItemType.GetTypeId(core, typeof(UserInfo)));
                    core.ItemCache.RequestItem(userInfoKey);

                    userInfo = (UserInfo)core.ItemCache[userInfoKey];
                }
                return userInfo;
            }
        }

        [JsonIgnore]
        public UserSettings UserSettings
        {
            get
            {
                if (userSettings == null)
                {
                    userSettings = new UserSettings(this);
                }
                return userSettings;
            }
        }

        /*[JsonProperty("user_info")]
        private UserInfo JsonUserInfo
        {
            get
            {
                return userInfo;
            }
        }*/

        [JsonIgnore]
        public UserProfile Profile
        {
            get
            {
                if (userProfile == null)
                {
                    ItemKey userProfileKey = new ItemKey(Id, ItemType.GetTypeId(core, typeof(UserProfile)));
                    core.ItemCache.RequestItem(userProfileKey);

                    userProfile = (UserProfile)core.ItemCache[userProfileKey];
                }
                return userProfile;
            }
        }

        [JsonProperty("user_profile")]
        private UserProfile JsonUserProfile
        {
            get
            {
                return userProfile;
            }
        }

        [JsonIgnore]
        public UserStyle Style
        {
            get
            {
                if (userStyle == null)
                {
                    try
                    {
                        userStyle = new UserStyle(core, userId);
                    }
                    catch
                    {
                        if (core.LoggedInMemberId == Id)
                        {
                            userStyle = UserStyle.Create(core, this, "");
                        }
                    }
                }
                return userStyle;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [JsonIgnore]
        public override long Id
        {
            get
            {
                return UserId;
            }
        }

        [JsonProperty("type")]
        public override string Type
        {
            get
            {
                return "USER";
            }
        }

        [JsonIgnore]
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
        [JsonProperty("username")]
        public string UserName
        {
            get
            {
                return userName;
            }
        }

        [JsonIgnore]
        public override string Key
        {
            get
            {
                return userName;
            }
        }

        [JsonIgnore]
        public override string DisplayNameOwnership
        {
            get
            {
                return UserInfo.DisplayNameOwnership;
            }
        }

        [JsonIgnore]
        public string Preposition
        {
            get
            {
                switch (Profile.GenderRaw)
                {
                    case Gender.Male:
                        return core.Prose.GetString("HIS");
                    case Gender.Female:
                        return core.Prose.GetString("HER");
                    default:
                        return core.Prose.GetString("THEIR");
                }
            }
        }

        [JsonProperty("display_name")]
        public override string DisplayName
        {
            get
            {
                return UserInfo.DisplayName;
            }
        }

        [JsonIgnore]
        public override string TitleNameOwnership
        {
            get
            {
                return DisplayNameOwnership;
            }
        }

        [JsonIgnore]
        public override string TitleName
        {
            get
            {
                return DisplayName;
            }
        }

        [JsonIgnore]
        public string ProfileUri
        {
            get
            {
                return core.Hyperlink.AppendSid(string.Format("{0}profile",
                    UriStub));
            }
        }

        [JsonIgnore]
        public string UserTiny
        {
            get
            {
                if (UserInfo.DisplayPictureId > 0)
                {
                    return string.Format("{0}images/_tiny/_{1}.png",
                        UriStub, UserName);
                }
                else
                {
                    return core.Hyperlink.AppendCoreSid(string.Format("/images/user/_tiny/{0}.png",
                        userName));
                }
            }
        }

        [JsonIgnore]
        public override string Thumbnail
        {
            get
            {
                if (UserInfo.DisplayPictureId > 0)
                {
                    return string.Format("{0}images/_thumb/_{1}.png",
                        UriStub, UserName);
                }
                else
                {
                    return core.Hyperlink.AppendCoreSid(string.Format("/images/user/_thumb/{0}.png",
                        userName));
                }
            }
        }

        [JsonIgnore]
        public string UserMobile
        {
            get
            {
                if (UserInfo.DisplayPictureId > 0)
                {
                    return string.Format("{0}images/_mobile/_{1}.png",
                        UriStub, UserName);
                }
                else
                {
                    return core.Hyperlink.AppendCoreSid(string.Format("/images/user/_mobile/{0}.png",
                        userName));
                }
            }
        }

        /// <summary>
        /// 50x50 display tile
        /// </summary>
        [JsonIgnore]
        public override string Icon
        {
            get
            {
                if (UserInfo.DisplayPictureId > 0)
                {
                    return string.Format("{0}images/_icon/_{1}.png",
                        UriStub, UserName);
                }
                else
                {
                    return core.Hyperlink.AppendCoreSid(string.Format("/images/user/_icon/{0}.png",
                        userName));
                }
            }
        }

        /// <summary>
        /// 100x100 display tile
        /// </summary>
        [JsonProperty("display_image_uri")]
        public override string Tile
        {
            get
            {
                if (UserInfo.DisplayPictureId > 0)
                {
                    return string.Format("{0}images/_tile/_{1}.png",
                        UriStub, UserName);
                }
                else
                {
                    return core.Hyperlink.AppendCoreSid(string.Format("/images/user/_tile/{0}.png",
                        userName));
                }
            }
        }

        /// <summary>
        /// 200x200 display tile
        /// </summary>
        [JsonProperty("display_image_uri_2x")]
        public override string Square
        {
            get
            {
                if (UserInfo.DisplayPictureId > 0)
                {
                    return string.Format("{0}images/_square/_{1}.png",
                        UriStub, UserName);
                }
                else
                {
                    return core.Hyperlink.AppendCoreSid(string.Format("/images/user/_square/{0}.png",
                        userName));
                }
            }
        }

        /// <summary>
        /// Cover photo
        /// </summary>
        [JsonProperty("cover_photo_uri")]
        public override string CoverPhoto
        {
            get
            {
                if (userCoverPhotoUri == "FALSE")
                {
                    return "FALSE";
                }
                else if (userCoverPhotoUri != null)
                {
                    return string.Format("{0}images/_cover{1}",
                        UriStub, userCoverPhotoUri);
                }
                else
                {
                    SelectQuery query = new SelectQuery("gallery_items");
                    query.AddField(new DataField("gallery_items", "gallery_item_cover_exists"));
                    query.AddField(new DataField("gallery_items", "gallery_item_storage_path"));
                    query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                    query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
                    query.AddCondition("gallery_item_id", UserInfo.CoverPhotoId);

                    System.Data.Common.DbDataReader coverReader = db.ReaderQuery(query);

                    if (coverReader.HasRows)
                    {
                        coverReader.Read();

                        if (core.Settings.UseCdn && (byte)coverReader["gallery_item_cover_exists"] > 0)
                        {
                            string uri = string.Format(core.Http.DefaultProtocol + "{0}/{1}", core.Settings.CdnCoverBucketDomain, (string)coverReader["gallery_item_storage_path"]);

                            coverReader.Close();
                            coverReader.Dispose();

                            return uri;
                        }

                        if (!(coverReader["gallery_item_uri"] is DBNull))
                        {
                            userCoverPhotoUri = string.Format("/{0}/{1}",
                                (string)coverReader["gallery_item_parent_path"], (string)coverReader["gallery_item_uri"]);

                            coverReader.Close();
                            coverReader.Dispose();

                            return string.Format("{0}images/_cover{1}",
                                UriStub, userCoverPhotoUri);
                        }
                    }

                    coverReader.Close();
                    coverReader.Dispose();

                    userCoverPhotoUri = "FALSE";
                    return "FALSE";
                }
            }
        }

        /// <summary>
        /// Cover photo
        /// </summary>
        [JsonIgnore]
        public override string MobileCoverPhoto
        {
            get
            {
                if (userCoverPhotoUri == "FALSE")
                {
                    return "FALSE";
                }
                else if (userCoverPhotoUri != null)
                {
                    return string.Format("{0}images/_mcover{1}",
                        UriStub, userCoverPhotoUri);
                }
                else
                {
                    SelectQuery query = new SelectQuery("gallery_items");
                    query.AddField(new DataField("gallery_items", "gallery_item_mobile_cover_exists"));
                    query.AddField(new DataField("gallery_items", "gallery_item_storage_path"));
                    query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                    query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
                    query.AddCondition("gallery_item_id", UserInfo.CoverPhotoId);

                    System.Data.Common.DbDataReader coverReader = db.ReaderQuery(query);

                    if (coverReader.HasRows)
                    {
                        coverReader.Read();

                        if (core.Settings.UseCdn && (byte)coverReader["gallery_item_mobile_cover_exists"] > 0)
                        {
                            string uri = string.Format(core.Http.DefaultProtocol + "{0}/{1}", core.Settings.CdnMobileCoverBucketDomain, (string)coverReader["gallery_item_storage_path"]);

                            coverReader.Close();
                            coverReader.Dispose();

                            return uri;
                        }

                        if (!(coverReader["gallery_item_uri"] is DBNull))
                        {
                            userCoverPhotoUri = string.Format("/{0}/{1}",
                                (string)coverReader["gallery_item_parent_path"], (string)coverReader["gallery_item_uri"]);

                            coverReader.Close();
                            coverReader.Dispose();

                            return string.Format("{0}images/_mcover{1}",
                                UriStub, userCoverPhotoUri);
                        }
                    }

                    coverReader.Close();
                    coverReader.Dispose();

                    userCoverPhotoUri = "FALSE";
                    return "FALSE";
                }
            }
        }

        [JsonIgnore]
        public string UserDomain
        {
            get
            {
                return domain;
            }
            set
            {
                if (domain != value)
                {
                    domain = value;

                    UpdateQuery uQuery = new UpdateQuery(typeof(User));
                    uQuery.AddField("user_domain", domain);
                    uQuery.AddCondition("user_id", Id);

                    db.Query(uQuery);

                    try
                    {
                        DnsRecord dns = new DnsRecord(core, this);

                        dns.Domain = domain;
                        dns.Update();
                    }
                    catch (InvalidDnsRecordException)
                    {
                        DnsRecord dns = DnsRecord.Create(core, this, domain);
                    }
                }
            }
        }

        [JsonIgnore]
        public List<UserEmail> EmailAddresses
        {
            get
            {
                return emailAddresses;
            }
        }

        protected User(Core core)
            : base(core)
        {
        }

        public User(Core core, long userId)
            : this(core, userId, UserLoadOptions.Info)
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
                query.AddFields(User.GetFieldsPrefixed(core, typeof(User)));
                query.AddCondition("`user_keys`.`user_id`", userId);

                if ((loadOptions & UserLoadOptions.Info) == UserLoadOptions.Info)
                {
                    containsInfoData = true;

                    query.AddJoin(JoinTypes.Inner, UserInfo.GetTable(typeof(UserInfo)), "user_id", "user_id");
                    query.AddFields(UserInfo.GetFieldsPrefixed(core, typeof(UserInfo)));

                    /*if ((loadOptions & UserLoadOptions.Icon) == UserLoadOptions.Icon)
                    {
                        containsIconData = true;

                        query.AddJoin(JoinTypes.Left, new DataField("user_info", "user_icon"), new DataField("gallery_items", "gallery_item_id"));
                        query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                        query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
                    }*/
                }

                if ((loadOptions & UserLoadOptions.Profile) == UserLoadOptions.Profile)
                {
                    containsProfileData = true;

                    query.AddJoin(JoinTypes.Inner, UserProfile.GetTable(typeof(UserProfile)), "user_id", "user_id");
                    query.AddFields(UserProfile.GetFieldsPrefixed(core, typeof(UserProfile)));

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
            : this (core, userName, UserLoadOptions.Info)
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
                    LoadItem("user_name_lower", userName.ToLower(), true);
                }
                catch (InvalidItemException)
                {
                    throw new InvalidUserException();
                }
            }
            else
            {
                SelectQuery query = new SelectQuery(User.GetTable(typeof(User)));
                query.AddFields(User.GetFieldsPrefixed(core, typeof(User)));
                query.AddCondition(new DataField("user_keys", "user_name_lower"), userName.ToLower());

                if ((loadOptions & UserLoadOptions.Info) == UserLoadOptions.Info)
                {
                    containsInfoData = true;

                    query.AddJoin(JoinTypes.Inner, UserInfo.GetTable(typeof(UserInfo)), "user_id", "user_id");
                    query.AddFields(UserInfo.GetFieldsPrefixed(core, typeof(UserInfo)));

                    /*if ((loadOptions & UserLoadOptions.Icon) == UserLoadOptions.Icon)
                    {
                        containsIconData = true;

                        query.AddJoin(JoinTypes.Left, new DataField("user_info", "user_icon"), new DataField("gallery_items", "gallery_item_id"));
                        query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                        query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
                    }*/
                }

                if ((loadOptions & UserLoadOptions.Profile) == UserLoadOptions.Profile)
                {
                    containsProfileData = true;

                    query.AddJoin(JoinTypes.Inner, UserProfile.GetTable(typeof(UserProfile)), "user_id", "user_id");
                    query.AddFields(UserProfile.GetFieldsPrefixed(core, typeof(UserProfile)));

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

        public User(Core core, DataRow userRow)
            : base(core)
        {
            UserLoadOptions loadOptions = UserLoadOptions.All;
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
            }
            else
            {
                throw new InvalidUserException();
            }
        }

        public User(Core core, System.Data.Common.DbDataReader userRow)
            : base(core)
        {
            UserLoadOptions loadOptions = UserLoadOptions.All;
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
            }
            else
            {
                throw new InvalidUserException();
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
            }
            else
            {
                throw new InvalidUserException();
            }
        }

        public User(Core core, System.Data.Common.DbDataReader userRow, UserLoadOptions loadOptions)
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
            }
            else
            {
                throw new InvalidUserException();
            }
        }

        private new void loadItemInfo(Type type, DataRow userRow)
        {
            if (type == typeof(User))
            {
                loadUser(userRow);
            }
            else
            {
                base.loadItemInfo(type, userRow);
            }
        }

        private new void loadItemInfo(Type type, System.Data.Common.DbDataReader userRow)
        {
            if (type == typeof(User))
            {
                loadUser(userRow);
            }
            else
            {
                base.loadItemInfo(type, userRow);
            }
        }

        protected override void loadItemInfo(DataRow userRow)
        {
            loadUser(userRow);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader userRow)
        {
            loadUser(userRow);
        }

        protected void loadUser(DataRow userRow)
        {
            loadValue(userRow, "user_id", out userId);
            loadValue(userRow, "user_name", out userName);
            loadValue(userRow, "user_name_lower", out userNameLower);
            loadValue(userRow, "user_domain", out domain);
            loadValue(userRow, "user_name_first", out userNameFirstCharacter);
            loadValue(userRow, "user_simple_permissions", out simplePermissions);

            itemLoaded(userRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
            core.PrimitiveCache.RegisterItem((Primitive)this);
        }

        protected void loadUser(System.Data.Common.DbDataReader userRow)
        {
            loadValue(userRow, "user_id", out userId);
            loadValue(userRow, "user_name", out userName);
            loadValue(userRow, "user_name_lower", out userNameLower);
            loadValue(userRow, "user_domain", out domain);
            loadValue(userRow, "user_name_first", out userNameFirstCharacter);
            loadValue(userRow, "user_simple_permissions", out simplePermissions);

            itemLoaded(userRow);
#if DEBUG
            Stopwatch httpTimer = new Stopwatch();
            httpTimer.Start();
#endif
            core.ItemCache.RegisterItem((NumberedItem)this);
            core.PrimitiveCache.RegisterItem((Primitive)this);
#if DEBUG
            httpTimer.Stop();
            if (HttpContext.Current != null && core.Session != null && core.Session.SessionMethod != SessionMethods.OAuth)
            {
                HttpContext.Current.Response.Write(string.Format("<!-- User {1} cached in {0} -->\r\n", httpTimer.ElapsedTicks / 10000000.0, userName));
            }
#endif
        }

        void User_ItemLoad()
        {
            ItemUpdated += new EventHandler(User_ItemUpdated);
            ItemDeleted += new ItemDeletedEventHandler(User_ItemDeleted);
            OnCommentPosted += new CommentHandler(User_CommentPosted);
        }

        bool User_CommentPosted(CommentPostedEventArgs e)
        {
            ApplicationEntry ae = core.GetApplication("GuestBook");
            /*ae.SendNotification(core, this, e.Comment.ItemKey, string.Format("[user]{0}[/user] commented on your guest book.", e.Poster.Id), string.Format("[quote=\"[iurl={0}]{1}[/iurl]\"]{2}[/quote]",
                e.Comment.BuildUri(this), e.Poster.DisplayName, e.Comment.Body));*/

            ae.QueueNotifications(core, e.Comment.ItemKey, "notifyUserComment");

            return true;
        }

        public void CommentPosted(CommentPostedEventArgs e)
        {
            if (OnCommentPosted != null)
            {
                OnCommentPosted(e);
            }
        }

        void User_ItemUpdated(object sender, EventArgs e)
        {
            core.Search.UpdateIndex(this);
        }

        void User_ItemDeleted(object sender, ItemDeletedEventArgs e)
        {
            core.Search.DeleteFromIndex(this);
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

        protected void loadUserInfo(System.Data.Common.DbDataReader userRow)
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

        protected void loadUserIcon(System.Data.Common.DbDataReader userRow)
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

        public List<long> GetSubscriptionUserIds()
        {
            return GetSubscriptionUserIds(255);
        }

        public List<long> GetSubscriptionUserIds(int count)
        {
            List<long> subscriptionIds = new List<long>();

            SelectQuery query = Subscription.GetSelectQueryStub(core, typeof(Subscription));
            query.AddCondition("user_id", userId);
            query.AddCondition("subscription_item_type_id", ItemType.GetTypeId(core, typeof(User)));
            query.AddSort(SortOrder.Descending, "subscription_time_ut");
            query.LimitCount = count;

            System.Data.Common.DbDataReader subscriptionsReader = db.ReaderQuery(query);

            while (subscriptionsReader.Read())
            {
                subscriptionIds.Add((long)subscriptionsReader["subscription_item_id"]);
            }

            subscriptionsReader.Close();
            subscriptionsReader.Dispose();

            return subscriptionIds;
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

            System.Data.Common.DbDataReader friendsReader = db.ReaderQuery(query);

            while (friendsReader.Read())
            {
                friendIds.Add((long)friendsReader["relation_you"]);
            }

            friendsReader.Close();
            friendsReader.Dispose();

            return friendIds;
        }

        public List<long> GetFriendsWithMeIds()
        {
            return GetFriendsWithMeIds(255);
        }

        public List<long> GetFriendsWithMeIds(int count)
        {
            List<long> friendIds = new List<long>();

            SelectQuery query = new SelectQuery("user_relations uf");
            query.AddFields("uf.relation_me");
            query.AddCondition("uf.relation_you", userId);
            query.AddCondition("uf.relation_type", "FRIEND");
            query.AddSort(SortOrder.Ascending, "(uf.relation_order - 1)");
            query.AddSort(SortOrder.Ascending, "relation_time_ut");
            query.LimitCount = count;

            System.Data.Common.DbDataReader friendsReader = db.ReaderQuery(query);

            while (friendsReader.Read())
            {
                friendIds.Add((long)friendsReader["relation_me"]);
            }

            friendsReader.Close();
            friendsReader.Dispose();

            return friendIds;
        }

        /// <summary>
        /// return a maximum of the first 255
        /// </summary>
        /// <returns></returns>
        public List<Friend> GetFriends()
        {
            return GetFriends(1, 255, null);
        }

        public List<Friend> GetFriends(string namePart)
        {
            List<Friend> friends = new List<Friend>();

            SelectQuery query = new SelectQuery("user_relations");
            query.AddFields(User.GetFieldsPrefixed(core, typeof(User)));
            query.AddFields(UserInfo.GetFieldsPrefixed(core, typeof(UserInfo)));
            query.AddFields(UserProfile.GetFieldsPrefixed(core, typeof(UserProfile)));
            query.AddFields(UserRelation.GetFieldsPrefixed(core, typeof(UserRelation)));
            query.AddField(new DataField("gallery_items", "gallery_item_uri"));
            query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
            query.AddFields(ItemInfo.GetFieldsPrefixed(core, typeof(ItemInfo)));
            query.AddJoin(JoinTypes.Inner, User.GetTable(typeof(User)), "relation_you", "user_id");
            query.AddJoin(JoinTypes.Inner, UserInfo.GetTable(typeof(UserInfo)), "relation_you", "user_id");
            query.AddJoin(JoinTypes.Inner, UserProfile.GetTable(typeof(UserProfile)), "relation_you", "user_id");
            query.AddJoin(JoinTypes.Left, new DataField("user_info", "user_icon"), new DataField("gallery_items", "gallery_item_id"));

            TableJoin join = query.AddJoin(JoinTypes.Left, new DataField(typeof(UserRelation), "relation_you"), new DataField(typeof(ItemInfo), "info_item_id"));
            join.AddCondition(new DataField(typeof(ItemInfo), "info_item_type_id"), ItemKey.GetTypeId(core, typeof(User)));

            query.AddCondition("relation_me", userId);
            query.AddCondition("relation_type", "FRIEND");
            QueryCondition qc1 = query.AddCondition(new DataField("user_info", "user_name"), ConditionEquality.Like, namePart + "%");
            qc1.AddCondition(ConditionRelations.Or, new DataField("user_profile", "profile_name_first"), ConditionEquality.Like, namePart + "%");
            qc1.AddCondition(ConditionRelations.Or, new DataField("user_info", "user_name_display"), ConditionEquality.Like, namePart + "%");
            query.AddSort(SortOrder.Ascending, "(relation_order - 1)");
            query.LimitCount = 10;

            System.Data.Common.DbDataReader friendsReader = db.ReaderQuery(query);

            while (friendsReader.Read())
            {
                friends.Add(new Friend(core, friendsReader, UserLoadOptions.All));
            }

            friendsReader.Close();
            friendsReader.Dispose();

            return friends;
        }

        public List<Friend> GetFriends(int page, int perPage, bool sortOnline)
        {
            return GetFriends(page, perPage, null, sortOnline);
        }

        public List<Friend> GetFriends(int page, int perPage, string filter)
        {
            return GetFriends(page, perPage, filter, false);
        }

        public List<Friend> GetFriends(int page, int perPage, string filter, bool sortOnline)
        {
            List<Friend> friends = new List<Friend>();

            SelectQuery query = new SelectQuery("user_relations");
            query.AddFields(User.GetFieldsPrefixed(core, typeof(User)));
            query.AddFields(UserInfo.GetFieldsPrefixed(core, typeof(UserInfo)));
            query.AddFields(UserProfile.GetFieldsPrefixed(core, typeof(UserProfile)));
            query.AddFields(UserRelation.GetFieldsPrefixed(core, typeof(UserRelation)));
            query.AddField(new DataField("gallery_items", "gallery_item_uri"));
            query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
            query.AddFields(ItemInfo.GetFieldsPrefixed(core, typeof(ItemInfo)));
            query.AddJoin(JoinTypes.Inner, User.GetTable(typeof(User)), "relation_you", "user_id");
            query.AddJoin(JoinTypes.Inner, UserInfo.GetTable(typeof(UserInfo)), "relation_you", "user_id");
            query.AddJoin(JoinTypes.Inner, UserProfile.GetTable(typeof(UserProfile)), "relation_you", "user_id");
            query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_country"), new DataField("countries", "country_iso"));
            query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_religion"), new DataField("religions", "religion_id"));
            query.AddJoin(JoinTypes.Left, new DataField("user_info", "user_icon"), new DataField("gallery_items", "gallery_item_id"));

            TableJoin join = query.AddJoin(JoinTypes.Left, new DataField(typeof(UserRelation), "relation_you"), new DataField(typeof(ItemInfo), "info_item_id"));
            join.AddCondition(new DataField(typeof(ItemInfo), "info_item_type_id"), ItemKey.GetTypeId(core, typeof(User)));

            query.AddCondition("relation_me", userId);
            query.AddCondition("relation_type", "FRIEND");
            if ((!string.IsNullOrEmpty(filter)) && filter.Length == 1)
            {
                query.AddCondition(new DataField(typeof(User), "user_name_first"), filter);
            }
            if (sortOnline)
            {
                query.AddSort(SortOrder.Descending, "user_last_visit_ut");
                query.AddSort(SortOrder.Ascending, new QueryCondition("relation_order", ConditionEquality.Equal, 0));
            }
            else
            {
                query.AddSort(SortOrder.Ascending, new QueryCondition("relation_order", ConditionEquality.Equal, 0));
            }
            query.LimitStart = (page - 1) * perPage;
            query.LimitCount = perPage;
            if (sortOnline)
            {
                query.LimitOrder = SortOrder.Descending;
            }

            System.Data.Common.DbDataReader friendsReader = db.ReaderQuery(query);

            while (friendsReader.Read())
            {
                friends.Add(new Friend(core, friendsReader, UserLoadOptions.All));
            }

            friendsReader.Close();
            friendsReader.Dispose();

            return friends;
        }

        public List<UserRelation> GetFriendsBirthdays(long startTimeRaw, long endTimeRaw)
        {
            DateTime st = core.Tz.DateTimeFromMysql(startTimeRaw - 24 * 60 * 60);
            DateTime et = core.Tz.DateTimeFromMysql(endTimeRaw + 48 * 60 * 60);

            List<UserRelation> friends = new List<UserRelation>();

            SelectQuery query = UserRelation.GetSelectQueryStub(core, UserLoadOptions.All);
            query.AddCondition("relation_me", userId);
            query.AddCondition("relation_type", "FRIEND");
            query.AddCondition("profile_date_of_birth_month_cache * 31 + profile_date_of_birth_day_cache", ConditionEquality.GreaterThanEqual, st.Month * 31 + st.Day);
            query.AddCondition("profile_date_of_birth_month_cache * 31 + profile_date_of_birth_day_cache", ConditionEquality.LessThanEqual, et.Month * 31 + et.Day);

            System.Data.Common.DbDataReader friendsReader = db.ReaderQuery(query);

            while (friendsReader.Read())
            {
                UserRelation friend = new UserRelation(core, friendsReader, UserLoadOptions.All);
                UnixTime tz = new UnixTime(core, friend.UserInfo.TimeZoneCode);
                DateTime dob = new DateTime(st.Year, friend.Profile.DateOfBirth.Month, friend.Profile.DateOfBirth.Day);
                long dobUt = tz.GetUnixTimeStamp(dob);

                if ((dobUt >= startTimeRaw && dobUt <= endTimeRaw) ||
                    (dobUt + 24 * 60 * 60 - 1 >= startTimeRaw && dobUt + 24 * 60 * 60 - 1 <= endTimeRaw))
                {
                    friends.Add(friend);
                }
            }

            friendsReader.Close();
            friendsReader.Dispose();

            return friends;
        }

        public List<UserRelation> GetFriendsOnline()
        {
            return GetFriendsOnline(1, 255);
        }

        public List<UserRelation> GetFriendsOnline(int page, int perPage)
        {
            List<UserRelation> friends = new List<UserRelation>();

            SelectQuery query = UserRelation.GetSelectQueryStub(core, UserLoadOptions.All);
            query.AddCondition("relation_me", userId);
            query.AddCondition("relation_type", "FRIEND");
            // last 15 minutes
            query.AddCondition("UNIX_TIMSTAMP() - ui.user_last_visit_ut", ConditionEquality.LessThan, 900);
            query.AddSort(SortOrder.Ascending, "(uf.relation_order - 1)");
            query.LimitStart = (page - 1) * perPage;
            query.LimitCount = perPage;

            System.Data.Common.DbDataReader friendsReader = db.ReaderQuery(query);

            while (friendsReader.Read())
            {
                friends.Add(new UserRelation(core, friendsReader, UserLoadOptions.All));
            }

            friendsReader.Close();
            friendsReader.Dispose();

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

            SelectQuery query = UserRelation.GetSelectQueryStub(core, UserLoadOptions.All);
            query.AddCondition("relation_me", userId);
            query.AddCondition("relation_type", "FRIEND");

            // here we are grouping the condition to do an OR between these two conditions only
            QueryCondition qc = query.AddCondition("user_info.user_name", ConditionEquality.Like, QueryCondition.EscapeLikeness(needle) + "%");
            qc.AddCondition(ConditionRelations.Or, "user_info.user_name_display", ConditionEquality.Like, QueryCondition.EscapeLikeness(needle) + "%");

            query.AddSort(SortOrder.Ascending, "(relation_order - 1)");
            query.LimitStart = (page - 1) * perPage;
            query.LimitCount = perPage;

            System.Data.Common.DbDataReader friendsReader = db.ReaderQuery(query);

            while (friendsReader.Read())
            {
                friends.Add(new UserRelation(core, friendsReader, UserLoadOptions.All));
            }

            friendsReader.Close();
            friendsReader.Dispose();

            return friends;
        }

        public Relation GetRelations(ItemKey member)
        {
            Relation returnValue = Relation.None;

            if (member == null)
            {
                return Relation.None;
            }

            if (member.Id == userId)
            {
                return Relation.Owner;
            }

            SelectQuery query = new SelectQuery(typeof(UserRelation));
            query.AddField(new DataField(typeof(UserRelation), "relation_type"));
            query.AddField(new DataField(typeof(UserRelation), "relation_order"));
            query.AddCondition(new DataField(typeof(UserRelation), "relation_me"), userId);
            query.AddCondition(new DataField(typeof(UserRelation), "relation_you"), member.Id);

            System.Data.Common.DbDataReader relationReader = db.ReaderQuery(query);

            /*string.Format("SELECT relation_type, relation_order FROM user_relations WHERE relation_me = {0} AND relation_you = {1};",
                    userId, member.Id)*/

            while (relationReader.Read())
            {
                if ((string)relationReader["relation_type"] == "FRIEND")
                {
                    returnValue |= Relation.Friend;
                }

                if ((string)relationReader["relation_type"] == "FAMILY")
                {
                    returnValue |= Relation.Family;
                }

                if ((string)relationReader["relation_type"] == "BLOCKED")
                {
                    returnValue |= Relation.Blocked;
                }
            }

            relationReader.Close();
            relationReader.Dispose();

            return returnValue;
        }

        public bool IsFriend(ItemKey member)
        {
            Relation viewerRelation;
            if (!sessionRelationsSet)
            {
                if (member == null)
                {
                    viewerRelation = Relation.None;
                }
                else
                {
                    viewerRelation = GetRelations(member);
                }
                sessionRelations = viewerRelation;
                sessionRelationsSet = true;
            }
            else
            {
                viewerRelation = sessionRelations;
            }

            if ((viewerRelation & Relation.Friend) == Relation.Friend)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsFamily(ItemKey member)
        {
            Relation viewerRelation;
            if (!sessionRelationsSet)
            {
                if (member == null)
                {
                    viewerRelation = Relation.None;
                }
                else
                {
                    viewerRelation = GetRelations(member);
                }
                sessionRelations = viewerRelation;
                sessionRelationsSet = true;
            }
            else
            {
                viewerRelation = sessionRelations;
            }

            if ((viewerRelation & Relation.Family) == Relation.Family)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsBlocked(ItemKey member)
        {
            Relation viewerRelation;
            if (!sessionRelationsSet)
            {
                if (member == null)
                {
                    viewerRelation = Relation.None;
                }
                else
                {
                    viewerRelation = GetRelations(member);
                }
                sessionRelations = viewerRelation;
                sessionRelationsSet = true;
            }
            else
            {
                viewerRelation = sessionRelations;
            }

            if ((viewerRelation & Relation.Blocked) == Relation.Blocked)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static SelectQuery GetSelectQueryStub(Core core, UserLoadOptions loadOptions)
        {
            long typeId = ItemType.GetTypeId(core, typeof(User));
            /*if (loadOptions == UserLoadOptions.All && QueryCache.HasQuery(typeId))
            {
                return (SelectQuery)QueryCache.GetQuery(typeof(User), typeId);
            }
            else*/
            {
#if DEBUG
                Stopwatch httpTimer = new Stopwatch();
                httpTimer.Start();
#endif
                SelectQuery query = new SelectQuery(GetTable(typeof(User)));
                query.AddFields(User.GetFieldsPrefixed(core, typeof(User)));
                query.AddFields(ItemInfo.GetFieldsPrefixed(core, typeof(ItemInfo)));
                TableJoin join = query.AddJoin(JoinTypes.Left, new DataField(typeof(User), "user_id"), new DataField(typeof(ItemInfo), "info_item_id"));
                join.AddCondition(new DataField(typeof(ItemInfo), "info_item_type_id"), ItemKey.GetTypeId(core, typeof(User)));

                if ((loadOptions & UserLoadOptions.Info) == UserLoadOptions.Info)
                {
                    query.AddJoin(JoinTypes.Inner, UserInfo.GetTable(typeof(UserInfo)), "user_id", "user_id");
                    query.AddFields(UserInfo.GetFieldsPrefixed(core, typeof(UserInfo)));

                    /*if ((loadOptions & UserLoadOptions.Icon) == UserLoadOptions.Icon)
                    {
                        query.AddJoin(JoinTypes.Left, new DataField("user_info", "user_icon"), new DataField("gallery_items", "gallery_item_id"));
                        query.AddField(new DataField("gallery_items", "gallery_item_uri"));
                        query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
                    }*/
                }

                if ((loadOptions & UserLoadOptions.Profile) == UserLoadOptions.Profile)
                {
                    query.AddJoin(JoinTypes.Inner, UserProfile.GetTable(typeof(UserProfile)), "user_id", "user_id");
                    query.AddFields(UserProfile.GetFieldsPrefixed(core, typeof(UserProfile)));

                    // Countries are cached separately as they do not change
                    /*if ((loadOptions & UserLoadOptions.Country) == UserLoadOptions.Country)
                    {
                        query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_country"), new DataField("countries", "country_iso"));
                    }*/

                    if ((loadOptions & UserLoadOptions.Religion) == UserLoadOptions.Religion)
                    {
                        query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_religion"), new DataField("religions", "religion_id"));
                    }
                }

#if DEBUG
                httpTimer.Stop();
                if (HttpContext.Current != null && core.Session != null && core.Session.SessionMethod != SessionMethods.OAuth)
                {
                    HttpContext.Current.Response.Write(string.Format("<!-- Build user query stub in {0} -->\r\n", httpTimer.ElapsedTicks / 10000000.0));
                }
#endif

                /*if (loadOptions == UserLoadOptions.All)
                {
                    QueryCache.AddQueryToCache(typeId, query);
                }*/
                return query;
            }
        }

        public static SelectQuery User_GetSelectQueryStub(Core core)
        {
            return GetSelectQueryStub(core, UserLoadOptions.All);
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
            if (core == null)
            {
                throw new NullCoreException();
            }

            Mysql db = core.Db;
            SessionState session = core.Session;

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
			query.AddField("user_name_lower", userName.ToLower());
            query.AddField("user_domain", "");
            query.AddField("user_name_first", userName[0].ToString().ToLower());

            db.BeginTransaction();
            long userId = db.Query(query);

            if (userId < 0)
            {
                db.RollBackTransaction();
                throw new InvalidUserException();
            }

            query = new InsertQuery("user_info");
            query.AddField("user_id", userId);
            query.AddField("user_name", userName);
            query.AddField("user_alternate_email", eMail);
            query.AddField("user_password", password);
            query.AddField("user_reg_date_ut", UnixTime.UnixTimeStamp());
            query.AddField("user_activate_code", activateKey);
            query.AddField("user_reg_ip", session.IPAddress.ToString());
            query.AddField("user_home_page", "/profile");
            query.AddField("user_bytes", 0);
            query.AddField("user_status_messages", 0);
            query.AddField("user_show_bbcode", 0x07);
            query.AddField("user_show_custom_styles", true);
            query.AddField("user_email_notifications", true);
            query.AddField("user_new_password", "");
            query.AddField("user_last_visit_ut", -30610224000L);
            query.AddField("user_language", "en");

            if (db.Query(query) < 0)
            {
                throw new InvalidUserException();
            }

            query = new InsertQuery("user_profile");
            query.AddField("user_id", userId);
            query.AddField("profile_date_of_birth_ut", -30610224000L);
            // TODO: ACLs

            db.Query(query);

            User newUser = new User(core, userId);
            UserEmail registrationEmail = UserEmail.Create(core, newUser, eMail, EmailAddressTypes.Personal, true);

            // Install a couple of applications
            try
            {
                ApplicationEntry profileAe = new ApplicationEntry(core, "Profile");
                profileAe.Install(core, newUser);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry mailAe = new ApplicationEntry(core, "Mail");
                mailAe.Install(core, newUser);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry galleryAe = new ApplicationEntry(core, "Gallery");
                galleryAe.Install(core, newUser);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry guestbookAe = new ApplicationEntry(core, "GuestBook");
                guestbookAe.Install(core, newUser);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry groupsAe = new ApplicationEntry(core, "Groups");
                groupsAe.Install(core, newUser);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry networksAe = new ApplicationEntry(core, "Networks");
                networksAe.Install(core, newUser);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry calendarAe = new ApplicationEntry(core, "Calendar");
                calendarAe.Install(core, newUser);
            }
            catch
            {
            }

            string activateUri = string.Format("{0}register/?mode=activate&id={1}&key={2}",
                core.Hyperlink.Uri, userId, activateKey);

            Template emailTemplate = new Template(core.Http.TemplateEmailPath, "registration_welcome.html");

            emailTemplate.Parse("SITE_TITLE", core.Settings.SiteTitle);
            emailTemplate.Parse("U_SITE", core.Hyperlink.StripSid(core.Hyperlink.AppendAbsoluteSid(core.Hyperlink.BuildHomeUri())));
            emailTemplate.Parse("TO_NAME", userName);
            emailTemplate.Parse("U_ACTIVATE", activateUri);
            emailTemplate.Parse("USERNAME", userName);
            emailTemplate.Parse("PASSWORD", passwordClearText);

            core.Email.SendEmail(eMail, "Activate your account. Welcome to " + core.Settings.SiteTitle, emailTemplate);

            Access.CreateAllGrantsForOwner(core, newUser);
            Access.CreateGrantForPrimitive(core, newUser, User.GetEveryoneGroupKey(core), "VIEW");
            Access.CreateGrantForPrimitive(core, newUser, User.GetEveryoneGroupKey(core), "VIEW_STATUS");
            Access.CreateGrantForPrimitive(core, newUser, Friend.GetFriendsGroupKey(core), "COMMENT");
            Access.CreateGrantForPrimitive(core, newUser, Friend.GetFriendsGroupKey(core), "VIEW_FRIENDS");
            Access.CreateGrantForPrimitive(core, newUser, Friend.GetFamilyGroupKey(core), "VIEW_FAMILY");

            core.Search.Index(newUser);

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
            disallowedNames.Add(WebConfigurationManager.AppSettings["boxsocial-title"].ToLower());
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
            disallowedNames.Add("likes");
            disallowedNames.Add("dev");
            disallowedNames.Add("developer");
            disallowedNames.Add("developers");
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
            disallowedNames.Add("musician");
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
            disallowedNames.Add("rss");
            disallowedNames.Add("tasks");
            disallowedNames.Add("application");
            disallowedNames.Add("applications");
            disallowedNames.Add("error-handler");
            disallowedNames.Add("news");
            disallowedNames.Add("politian");
            disallowedNames.Add("officer");
            disallowedNames.Add("sale");
            disallowedNames.Add("donate");
            disallowedNames.Add("comedian");
            disallowedNames.Add("cart");
            disallowedNames.Add("api");
            disallowedNames.Add("oauth");
            disallowedNames.Add("oauth2");
            disallowedNames.Add("download");
            disallowedNames.Add("app");
            disallowedNames.Sort();

            if (disallowedNames.BinarySearch(userName.ToLower()) >= 0)
            {
                matches++;
            }

            if (!Regex.IsMatch(userName, @"^([A-Za-z0-9\-_\.\!~\*'&=\$]+)$"))
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

            if (userName.EndsWith(".aspx", StringComparison.Ordinal))
            {
                matches++;
            }

            if (userName.EndsWith(".asax", StringComparison.Ordinal))
            {
                matches++;
            }

            if (userName.EndsWith(".php", StringComparison.Ordinal))
            {
                matches++;
            }

            if (userName.EndsWith(".html", StringComparison.Ordinal))
            {
                matches++;
            }

            if (userName.EndsWith(".gif", StringComparison.Ordinal))
            {
                matches++;
            }

            if (userName.EndsWith(".png", StringComparison.Ordinal))
            {
                matches++;
            }

            if (userName.EndsWith(".js", StringComparison.Ordinal))
            {
                matches++;
            }

            if (userName.EndsWith(".bmp", StringComparison.Ordinal))
            {
                matches++;
            }

            if (userName.EndsWith(".jpg", StringComparison.Ordinal))
            {
                matches++;
            }

            if (userName.EndsWith(".jpeg", StringComparison.Ordinal))
            {
                matches++;
            }

            if (userName.EndsWith(".zip", StringComparison.Ordinal))
            {
                matches++;
            }

            if (userName.EndsWith(".jsp", StringComparison.Ordinal))
            {
                matches++;
            }

            if (userName.EndsWith(".cfm", StringComparison.Ordinal))
            {
                matches++;
            }

            if (userName.EndsWith(".exe", StringComparison.Ordinal))
            {
                matches++;
            }

            if (userName.EndsWith(".bat", StringComparison.Ordinal))
            {
                matches++;
            }

            if (userName.EndsWith(".do", StringComparison.Ordinal))
            {
                matches++;
            }

            if (userName.StartsWith(".", StringComparison.Ordinal))
            {
                matches++;
            }

            if (userName.EndsWith(".", StringComparison.Ordinal))
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
            if (db.Query(string.Format("SELECT user_name FROM user_keys WHERE user_name_lower = '{0}';",
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

        public static string GeneratePhoneActivationToken()
        {
            Random rand = new Random();
            string captchaString = "";

            char[] chars = { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };

            for (int i = 0; i < 6; i++)
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

        public override bool IsItemOwner(User member)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="accessBits"></param>
        /// <param name="viewer"></param>
        /// <param name="canRead"></param>
        /// <param name="canComment"></param>
        /// <param name="canCreate"></param>
        /// <param name="canChange"></param>
        public void GetCan(ushort accessBits, User viewer, out bool canRead, out bool canComment, out bool canCreate, out bool canChange)
        {
            Relation viewerRelation;
            if (!sessionRelationsSet)
            {
                viewerRelation = GetRelations(viewer.ItemKey);
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
            string path = this.UriStub;

            if (core.IsMobile)
            {
                if (parts.Count > 1)
                {
                    bool lastAbsolute = parts[parts.Count - 2][0].StartsWith("!", StringComparison.Ordinal);
                    if (!lastAbsolute)
                    {
                        for (int i = 0; i < parts.Count - 2; i++)
                        {
                            bool absolute = parts[i][0].StartsWith("!", StringComparison.Ordinal);
                            bool ignore = parts[i][0].StartsWith("*", StringComparison.Ordinal);

                            if ((!ignore) && (!absolute))
                            {
                                path += parts[i][0] + "/";
                            }
                        }
                    }

                    output += string.Format("<span class=\"breadcrumbs\"><strong>&#8249;</strong> <a href=\"{1}\">{0}</a></span>",
                        parts[parts.Count - 2][1], core.Hyperlink.AppendSid((!lastAbsolute ? path : string.Empty) + parts[parts.Count - 2][0].TrimStart(new char[] { '*', '!' })));
                }
                if (parts.Count == 1)
                {
                    output += string.Format("<span class=\"breadcrumbs\"><strong>&#8249;</strong> <a href=\"{1}\">{0}</a></span>",
                        DisplayName, core.Hyperlink.AppendSid(path));
                }
                if (parts.Count == 0)
                {
                    output += string.Format("<span class=\"breadcrumbs\"><strong>&#8249;</strong> <a href=\"{1}\">{0}</a></span>",
                        core.Prose.GetString("HOME"), core.Hyperlink.AppendCoreSid("/"));
                }
            }
            else
            {
                output = string.Format("<a href=\"{1}\">{0}</a>",
                        DisplayName, core.Hyperlink.AppendSid(path));

                for (int i = 0; i < parts.Count; i++)
                {
                    if (parts[i][0] != "")
                    {
                        bool absolute = parts[i][0].StartsWith("!", StringComparison.Ordinal);
                        bool ignore = parts[i][0].StartsWith("*", StringComparison.Ordinal);

                        output += string.Format(" <strong>&#8249;</strong> <a href=\"{1}\">{0}</a>",
                            parts[i][1], core.Hyperlink.AppendSid((!absolute ? path : string.Empty) + parts[i][0].TrimStart(new char[] { '*', '!' })));
                        if ((!ignore) && (!absolute))
                        {
                            path += parts[i][0] + "/";
                        }
                    }
                }
            }

            return output;
        }

        [JsonIgnore]
        public override string UriStub
        {
            get
            {
                if ((string.IsNullOrEmpty(domain) || core.Session.IsBot || core.IsMobile || core.Session.SessionMethod == SessionMethods.OAuth || (core.Http == null && core.Settings.UseSecureCookies) || (core.Http != null && core.Http.IsSecure) || (core.Http != null && core.Http.ForceDomain)))
                {
                    if (core.Http != null && core.Http.Domain != Hyperlink.Domain || core.Session.SessionMethod == SessionMethods.OAuth)
                    {
                        return core.Hyperlink.Uri + "user/" + UserName.ToLower() + "/";
                    }
                    else
                    {
                        return string.Format("/user/{0}/", 
                            UserName.ToLower());
                    }
                }
                else
                {
                    if (core.Http != null && domain == core.Http.Domain)
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

        [JsonIgnore]
        public override string UriStubAbsolute
        {
            get
            {
                if ((string.IsNullOrEmpty(domain) || core.Session.IsBot || core.IsMobile || core.Session.SessionMethod == SessionMethods.OAuth || (core.Http == null && core.Settings.UseSecureCookies) || (core.Http != null && core.Http.IsSecure) || (core.Http != null && core.Http.ForceDomain)))
                {
                    return core.Hyperlink.AppendAbsoluteSid(UriStub);
                }
                else
                {
                    return core.Hyperlink.AppendAbsoluteSid(string.Format("http://{0}/",
                            domain));
                }
            }
        }

        [JsonIgnore]
        public override string Uri
        {
            get
            {
                return core.Hyperlink.AppendSid(UriStub);
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

        [JsonIgnore]
        public override string AccountUriStub
        {
            get
            {
                if (Hyperlink.Domain == core.Hyperlink.CurrentDomain)
                {
                    return "/account/";
                }
                else
                {
                    return core.Hyperlink.Uri + "account/";
                }
            }
        }

        [JsonIgnore]
        public long Comments
        {
            get
            {
                return Info.Comments;
            }
        }

        [JsonIgnore]
        public SortOrder CommentSortOrder
        {
            get
            {
                return SortOrder.Descending;
            }
        }

        [JsonIgnore]
        public byte CommentsPerPage
        {
            get
            {
                return 10;
            }
        }

        // Force all inherited classes to expose themselves as base type of User
        [JsonIgnore]
        public new long TypeId
        {
            get
            {
                return ItemKey.GetTypeId(core, typeof(User));
            }
        }

        [JsonIgnore]
        public new ItemKey ItemKey
        {
            get
            {
                return new ItemKey(Id, TypeId);
            }
        }

        public new long Update()
        {
            //throw new Exception("Cannot update user key table.");
            // we will pretend we did, but we didn't
            return 1;
        }

        [JsonIgnore]
        public bool IsOnline
        {
            get
            {
                return (UserInfo.LastVisitDateRaw > UnixTime.UnixTimeStamp() - 90);
            }
        }

        [JsonIgnore]
        public override Access Access
        {
            get
            {
                if (access == null)
                {
                    access = new Access(core, this);
                }

                return access;
            }
        }

        [JsonIgnore]
        public override bool IsSimplePermissions
        {
            get
            {
                return simplePermissions;
            }
            set
            {
                if (simplePermissions != value)
                {
                    simplePermissions = value;

                    UpdateQuery uQuery = new UpdateQuery(typeof(User));
                    uQuery.AddField("user_simple_permissions", simplePermissions);
                    uQuery.AddCondition("user_id", Id);

                    db.Query(uQuery);
                }
            }
        }

        [JsonIgnore]
        public override List<AccessControlPermission> AclPermissions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsItemGroupMember(ItemKey viewer, ItemKey key)
        {
            return false;
        }

        public override List<PrimitivePermissionGroup> GetPrimitivePermissionGroups()
        {
            List<PrimitivePermissionGroup> ppgs = new List<PrimitivePermissionGroup>();

            ppgs.Add(new PrimitivePermissionGroup(User.GetCreatorKey(core), "CREATOR", null, string.Empty));
            ppgs.Add(new PrimitivePermissionGroup(User.GetEveryoneGroupKey(core), "EVERYONE", null, string.Empty));
            ppgs.Add(new PrimitivePermissionGroup(User.GetRegisteredUsersGroupKey(core), "REGISTERED_USERS", null, string.Empty));
            ppgs.Add(new PrimitivePermissionGroup(Friend.GetFriendsGroupKey(core), "FRIENDS", null, string.Empty));
            ppgs.Add(new PrimitivePermissionGroup(Friend.GetFamilyGroupKey(core), "FAMILY_MEMBERS", null, string.Empty));
            ppgs.Add(new PrimitivePermissionGroup(Friend.GetBlockedGroupKey(core), "BLOCKED_USERS", null, string.Empty));

            return ppgs;
        }

        public override List<User> GetPermissionUsers()
        {
            List<Friend> friends = GetFriends();

            List<User> users = new List<User>();

            foreach (Friend friend in friends)
            {
                users.Add(friend);
            }

            return users;
        }

        public override List<User> GetPermissionUsers(string namePart)
        {
            List<Friend> friends = GetFriends(namePart);

            List<User> users = new List<User>();

            foreach (Friend friend in friends)
            {
                users.Add(friend);
            }

            return users;
        }

        public /*override*/ List<ItemKey> GetPrimitiveDefaultViewGroups()
        {
            List<ItemKey> ppgs = new List<ItemKey>();

            ppgs.Add(User.GetEveryoneGroupKey(core));
            ppgs.Add(User.GetRegisteredUsersGroupKey(core));
            ppgs.Add(Friend.GetFriendsGroupKey(core));
            ppgs.Add(Friend.GetFamilyGroupKey(core));

            return ppgs;
        }

        public bool IsMemberOf(IPermissibleItem item, Type type, long subType)
        {
            if (ItemType.GetTypeId(core, type) == ItemType.GetTypeId(core, typeof(Friend)))
            {
                switch (subType)
                {
                    case -1:
                        if (IsFriend(core.Session.LoggedInMember.ItemKey))
                        {
                            return true;
                        }
                        break;
                    case -2:
                        if (IsFamily(core.Session.LoggedInMember.ItemKey))
                        {
                            return true;
                        }
                        break;
                    case -3:
                        if (IsBlocked(core.Session.LoggedInMember.ItemKey))
                        {
                            return true;
                        }
                        break;
                }
            }

            if (ItemType.GetTypeId(core, type) == ItemType.GetTypeId(core, typeof(User)))
            {
                switch (subType)
                {
                    case -1:
                        if (item.Owner.Id == core.LoggedInMemberId)
                        {
                            return true;
                        }
                        break;
                    case -2:
                        return true;
                    case -3:
                        if (core.Session.IsLoggedIn)
                        {
                            return true;
                        }
                        break;
                }
            }

            return false;
        }

        [JsonIgnore]
        private new IPermissibleItem PermissiveParent
        {
            get
            {
                return null;
            }
        }

        [JsonIgnore]
        public ItemKey PermissiveParentKey
        {
            get
            {
                return null;
            }
        }

        public override bool GetIsMemberOfPrimitive(ItemKey viewer, ItemKey primitiveKey)
        {
            if (viewer == null)
            {
                return false;
            }

            if (primitiveKey.TypeId == ItemType.GetTypeId(core, typeof(Friend)))
            {
                switch (primitiveKey.Id)
                {
                    case -1:
                        if (viewer.Id == -1)
                        {
                            return true;
                        }
                        if (IsFriend(viewer))
                        {
                            return true;
                        }
                        break;
                    case -2:
                        if (viewer.Id == -2)
                        {
                            return true;
                        }
                        if (IsFamily(viewer))
                        {
                            return true;
                        }
                        break;
                    case -3:
                        if (IsBlocked(viewer))
                        {
                            return true;
                        }
                        break;
                }
            }

            if (primitiveKey.TypeId == ItemType.GetTypeId(core, typeof(User)))
            {
                switch (primitiveKey.Id)
                {
                    case -1:
                        if (Id == viewer.Id)
                        {
                            return true;
                        }
                        break;
                    case -2:
                        return true;
                    case -3:
                        if (core.Session.IsLoggedIn)
                        {
                            return true;
                        }
                        break;
                    default:
                        if (Id == viewer.Id && Id == primitiveKey.Id)
                        {
                            return true;
                        }
                        break;
                }
            }

            return false;
        }

        public override bool CanEditPermissions()
        {
            if (Id == core.LoggedInMemberId)
            {
                return true;
            }

            return false;
        }

        public override bool CanEditItem()
        {
            if (Id == core.LoggedInMemberId)
            {
                return true;
            }

            return false;
        }

        public override bool CanDeleteItem()
        {
            if (Id == core.LoggedInMemberId)
            {
                return true;
            }

            return false;
        }

        public override bool GetDefaultCan(string permission, ItemKey viewer)
        {
            return false;
        }

        [JsonIgnore]
        public override string DisplayTitle
        {
            get
            {
                return "User: " + DisplayName + " (" + UserName + ")";
            }
        }

        public override string ParentPermissionKey(Type parentType, string permission)
        {
            return permission;
        }

        public static ItemKey GetCreatorKey(Core core)
        {
            return new ItemKey(-1, ItemType.GetTypeId(core, typeof(User)));
        }

        public static ItemKey GetEveryoneGroupKey(Core core)
        {
            return new ItemKey(-2, ItemType.GetTypeId(core, typeof(User)));
        }

        public static ItemKey GetRegisteredUsersGroupKey(Core core)
        {
            return new ItemKey(-3, ItemType.GetTypeId(core, typeof(User)));
        }

        [JsonIgnore]
        public string IndexingString
        {
            get
            {
                return UserName + " " + Profile.FirstName + " " + Profile.MiddleName + " " + Profile.LastName;
            }
        }

        [JsonIgnore]
        public string IndexingTitle
        {
            get
            {
                return DisplayName;
            }
        }

        [JsonIgnore]
        public string IndexingTags
        {
            get
            {
                return string.Empty;
            }
        }

        public Template RenderPreview()
        {
            Template template = new Template("search_result.user.html");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            template.Parse("USER_DISPLAY_NAME", DisplayName);
            template.Parse("ICON", Icon);
            template.Parse("TILE", Tile);
            template.Parse("U_PROFILE", Uri);
            template.Parse("JOIN_DATE", core.Tz.DateTimeToString(UserInfo.GetRegistrationDate(core.Tz)));
            template.Parse("USER_AGE", Profile.AgeString);
            template.Parse("USER_COUNTRY", Profile.Country);

            if (core.Session.IsLoggedIn)
            {
                List<long> friendIds = core.Session.LoggedInMember.GetFriendIds();
                if (!friendIds.Contains(Id))
                {
                    template.Parse("U_ADD_FRIEND", core.Hyperlink.BuildAddFriendUri(Id, true));
                }
            }

            return template;
        }

        [JsonIgnore]
        public string Noun
        {
            get
            {
                return "guest book";
            }
        }

        public List<UserLink> GetLinks()
        {
            return getSubItems(typeof(UserLink)).ConvertAll<UserLink>(new Converter<Item, UserLink>(convertToUserLink));
        }

        public UserLink convertToUserLink(Item input)
        {
            return (UserLink)input;
        }

        public List<UserPhoneNumber> GetPhoneNumbers()
        {
            return getSubItems(typeof(UserPhoneNumber)).ConvertAll<UserPhoneNumber>(new Converter<Item, UserPhoneNumber>(convertToUserPhoneNumber));
        }

        public UserPhoneNumber convertToUserPhoneNumber(Item input)
        {
            return (UserPhoneNumber)input;
        }

        [JsonProperty("subscribers")]
        public long Subscribers
        {
            get
            {
                return Info.Subscribers;
            }
        }


        public Dictionary<string, string> GetNotificationActions(string verb)
        {
            Dictionary<string, string> actions = new Dictionary<string, string>();
            switch (verb)
            {
                case "relationship":
                    actions.Add("confirm-relationship", core.Prose.GetString("CONFIRM"));
                    break;
                case "friendship":
                    actions.Add("confirm-friendship", core.Prose.GetString("ADD_FRIEND"));
                    break;
            }
            return actions;
        }

        public string GetNotificationActionUrl(string action)
        {
            switch (action)
            {
                case "confirm-relationship":
                    return core.Hyperlink.BuildAccountSubModuleUri("profile", "lifestyle", "confirm-relationship", core.LoggedInMemberId);
                case "confirm-friendship":
                    return core.Hyperlink.BuildAddFriendUri(Id, false);
            }

            return string.Empty;
        }

        [JsonIgnore]
        public string Title
        {
            get
            {
                return DisplayName;
            }
        }

        public bool CanComment
        {
            get
            {
                return Access.Can("COMMENT");
            }
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
