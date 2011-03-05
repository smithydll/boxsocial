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

namespace BoxSocial.Applications.Gallery
{
    [AccountSubModule(AppPrimitives.Any, "galleries", "settings")]
    class AccountGalleriesSettings : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Settings";
            }
        }

        public override int Order
        {
            get
            {
                return 2;
            }
        }

        public AccountGalleriesSettings(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountGalleriesSettings_Load);
            this.Show += new EventHandler(AccountGalleriesSettings_Show);
        }

        void AccountGalleriesSettings_Load(object sender, EventArgs e)
        {
            
        }

        void AccountGalleriesSettings_Show(object sender, EventArgs e)
        {
            SetTemplate("account_galleries_settings");

            Save(new EventHandler(AccountGalleriesSettings_Save));

            GallerySettings settings;
            try
            {
                settings = new GallerySettings(core, Owner);
            }
            catch (InvalidGallerySettingsException)
            {
                GallerySettings.Create(core, Owner);
                settings = new GallerySettings(core, Owner);
            }
        }

        void AccountGalleriesSettings_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            GallerySettings settings = new GallerySettings(core, Owner);

            settings.Update();

            this.SetInformation("Gallery Settings Saved");
        }
    }
}
