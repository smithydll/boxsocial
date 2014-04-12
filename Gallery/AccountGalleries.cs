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
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Applications.Gallery;

namespace BoxSocial.Applications.Gallery
{
    /// <summary>
    /// 
    /// </summary>
    [AccountModule("galleries")]
    public class AccountGalleries : AccountModule
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="account"></param>
        public AccountGalleries(Account account)
            : base(account)
        {
            //RegisterSubModule += new RegisterSubModuleHandler(ManageGalleries);
            // TODO: Gallery Preferences
            //RegisterSubModule += new RegisterSubModuleHandler(UploadPhoto);
            //RegisterSubModule += new RegisterSubModuleHandler(NewGallery);
            //RegisterSubModule += new RegisterSubModuleHandler(MarkPhotoAsDisplayPicture);
            //RegisterSubModule += new RegisterSubModuleHandler(MarkPhotoAsGalleryCover);
            //RegisterSubModule += new RegisterSubModuleHandler(EditPhoto);
            //RegisterSubModule += new RegisterSubModuleHandler(RotatePhoto);
            //RegisterSubModule += new RegisterSubModuleHandler(PhotoDelete);
            //RegisterSubModule += new RegisterSubModuleHandler(PhotoTag);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="e"></param>
        protected override void RegisterModule(Core core, EventArgs e)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public override string Name
        {
            get
            {
                return core.Prose.GetString("GALLERIES");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override int Order
        {
            get
            {
                return 6;
            }
        }

    }
}
