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
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Musician
{
    public enum MusicianLoadOptions : byte
    {
        Key = 0x01,
        Info = Key | 0x02,
        Icon = Key | 0x08,
        Common = Key | Info,
        All = Key | Info | Icon,
    }

    public enum MusicianType
    {
        Musician = 1,
        Duo = 2,
        Trio = 3,
        Quartet = 4,
        Quintet = 5,
        Band = 6,
        Group = 7,
        Orchestra = 8,
        Choir = 9,
    }

    [DataTable("musicians", "MUSIC")]
    [Primitive("MUSIC", MusicianLoadOptions.All, "musician_id", "musician_slug")]
    [Permission("VIEW", "Can view the musician's profile", PermissionTypes.View)]
    [Permission("COMMENT", "Can write on the guest book", PermissionTypes.Interact)]
    public class Musician : Primitive, IPermissibleItem
    {
        [DataField("musician_id", DataFieldKeys.Primary)]
        private long musicianId;
        [DataField("musician_name", 63)]
        private string name;
        [DataField("musician_slug", DataFieldKeys.Unique, 63)]
        private string slug;
        [DataField("musician_name_first", DataFieldKeys.Index, 1)]
        protected string nameFirstCharacter;
        [DataField("musician_bio", MYSQL_TEXT)]
        private string biography;
        [DataField("musician_songs")]
        private long songs;
        [DataField("musician_recordings")]
        private long recordings;
        [DataField("musician_releases")]
        private long releases;
        [DataField("musician_tours")]
        private long tours;
        [DataField("musician_gigs")]
        private long gigs;
        [DataField("musician_fans")]
        private long fans;
        [DataField("musician_members")]
        private long members;
        [DataField("musician_genre")]
        private long genre;
        [DataField("musician_subgenre")]
        private long subgenre;
        [DataField("musician_home_page", MYSQL_TEXT)]
        private string homepage;
        [DataField("musician_icon")]
        private long displayPictureId;

        private Access access;

        private Dictionary<User, bool> musicianMemberCache = new Dictionary<User, bool>();
        private string iconUri = string.Empty;

        public long MusicianId
        {
            get
            {
                return musicianId;
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

        public string Homepage
        {
            get
            {
                return homepage;
            }
            set
            {
                SetProperty("homepage", value);
            }
        }

        public long DisplayPictureId
        {
            get
            {
                return displayPictureId;
            }
            set
            {
                SetProperty("displayPictureId", value);
            }
        }

        public long GenreRaw
        {
            get
            {
                return genre;
            }
            set
            {
                SetProperty("genre", value);
            }
        }

        public long SubGenreRaw
        {
            get
            {
                return subgenre;
            }
            set
            {
                SetProperty("subgenre", value);
            }
        }

        public string Icon
        {
            get
            {
                if (iconUri != null)
                {
                    return string.Format("{0}images/_icon{1}",
                        UriStub, iconUri);
                }
                else
                {
                    return "FALSE";
                }
            }
        }

        public string Thumbnail
        {
            get
            {
                if (iconUri != null)
                {
                    return string.Format("{0}images/_thumb{1}",
                        UriStub, iconUri);
                }
                else
                {
                    return "FALSE";
                }
            }
        }

        public string Tile
        {
            get
            {
                if (iconUri != null)
                {
                    return string.Format("{0}images/_tile{1}",
                        UriStub, iconUri);
                }
                else
                {
                    return "FALSE";
                }
            }
        }

        public Musician(Core core, long musicianId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Musician_ItemLoad);

            try
            {
                LoadItem(musicianId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidMusicianException();
            }
        }

        public Musician(Core core, string slug)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Musician_ItemLoad);

            try
            {
                LoadItem("musician_slug", slug);
            }
            catch (InvalidItemException ex)
            {
                throw new InvalidMusicianException();
            }
        }

        public Musician(Core core, DataRow musicianRow, MusicianLoadOptions loadOptions)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Musician_ItemLoad);

            if (musicianRow != null)
            {
                loadItemInfo(typeof(Musician), musicianRow);

                /* TODO */
                /*if ((loadOptions & MusicianLoadOptions.Info) == MusicianLoadOptions.Info)
                {
                    musicianInfo = new UserGroupInfo(core, groupRow);
                }/*/

                if ((loadOptions & MusicianLoadOptions.Icon) == MusicianLoadOptions.Icon)
                {
                    loadIcon(musicianRow);
                }
            }
            else
            {
                throw new InvalidMusicianException();
            }
        }

        void Musician_ItemLoad()
        {
        }

        protected void loadIcon(DataRow musicianRow)
        {
            if (!(musicianRow["gallery_item_uri"] is DBNull))
            {
                iconUri = string.Format("/{0}/{1}",
                    (string)musicianRow["gallery_item_parent_path"], (string)musicianRow["gallery_item_uri"]);
            }
        }

        public List<MusicianMember> GetMembers()
        {
            List<MusicianMember> members = new List<MusicianMember>();

            SelectQuery query = new SelectQuery(typeof(MusicianMember));
            query.AddField(new DataField(typeof(MusicianMember), "user_id"));
            query.AddCondition("musician_id", musicianId);

            DataTable membersTable = db.Query(query);

            List<long> memberIds = new List<long>();

            foreach (DataRow dr in membersTable.Rows)
            {
                memberIds.Add((long)dr["user_id"]);
            }

            core.LoadUserProfiles(memberIds);

            foreach (DataRow dr in membersTable.Rows)
            {
                members.Add(new MusicianMember(core, dr));
            }

            return members;
        }

        public List<Fan> GetFans(int page, int perPage)
		{
            return GetFans(page, perPage, null);
		}

        public List<Fan> GetFans(int page, int perPage, string filter)
        {
            List<Fan> fans = new List<Fan>();

            SelectQuery query = new SelectQuery(typeof(Fan));
            query.AddJoin(JoinTypes.Inner, "user_keys", "user_id", "user_id");
            query.AddFields(Fan.GetFieldsPrefixed(typeof(Fan)));
            query.AddCondition("musician_id", musicianId);
            if (!string.IsNullOrEmpty(filter))
            {
                query.AddCondition("user_keys.user_name_first", filter);
            }
            query.AddSort(SortOrder.Ascending, "fan_date_ut");
            query.LimitStart = (page - 1) * perPage;
            query.LimitCount = perPage;

            DataTable fansTable = db.Query(query);

            List<long> fanIds = new List<long>();

            foreach (DataRow dr in fansTable.Rows)
            {
                fanIds.Add((long)dr["user_id"]);
            }

            core.LoadUserProfiles(fanIds);

            foreach (DataRow dr in fansTable.Rows)
            {
                fans.Add(new Fan(core, dr));
            }

            return fans;
        }

        public List<Tour> GetTours()
        {
            return getSubItems(typeof(Tour), true).ConvertAll<Tour>(new Converter<Item, Tour>(convertToTour));
        }

        public Tour convertToTour(Item input)
        {
            return (Tour)input;
        }

        public List<Gig> GetGigs()
        {
            return getSubItems(typeof(Gig), true).ConvertAll<Gig>(new Converter<Item, Gig>(convertToGig));
        }

        public Gig convertToGig(Item input)
        {
            return (Gig)input;
        }

        public List<Song> GetSongs()
        {
            return getSubItems(typeof(Song), true).ConvertAll<Song>(new Converter<Item, Song>(convertToSong));
        }

        public Song convertToSong(Item input)
        {
            return (Song)input;
        }

        public static Musician Create(Core core, string title, string slug)
        {
            Mysql db = core.Db;
            SessionState session = core.Session;

            if (core.Session.LoggedInMember == null)
            {
                return null;
            }

            if (!CheckMusicianNameUnique(core, slug))
            {
                return null;
            }

            db.BeginTransaction();
            InsertQuery iQuery = new InsertQuery(Musician.GetTable(typeof(Musician)));
            iQuery.AddField("musician_name", title);
            iQuery.AddField("musician_slug", slug);
            iQuery.AddField("musician_name_first", title.ToLower()[0]);

            long musicianId = db.Query(iQuery);

            db.UpdateQuery(string.Format("INSERT INTO musician_members (user_id, musician_id, member_date_ut) VALUES ({0}, {1}, UNIX_TIMESTAMP())",
                session.LoggedInMember.UserId, musicianId));

            Musician newMusician = new Musician(core, musicianId);

            // Install a couple of applications
            /*try
            {
                ApplicationEntry profileAe = new ApplicationEntry(core, null, "Profile");
                profileAe.Install(core, newMusician);
            }
            catch (Exception ex)
            {
                HttpContext.Current.Response.Write(ex.ToString());
            }*/

            try
            {
                ApplicationEntry musicianAe = new ApplicationEntry(core, null, "Musician");
                musicianAe.Install(core, newMusician);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry galleryAe = new ApplicationEntry(core, null, "Gallery");
                galleryAe.Install(core, newMusician);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry guestbookAe = new ApplicationEntry(core, null, "GuestBook");
                guestbookAe.Install(core, newMusician);
            }
            catch
            {
            }

            return newMusician;
        }

        public static bool CheckMusicianNameUnique(Core core, string musicianName)
        {
            if (core.Db.Query(string.Format("SELECT musician_slug FROM musicians WHERE LCASE(musician_slug) = '{0}';",
                Mysql.Escape(musicianName.ToLower()))).Rows.Count > 0)
            {
                return false;
            }
            return true;
        }

        public static bool CheckMusicianNameValid(string musicianName)
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
            disallowedNames.Add("create");
            disallowedNames.Add("edit");
            disallowedNames.Add("delete");
            disallowedNames.Add("remove");
            disallowedNames.Add("sid");
            disallowedNames.Add("network");
            disallowedNames.Add("networks");
            disallowedNames.Add("directory");
            disallowedNames.Add("folder");
            disallowedNames.Add("genre");
            disallowedNames.Add("genres");
            disallowedNames.Add("artist");
            disallowedNames.Add("artists");
            disallowedNames.Sort();

            if (disallowedNames.BinarySearch(musicianName.ToLower()) >= 0)
            {
                matches++;
            }

            if (!Regex.IsMatch(musicianName, @"^([A-Za-z0-9\-_\.\!~\*'&=\$].+)$"))
            {
                matches++;
            }

            musicianName = musicianName.Normalize().ToLower();

            if (musicianName.Length < 2)
            {
                matches++;
            }

            if (musicianName.Length > 64)
            {
                matches++;
            }

            if (musicianName.EndsWith(".aspx"))
            {
                matches++;
            }

            if (musicianName.EndsWith(".asax"))
            {
                matches++;
            }

            if (musicianName.EndsWith(".php"))
            {
                matches++;
            }

            if (musicianName.EndsWith(".html"))
            {
                matches++;
            }

            if (musicianName.EndsWith(".gif"))
            {
                matches++;
            }

            if (musicianName.EndsWith(".png"))
            {
                matches++;
            }

            if (musicianName.EndsWith(".js"))
            {
                matches++;
            }

            if (musicianName.EndsWith(".bmp"))
            {
                matches++;
            }

            if (musicianName.EndsWith(".jpg"))
            {
                matches++;
            }

            if (musicianName.EndsWith(".jpeg"))
            {
                matches++;
            }

            if (musicianName.EndsWith(".zip"))
            {
                matches++;
            }

            if (musicianName.EndsWith(".jsp"))
            {
                matches++;
            }

            if (musicianName.EndsWith(".cfm"))
            {
                matches++;
            }

            if (musicianName.EndsWith(".exe"))
            {
                matches++;
            }

            if (musicianName.StartsWith("."))
            {
                matches++;
            }

            if (musicianName.EndsWith("."))
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

        public override long Id
        {
            get
            {
                return musicianId;
            }
        }

        public override string Key
        {
            get
            {
                return slug;
            }
        }

        public override string Type
        {
            get
            {
                return "MUSIC";
            }
        }

        public override string AccountUriStub
        {
            get
            {
                return string.Format("/music/{0}/account/",
                    Key);
            }
        }

        public override AppPrimitives AppPrimitive
        {
            get
            {
                return AppPrimitives.Musician;
            }
        }

        public override string UriStub
        {
            get
            {
                return string.Format("/music/{0}/",
                    Key);
            }
        }

        public override string UriStubAbsolute
        {
            get
            {
                return core.Uri.AppendAbsoluteSid(UriStub);
            }
        }

        public override string Uri
        {
            get
            {
                return core.Uri.AppendSid(UriStub);
            }
        }

        public string FansUri
        {
            get
            {
                return core.Uri.AppendSid(string.Format("{0}fans",
                    UriStub));
            }
        }
        public string GetFansUri(string filter)
        {
            return core.Uri.AppendSid(string.Format("{0}fans?filter={1}",
                    UriStub, filter));
        }

        public override string TitleName
        {
            get
            {
                return name;
            }
        }

        public override string TitleNameOwnership
        {
            get { throw new NotImplementedException(); }
        }

        public override string DisplayName
        {
            get
            {
                return name;
            }
        }

        public override string DisplayNameOwnership
        {
            get { throw new NotImplementedException(); }
        }

        public override bool CanModerateComments(User member)
        {
            throw new NotImplementedException();
        }

        public override bool IsCommentOwner(User member)
        {
            throw new NotImplementedException();
        }

        public override ushort GetAccessLevel(User viewer)
        {
            throw new NotImplementedException();
        }

        public void GetCan(ushort accessBits, User viewer, out bool canRead, out bool canComment, out bool canCreate, out bool canChange)
        {
            throw new NotImplementedException();
        }

        public override string GenerateBreadCrumbs(List<string[]> parts)
        {
            string output = "";
            string path = string.Format("/music/{0}", Key);
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

        public bool IsMusicianMember(User user)
        {
            if (user != null)
            {
                if (musicianMemberCache.ContainsKey(user))
                {
                    return musicianMemberCache[user];
                }
                else
                {
                    preLoadMemberCache(user);
                    return musicianMemberCache[user];
                }
            }
            return false;
        }

        private void preLoadMemberCache(User member)
        {
            SelectQuery query = new SelectQuery("musician_members");
            query.AddCondition("musician_id", musicianId);
            query.AddCondition("user_id", member.UserId);

            DataTable memberTable = db.Query(query);

            if (memberTable.Rows.Count > 0)
            {
                musicianMemberCache.Add(member, true);
            }
            else
            {
                musicianMemberCache.Add(member, false);
            }
        }

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

        public override List<AccessControlPermission> AclPermissions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override List<PrimitivePermissionGroup> GetPrimitivePermissionGroups()
        {
            List<PrimitivePermissionGroup> ppgs = new List<PrimitivePermissionGroup>();

            ppgs.Add(new PrimitivePermissionGroup(ItemType.GetTypeId(typeof(MusicianMember)), -1, "GROUP_MEMBERS"));
            ppgs.Add(new PrimitivePermissionGroup(ItemType.GetTypeId(typeof(Fan)), -1, "FANS"));
            ppgs.Add(new PrimitivePermissionGroup(ItemType.GetTypeId(typeof(User)), -2, "EVERYONE"));

            return ppgs;
        }

        public override bool GetIsMemberOfPrimitive(User viewer, ItemKey primitiveKey)
        {
            if (core.LoggedInMemberId > 0)
            {
                return IsMusicianMember(viewer);
            }

            return false;
        }

        public override bool CanEditPermissions()
        {
            if (core.LoggedInMemberId > 0)
            {
                return IsMusicianMember(core.Session.LoggedInMember);
            }

            return false;
        }

        public override bool CanEditItem()
        {
            if (core.LoggedInMemberId > 0)
            {
                return IsMusicianMember(core.Session.LoggedInMember);
            }

            return false;
        }

        public override bool CanDeleteItem()
        {
            if (core.LoggedInMemberId > 0)
            {
                return IsMusicianMember(core.Session.LoggedInMember);
            }

            return false;
        }

        public override bool GetDefaultCan(string permission)
        {
            return false;
        }

        public override string DisplayTitle
        {
            get
            {
                return "Musician: " + DisplayName;
            }
        }

        public static List<Musician> GetMusicians(Core core, string firstLetter, int page)
        {
            return GetMusicians(core, firstLetter, null, page);
        }

        public static List<Musician> GetMusicians(Core core, string firstLetter, MusicGenre genre, int page)
        {
            List<Musician> musicians = new List<Musician>();

            SelectQuery query = Musician.GetSelectQueryStub(typeof(Musician));
            if (genre != null)
            {
                if (genre.IsSubGenre)
                {
                    query.AddCondition("musician_genre", genre.Id);
                }
                else
                {
                    query.AddCondition("musician_genre", genre.Id);
                }
            }

            if (!string.IsNullOrEmpty(firstLetter))
            {
                query.AddCondition("musician_name_first", firstLetter);
            }

            if (page >= -1)
            {
                query.LimitCount = 10;
                query.LimitStart = Functions.LimitPageToStart(page, 10);
            }

            query.AddSort(SortOrder.Ascending, "musician_slug");

            DataTable musiciansTable = core.Db.Query(query);

            foreach (DataRow dr in musiciansTable.Rows)
            {
                musicians.Add(new Musician(core, dr, MusicianLoadOptions.Common));
            }

            return musicians;
        }

        private static void prepareNewCaptcha(Core core)
        {
            // prepare the captcha
            string captchaString = Captcha.GenerateCaptchaString();

            // delete all existing for this session
            // captcha is a use once thing, destroy all for this session
            Confirmation.ClearStale(core, core.Session.SessionId, 3);

            // create a new confimation code
            Confirmation confirm = Confirmation.Create(core, core.Session.SessionId, captchaString, 3);

            core.Template.Parse("U_CAPTCHA", core.Uri.AppendSid("/captcha.aspx?secureid=" + confirm.ConfirmId.ToString(), true));
        }

        internal static void ShowRegister(object sender, ShowPageEventArgs e)
        {
            e.Template.SetTemplate("createmuiscian.html");

            if (e.Core.Session.IsLoggedIn == false)
            {
                e.Template.Parse("REDIRECT_URI", "/sign-in/?redirect=/music/register");
                e.Core.Display.ShowMessage("Not Logged In", "You must be logged in to register a new musician.");
                return;
            }

            string slug = e.Core.Http.Form["slug"];
            string title = e.Core.Http.Form["title"];

            if (string.IsNullOrEmpty(slug))
            {
                slug = title;
            }

            if (!string.IsNullOrEmpty(title))
            {
                // normalise slug if it has been fiddeled with
                slug = slug.ToLower().Normalize(NormalizationForm.FormD);
                string normalisedSlug = "";

                for (int i = 0; i < slug.Length; i++)
                {
                    if (CharUnicodeInfo.GetUnicodeCategory(slug[i]) != UnicodeCategory.NonSpacingMark)
                    {
                        normalisedSlug += slug[i];
                    }
                }
                slug = Regex.Replace(normalisedSlug, @"([\W]+)", "-");
            }

            if (e.Core.Http.Form["submit"] == null)
            {
                prepareNewCaptcha(e.Core);
            }
            else
            {
                // submit the form
                e.Template.Parse("MUSICIAN_TITLE", e.Core.Http.Form["title"]);
                e.Template.Parse("MUSICIAN_NAME_SLUG", slug);

                DataTable confirmTable = e.Db.Query(string.Format("SELECT confirm_code FROM confirm WHERE confirm_type = 3 AND session_id = '{0}' LIMIT 1",
                    Mysql.Escape(e.Core.Session.SessionId)));

                if (confirmTable.Rows.Count != 1)
                {
                    e.Template.Parse("ERROR", "Captcha error, please try again.");
                    prepareNewCaptcha(e.Core);
                }
                else if (((string)confirmTable.Rows[0]["confirm_code"]).ToLower() != e.Core.Http.Form["captcha"].ToLower())
                {
                    e.Template.Parse("ERROR", "Captcha is invalid, please try again.");
                    prepareNewCaptcha(e.Core);
                }
                else if (!Musician.CheckMusicianNameValid(slug))
                {
                    e.Template.Parse("ERROR", "Musician slug is invalid, you may only use letters, numbers, period, underscores or a dash (a-z, 0-9, '_', '-', '.').");
                    prepareNewCaptcha(e.Core);
                }
                else if (!Musician.CheckMusicianNameUnique(e.Core, slug))
                {
                    e.Template.Parse("ERROR", "Musician slug is already taken, please choose another one.");
                    prepareNewCaptcha(e.Core);
                }
                else if (e.Core.Http.Form["agree"] != "true")
                {
                    e.Template.Parse("ERROR", "You must accept the ZinZam Terms of Service to create register a musician.");
                    prepareNewCaptcha(e.Core);
                }
                else
                {
                    Musician newMusician = Musician.Create(e.Core, e.Core.Http.Form["title"], slug);

                    if (newMusician == null)
                    {
                        e.Template.Parse("ERROR", "Bad registration details");
                        prepareNewCaptcha(e.Core);
                    }
                    else
                    {
                        // captcha is a use once thing, destroy all for this session
                        e.Db.UpdateQuery(string.Format("DELETE FROM confirm WHERE confirm_type = 3 AND session_id = '{0}'",
                            Mysql.Escape(e.Core.Session.SessionId)));

                        //Response.Redirect("/", true);
                        e.Template.Parse("REDIRECT_URI", newMusician.Uri);
                        e.Core.Display.ShowMessage("Musician Registered", "You have have registered a new musician. You will be redirected to the musician home page in a second.");
                        return; /* stop processing the display of this page */
                    }
                }
            }
        }

        internal static void ShowProfile(object sender, ShowMPageEventArgs e)
        {
            e.Template.SetTemplate("Musician", "viewmusician");
        }
    }

    public class InvalidMusicianException : Exception
    {
    }
}
