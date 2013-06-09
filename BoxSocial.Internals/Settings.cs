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
using System.Text;
using System.Web;
using System.Web.Configuration;

namespace BoxSocial.Internals
{
    public class Settings
    {
        private Core core;

        public string SiteTitle
        {
            get
            {
                return WebConfigurationManager.AppSettings["boxsocial-title"];
            }
        }

        public string SiteSlogan
        {
            get
            {
                return WebConfigurationManager.AppSettings["boxsocial-slogan"];
            }
        }

        public string StorageProvider
        {
            get
            {
                return WebConfigurationManager.AppSettings["storage-provider"];
            }
        }

        public string StorageRootUserFiles
        {
            get
            {
                return WebConfigurationManager.AppSettings["storage-root"];
            }
        }

        public string StorageBinUserFilesPrefix
        {
            get
            {
                return WebConfigurationManager.AppSettings["storage-bin"];
            }
        }

        public Settings(Core core)
        {
            this.core = core;
        }

        public long MaxStorage
        {
            get
            {
                long bytes = 1024L * 1024L * 1024L * 1024L;
                if (long.TryParse(WebConfigurationManager.AppSettings["storage-max"], out bytes))
                {
                    return bytes;
                }
                else
                {
                    return 1024L * 1024L * 1024L * 1024L;
                }
            }
        }

        public long MaxUserStorage
        {
            get
            {
                long bytes = 1024L * 1024L * 1024L;
                if (long.TryParse(WebConfigurationManager.AppSettings["storage-user-max"], out bytes))
                {
                    return bytes;
                }
                else
                {
                    return 1024L * 1024L * 1024L;
                }
            }
        }

        public long MaxFileSize
        {
            get
            {
                long bytes = 10L * 1024L * 1024L;
                if (long.TryParse(WebConfigurationManager.AppSettings["storage-file-max"], out bytes))
                {
                    return bytes;
                }
                else
                {
                    return 10L * 1024L * 1024L;
                }
            }
        }

        public string SignupMode
        {
            get
            {
                return WebConfigurationManager.AppSettings["signup-mode"];
            }
        }

        public int MaxSignups
        {
            get
            {
                int max = 0;
                int.TryParse(WebConfigurationManager.AppSettings["signups-max"], out max);
                return max;
            }
        }

        public int MaxInvitesPerUser
        {
            get
            {
                int max = 0;
                int.TryParse(WebConfigurationManager.AppSettings["invites-user-max"], out max);
                return max;
            }
        }
    }
}
