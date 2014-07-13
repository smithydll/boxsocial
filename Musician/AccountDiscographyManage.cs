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
    [AccountSubModule(AppPrimitives.Musician, "music", "discography", true)]
    public class AccountDiscographyManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Manage Discography";
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
        /// Initializes a new instance of the AccountDiscographyManage class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountDiscographyManage(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountDiscographyManage_Load);
            this.Show += new EventHandler(AccountDiscographyManage_Show);
        }

        void AccountDiscographyManage_Load(object sender, EventArgs e)
        {
            this.AddModeHandler("add", AccountDiscographyManage_Edit);
            this.AddModeHandler("edit", AccountDiscographyManage_Edit);
            this.AddModeHandler("delete", AccountDiscographyManage_Delete);
        }

        void AccountDiscographyManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_discography");

        }

        void AccountDiscographyManage_Edit(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_discography_album_edit");

            /* */
            TextBox titleTextBox = new TextBox("title");
            titleTextBox.MaxLength = 63;

            /* */
            SelectBox releaseTypeSelectBox = new SelectBox("release-type");
            releaseTypeSelectBox.Add(new SelectBoxItem(((byte)ReleaseType.Demo).ToString(), "Demo"));
            releaseTypeSelectBox.Add(new SelectBoxItem(((byte)ReleaseType.Single).ToString(), "Single"));
            releaseTypeSelectBox.Add(new SelectBoxItem(((byte)ReleaseType.Album).ToString(), "Album"));
            releaseTypeSelectBox.Add(new SelectBoxItem(((byte)ReleaseType.EP).ToString(), "EP"));
            releaseTypeSelectBox.Add(new SelectBoxItem(((byte)ReleaseType.DVD).ToString(), "DVD"));
            releaseTypeSelectBox.Add(new SelectBoxItem(((byte)ReleaseType.Compilation).ToString(), "Compilation"));

            switch (e.Mode)
            {
                case "add":

                    releaseTypeSelectBox.SelectedKey = ((byte)ReleaseType.Demo).ToString();
                    break;
                case "edit":
                    long releaseId = core.Functions.FormLong("id", core.Functions.RequestLong("id", 0));

                    Release release = null;

                    try
                    {
                        release = new Release(core, releaseId);

                        titleTextBox.Value = release.Title;
                        releaseTypeSelectBox.SelectedKey = ((byte)release.ReleaseType).ToString();
                    }
                    catch (InvalidReleaseException)
                    {
                        return;
                    }
                    break;
            }

            template.Parse("S_TITLE", titleTextBox);

            SaveMode(AccountDiscographyManage_EditSave);
        }

        void AccountDiscographyManage_EditSave(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            string title = Functions.TrimStringToWord(core.Http.Form["title"], 63);
            ReleaseType releaseType = (ReleaseType)core.Functions.FormByte("release-type", (byte)ReleaseType.Demo);

            Release release = null;

            switch (e.Mode)
            {
                case "add":
                    // TODO:
                    release = Release.Create(core, (Musician)Owner, releaseType, title, -1);
                    SetRedirectUri(BuildUri());
                    break;
                case "edit":
                    long releaseId = core.Functions.FormLong("id", 0);

                    try
                    {
                        release = new Release(core, releaseId);
                    }
                    catch (InvalidReleaseException)
                    {
                        // TODO: throw exception
                        return;
                    }

                    if (release.Musician.Id != Owner.Id)
                    {
                        // TODO: throw exception
                        return;
                    }

                    release.Title = title;
                    release.ReleaseType = releaseType;

                    release.Update();

                    SetInformation("Release information updated");
                    break;
            }
        }

        void AccountDiscographyManage_Delete(object sender, ModuleModeEventArgs e)
        {
        }
    }
}
