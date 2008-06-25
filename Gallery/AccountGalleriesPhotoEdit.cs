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
    [AccountSubModule("galleries", "edit-photo")]
    public class AccountGalleriesPhotoEdit : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return null;
            }
        }

        public override int Order
        {
            get
            {
                return -1;
            }
        }

        public AccountGalleriesPhotoEdit()
        {
            this.Load += new EventHandler(AccountGalleriesPhotoEdit_Load);
            this.Show += new EventHandler(AccountGalleriesPhotoEdit_Show);
        }

        void AccountGalleriesPhotoEdit_Load(object sender, EventArgs e)
        {
        }

        void AccountGalleriesPhotoEdit_Show(object sender, EventArgs e)
        {
            Save(new EventHandler(AccountGalleriesPhotoEdit_Save));
        }

        void AccountGalleriesPhotoEdit_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();
        }
    }
}
