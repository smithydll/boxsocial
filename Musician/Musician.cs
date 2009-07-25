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
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Musician
{
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
    public class Musician : Primitive
    {
        [DataField("musician_id")]
        private long musicianId;
        [DataField("musician_name", 63)]
        private string name;
        [DataField("musician_slug", 63)]
        private string slug;
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

        private Dictionary<User, bool> musicianMemberCache = new Dictionary<User, bool>();

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

        public Musician(Core core, long musicianId)
            : base(core)
        {
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
            try
            {
                LoadItem("muscian_slug", slug);
            }
            catch (InvalidItemException)
            {
                throw new InvalidMusicianException();
            }
        }

        public static Musician Create(Core core, string title, string slug)
        {
            Mysql db = core.db;
            SessionState session = core.session;

            if (core.session.LoggedInMember == null)
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

            long musicianId = db.Query(iQuery);

            db.UpdateQuery(string.Format("INSERT INTO musician_members (user_id, musician_id, musician_member_date_ut) VALUES ({0}, {1}, UNIX_TIMESTAMP())",
                session.LoggedInMember.UserId, musicianId));

            Musician newMusician = new Musician(core, musicianId);

            // Install a couple of applications
            try
            {
                ApplicationEntry profileAe = new ApplicationEntry(core, null, "Profile");
                profileAe.Install(core, newMusician);
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
            if (core.db.Query(string.Format("SELECT musician_slug FROM musicians WHERE LCASE(musician_slug) = '{0}';",
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

        public override void GetCan(ushort accessBits, User viewer, out bool canRead, out bool canComment, out bool canCreate, out bool canChange)
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
    }

    public class InvalidMusicianException : Exception
    {
    }
}
