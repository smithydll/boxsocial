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
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Web;
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Musician
{
    [AccountSubModule(AppPrimitives.Musician, "music", "profile", true)]
    public class AccountProfileManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Profile";
            }
        }

        public override int Order
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountProfileManage class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountProfileManage(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountProfileManage_Load);
            this.Show += new EventHandler(AccountProfileManage_Show);
        }

        void AccountProfileManage_Load(object sender, EventArgs e)
        {
        }

        void AccountProfileManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_profile");

            Musician musician = (Musician)Owner;

            /* */
            TextBox biographyTextBox = new TextBox("biography");
            biographyTextBox.IsFormatted = true;
            biographyTextBox.Lines = 7;

            /* */
            TextBox homepageTextBox = new TextBox("homepage");
            homepageTextBox.MaxLength = 1024;

            /* */
            TextBox nameTextBox = new TextBox("name");
            nameTextBox.IsDisabled = true;
            nameTextBox.MaxLength = 63;

            /* */
            SelectBox genreSelectBox = new SelectBox("genre");

            /* */
            SelectBox musicianTypeSelectBox = new SelectBox("musician-type");

            List<MusicGenre> genres = MusicGenre.GetGenres(core);

            foreach (MusicGenre genre in genres)
            {
                genreSelectBox.Add(new SelectBoxItem(genre.Id.ToString(), genre.Name));
            }

            musicianTypeSelectBox.Add(new SelectBoxItem(((byte)MusicianType.Musician).ToString(), "Musician"));
            musicianTypeSelectBox.Add(new SelectBoxItem(((byte)MusicianType.Duo).ToString(), "Duo"));
            musicianTypeSelectBox.Add(new SelectBoxItem(((byte)MusicianType.Trio).ToString(), "Trio"));
            musicianTypeSelectBox.Add(new SelectBoxItem(((byte)MusicianType.Quartet).ToString(), "Quartet"));
            musicianTypeSelectBox.Add(new SelectBoxItem(((byte)MusicianType.Quintet).ToString(), "Quintet"));
            musicianTypeSelectBox.Add(new SelectBoxItem(((byte)MusicianType.Band).ToString(), "Band"));
            musicianTypeSelectBox.Add(new SelectBoxItem(((byte)MusicianType.Group).ToString(), "Group"));
            musicianTypeSelectBox.Add(new SelectBoxItem(((byte)MusicianType.Orchestra).ToString(), "Orchestra"));
            musicianTypeSelectBox.Add(new SelectBoxItem(((byte)MusicianType.Choir).ToString(), "Choir"));

            biographyTextBox.Value = musician.Biography;
            homepageTextBox.Value = musician.Homepage;
            nameTextBox.Value = musician.TitleName;
            musicianTypeSelectBox.SelectedKey = ((byte)musician.MusicianType).ToString();

            template.Parse("S_BIOGRAPHY", biographyTextBox);
            template.Parse("S_HOMEPAGE", homepageTextBox);
            template.Parse("S_NAME", nameTextBox);
            template.Parse("S_GENRE", genreSelectBox);
            template.Parse("S_MUSICIAN_TYPE", musicianTypeSelectBox);

            Save(AccountProfileManage_Save);
        }

        void AccountProfileManage_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();
            Musician musician = (Musician)Owner;

            long myGenre = core.Functions.FormLong("genre", musician.GenreRaw);
            MusicianType musicianType = (MusicianType)core.Functions.FormByte("musician-type", (byte)musician.MusicianType);

            musician.Biography = core.Http.Form["biography"];
            musician.Homepage = core.Http.Form["homepage"];
            musician.GenreRaw = myGenre;
            musician.MusicianType = musicianType;

            musician.Update();

            SetRedirectUri(BuildUri());
            core.Display.ShowMessage("Profile Updated", "Your musician profile has been updated.");
        }
    }
}
