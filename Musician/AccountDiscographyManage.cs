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

        public AccountDiscographyManage()
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

            TextBox titleTextBox = new TextBox("title");
            titleTextBox.MaxLength = 63;

            switch (e.Mode)
            {
                case "add":
                    break;
                case "edit":
                    long releaseId = core.Functions.FormLong("id", core.Functions.RequestLong("id", 0));

                    Release release = null;

                    try
                    {
                        release = new Release(core, releaseId);

                        titleTextBox.Value = release.Title;
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

            switch (e.Mode)
            {
                case "add":
                    // TODO:
                    Release release = Release.Create(core, (Musician)Owner, title, -1);
                    SetRedirectUri(BuildUri());
                    break;
                case "edit":
                    break;
            }
        }

        void AccountDiscographyManage_Delete(object sender, ModuleModeEventArgs e)
        {
        }
    }
}
