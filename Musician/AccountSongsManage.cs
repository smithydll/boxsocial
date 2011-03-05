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
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Musician
{
    [AccountSubModule(AppPrimitives.Musician, "music", "songs", true)]
    public class AccountSongsManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Manage Songs";
            }
        }

        public override int Order
        {
            get
            {
                return 1;
            }
        }

        public AccountSongsManage(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountSongsManage_Load);
            this.Show += new EventHandler(AccountSongsManage_Show);
        }

        void AccountSongsManage_Load(object sender, EventArgs e)
        {
            AddModeHandler("add", new ModuleModeHandler(AccountSongsManage_Add));
            AddModeHandler("edit", new ModuleModeHandler(AccountSongsManage_Add));
        }

        void AccountSongsManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_songs");

            List<Song> songs = ((Musician)Owner).GetSongs();

            foreach (Song song in songs)
            {
                VariableCollection songsVariableCollection = template.CreateChild("song_list");

                songsVariableCollection.Parse("ID", song.Id.ToString());
                songsVariableCollection.Parse("TITLE", song.Title);
                songsVariableCollection.Parse("RECORDINGS", song.Recordings.ToString());
                songsVariableCollection.Parse("U_EDIT", BuildUri("songs", "edit", song.Id));
                songsVariableCollection.Parse("U_ADD_RECORDING", BuildUri("recordings", "add", song.Id));
                songsVariableCollection.Parse("U_DELETE", BuildUri("songs", "delete", song.Id));
            }

            template.Parse("U_ADD_SONG", BuildUri("songs", "add"));
        }

        void AccountSongsManage_Add(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_song_edit");

            if (e.Mode == "edit")
            {
                Song song = null;

                try
                {
                    song = new Song(core, core.Functions.RequestLong("id", 0));
                }
                catch (InvalidSongException)
                {
                    core.Display.ShowMessage("Error", "Cannot edit the song");
                    return;
                }

                template.Parse("S_TITLE", song.Title);
                template.Parse("S_LYRICS", song.Lyrics);
                template.Parse("S_MODE", "edit");
                template.Parse("S_ID", song.Id.ToString());

                core.Display.ParseLicensingBox(template, "S_LICENSE", song.LicenseId);
            }
            else
            {
                template.Parse("S_MODE", "add");

                core.Display.ParseLicensingBox(template, "S_LICENSE", 0);
            }

            SaveMode(AccountSongsManage_Save);
        }

        void AccountSongsManage_Save(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            if (e.Mode == "edit")
            {
                Song song = null;

                try
                {
                    song = new Song(core, core.Functions.FormLong("id", 0));
                }
                catch (InvalidSongException)
                {
                    core.Display.ShowMessage("Error", "Cannot edit the song");
                    return;
                }

                song.Title = core.Http.Form["title"];
                song.Lyrics = core.Http.Form["lyrics"];
                song.LicenseId = core.Functions.GetLicenseId();

                try
                {
                    song.Update();
                }
                catch (UnauthorisedToUpdateItemException)
                {
                    core.Display.ShowMessage("Unauthorised", "Unauthorised to update song");
                    return;
                }

                this.SetRedirectUri(BuildUri("songs"));
                core.Display.ShowMessage("Song Saved", "The song has been updated in the database.");
            }
            else
            {
                Song song = Song.Create(core, (Musician)Owner, core.Http.Form["title"], core.Http.Form["lyrics"], core.Functions.GetLicenseId());

                this.SetRedirectUri(BuildUri("songs"));
                core.Display.ShowMessage("Song Saved", "The song has been saved in the database.");
            }
        }
    }
}
