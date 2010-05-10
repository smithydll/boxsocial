﻿/*
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
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Musician
{
    public class AppInfo : Application
    {

        public AppInfo(Core core)
            : base(core)
        {
            core.AddPrimitiveType(typeof(Musician));
        }

        public override string Title
        {
            get
            {
                return "Music";
            }
        }

        public override string Stub
        {
            get
            {
                return "music";
            }
        }

        public override string Description
        {
            get
            {
                return string.Empty;
            }
        }

        public override bool UsesComments
        {
            get
            {
                return true;
            }
        }

        public override bool UsesRatings
        {
            get
            {
                return false;
            }
        }

        public override System.Drawing.Image Icon
        {
            get
            {
                //return System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("profile");
                return null;
            }
        }

        public override byte[] SvgIcon
        {
            get
            {
                return Properties.Resources.svgIcon;
            }
        }

        public override string StyleSheet
        {
            get
            {
                return Properties.Resources.style;
            }
        }

        public override string JavaScript
        {
            get
            {
                return Properties.Resources.script;
            }
        }

        public override void Initialise(Core core)
        {
            core.PageHooks += new Core.HookHandler(core_PageHooks);
            core.FootHooks += new Core.HookHandler(core_FootHooks);
            core.LoadApplication += new Core.LoadHandler(core_LoadApplication);

            core.RegisterCommentHandle(ItemKey.GetTypeId(typeof(Gig)), gigCanPostComment, gigCanDeleteComment, gigAdjustCommentCount, gigCommentPosted);
        }

        private void gigCommentPosted(CommentPostedEventArgs e)
        {
        }

        private bool gigCanPostComment(ItemKey itemKey, User member)
        {
            SelectQuery query = Gig.GetSelectQueryStub(typeof(Gig), false);
            query.AddCondition("gig_id", itemKey.Id);

            DataTable gigTable = core.Db.Query(query);

            if (gigTable.Rows.Count == 1)
            {
                Musician owner = new Musician(core, (long)gigTable.Rows[0]["musician_id"]);

                if (owner.Access.Can("COMMENT_GIGS"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                throw new InvalidItemException();
            }
        }

        private bool gigCanDeleteComment(ItemKey itemKey, User member)
        {
            SelectQuery query = Gig.GetSelectQueryStub(typeof(Gig), false);
            query.AddCondition("gig_id", itemKey.Id);

            DataTable gigTable = core.Db.Query(query);

            if (gigTable.Rows.Count == 1)
            {
                Musician owner = new Musician(core, (long)gigTable.Rows[0]["musician_id"]);

                if (owner.IsMusicianMember(member))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                throw new InvalidItemException();
            }
        }

        private void gigAdjustCommentCount(ItemKey itemKey, int adjustment)
        {
            /*core.db.UpdateQuery(string.Format("UPDATE music_gigs SET gig_comments = gig_comments + {1} WHERE gig_id = {0};",
                itemKey.Id, adjustment));*/

            Item.IncrementItemColumn(core, typeof(Gig), itemKey.Id, "gig_comments", adjustment);
        }

        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = this.GetInstallInfo();

            /*aii.AddSlug("profile", @"^/profile(|/)$", AppPrimitives.Musician);
            aii.AddSlug("members", @"^/members(|/)$", AppPrimitives.Musician);
            aii.AddSlug("fans", @"^/fans(|/)$", AppPrimitives.Musician);

            aii.AddModule("music");*/

            return aii;
        }

        public override Dictionary<string, string> PageSlugs
        {
            get
            {
                Dictionary<string, string> slugs = new Dictionary<string, string>();
                //slugs.Add("profile", "Profile");
                //slugs.Add("tours", "Tours");
                //slugs.Add("gigs", "Gigs");
                //slugs.Add("discography", "Discography");
                return slugs;
            }
        }

        void core_LoadApplication(Core core, object sender)
        {
            /*core.RegisterApplicationPage(@"^/profile(|/)$", showMusician);
            core.RegisterApplicationPage(@"^/members(|/)$", showMemberlist);*/
        }

        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.None | AppPrimitives.Member | AppPrimitives.Musician;
        }

        void core_PageHooks(HookEventArgs e)
        {
            if (e.PageType == AppPrimitives.Member)
            {
                ShowMemberMusicians(e);
            }
        }

        void core_FootHooks(HookEventArgs e)
        {
        }

        public void ShowMemberMusicians(HookEventArgs e)
        {
            User profileOwner = (User)e.Owner;
            Template template = new Template(Assembly.GetExecutingAssembly(), "viewprofilemusicians");

            /*List<UserGroup> groups = UserGroup.GetUserGroups(e.core, profileOwner);
            if (groups.Count > 0)
            {
                template.Parse("HAS_GROUPS", "TRUE");
            }

            foreach (UserGroup group in groups)
            {
                VariableCollection groupVariableCollection = template.CreateChild("groups_list");

                groupVariableCollection.Parse("TITLE", group.DisplayName);
                groupVariableCollection.Parse("U_GROUP", group.Uri);
            }*/

            e.core.AddSidePanel(template);
        }

        [StaticShow("music", @"^/music/register(|/)$")]
        private void showCreateMusician(Core core, object sender)
        {
            if (sender is TPage)
            {
                Musician.ShowRegister(sender, new ShowPageEventArgs((TPage)sender));
            }
        }

        [StaticShow("music", @"^/music(|/)$")]
        private void showDefault(Core core, object sender)
        {
            if (sender is TPage)
            {
                Default.Show(sender, new ShowPageEventArgs((TPage)sender));
            }
        }

        [StaticShow("music", @"^/music/chart(|/)$")]
        private void showChart(Core core, object sender)
        {
            if (sender is TPage)
            {
                Default.ShowChart(sender, new ShowPageEventArgs((TPage)sender));
            }
        }

        [StaticShow("music", @"^/music/chart/artists(|/)$")]
        private void showChartArtists(Core core, object sender)
        {
            if (sender is TPage)
            {
                //Default.ShowArtistChart(sender, new ShowPageEventArgs((TPage)sender));
            }
        }

        [StaticShow("music", @"^/music/chart/albums(|/)$")]
        private void showChartAlbums(Core core, object sender)
        {
            //Default.ShowAlbumChart(sender, new ShowPageEventArgs((TPage)sender));
        }

        [StaticShow("music", @"^/music/chart/singles(|/)$")]
        private void showChartSingles(Core core, object sender)
        {
            //Default.ShowSinglesChart(sender, new ShowPageEventArgs((TPage)sender));
        }

        [StaticShow("music", @"^/music/directory(|/)$")]
        private void showDirectory(Core core, object sender)
        {
            if (sender is TPage)
            {
                Directory.Show(sender, new ShowPageEventArgs((TPage)sender));
            }
        }

        [StaticShow("music", @"^/music/directory/genres(|/)$")]
        private void showGenres(Core core, object sender)
        {
            if (sender is TPage)
            {
                Directory.ShowGenres(sender, new ShowPageEventArgs((TPage)sender));
            }
        }

        [StaticShow("music", @"^/music/directory/genre/([a-z0-9\-_\+]+)(|/)$")]
        private void showGenre(Core core, object sender)
        {
            if (sender is TPage)
            { 
                Directory.ShowGenre(sender, new ShowPageEventArgs((TPage)sender));
            }
        }

        [PageSlug("Profile")]
        [Show(@"^/profile(|/)$", AppPrimitives.Musician)]
        private void showProfile(Core core, object sender)
        {
            if (sender is MPage)
            {
                Musician.ShowProfile(sender, new ShowMPageEventArgs((MPage)sender));
            }
        }

        [PageSlug("Members")]
        [Show(@"^/profile/members(|/)$", AppPrimitives.Musician)]
        private void showMembers(Core core, object sender)
        {
            if (sender is MPage)
            {
                MusicianMember.ShowAll(sender, new ShowMPageEventArgs((MPage)sender));
            }
        }

        [Show(@"^/profile/members/([a-z0-9\-_\+]+)(|/)$", AppPrimitives.Musician)]
        private void showMember(Core core, object sender)
        {
            if (sender is MPage)
            {
                MusicianMember.Show(sender, new ShowMPageEventArgs((MPage)sender, core.PagePathParts[1].Value));
            }
        }

        [PageSlug("Tours")]
        [Show(@"^/tours(|/)$", AppPrimitives.Musician)]
        private void showTours(Core core, object sender)
        {
            if (sender is MPage)
            {
                Tour.Show(core, (MPage)sender);
            }
        }

        [Show(@"^/tours/([0-9]+)(|/)$", AppPrimitives.Musician)]
        private void showTour(Core core, object sender)
        {
            if (sender is MPage)
            {
                Tour.Show(core, (MPage)sender, long.Parse(core.PagePathParts[1].Value));
            }
        }

        [PageSlug("Gigs")]
        [Show(@"^/gigs(|/)$", AppPrimitives.Musician)]
        private void showGigs(Core core, object sender)
        {
            if (sender is MPage)
            {
                Gig.ShowAll(sender, new ShowMPageEventArgs((MPage)sender));
            }
        }

        [Show(@"^/gigs/([0-9]+)(|/)$", AppPrimitives.Musician)]
        private void showGig(Core core, object sender)
        {
            if (sender is MPage)
            {
                Gig.Show(sender, new ShowMPageEventArgs((MPage)sender, long.Parse(core.PagePathParts[1].Value)));
            }
            else
            {
                core.Functions.Generate404();
                return;
            }
        }

        [PageSlug("Fans")]
        [Show(@"^/fans(|/)$", AppPrimitives.Musician)]
        private void showFans(Core core, object sender)
        {
            if (sender is MPage)
            {
                Fan.ShowAll(sender, new ShowMPageEventArgs((MPage)sender));
            }
        }

        [PageSlug("Discography")]
        [Show(@"^/discography(|/)$", AppPrimitives.Musician)]
        private void showDiscography(Core core, object sender)
        {
            if (sender is MPage)
            {
                Release.ShowDiscography(sender, new ShowMPageEventArgs((MPage)sender));
            }
        }

        [PageSlug("Songs")]
        [Show(@"^/songs(|/)$", AppPrimitives.Musician)]
        private void showSongs(Core core, object sender)
        {
            if (sender is MPage)
            {
                //Song.ShowAll(sender, new ShowMPageEventArgs((MPage)sender));
            }
        }

        [Show(@"^/songs/([0-9]+)(|/)$", AppPrimitives.Musician)]
        private void showSong(Core core, object sender)
        {
            if (sender is MPage)
            {
                Song.Show(sender, new ShowMPageEventArgs((MPage)sender, long.Parse(core.PagePathParts[1].Value)));
            }
            else
            {
                core.Functions.Generate404();
                return;
            }
        }
    }
}
