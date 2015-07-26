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

        public string SearchProvider
        {
            get
            {
                return WebConfigurationManager.AppSettings["search-provider"];
            }
        }

        public string MailProvider
        {
            get
            {
                return WebConfigurationManager.AppSettings["mail-provider"];
            }
        }

        public string SmsProvider
        {
            get
            {
                return WebConfigurationManager.AppSettings["sms-provider"];
            }
        }

        public string SmsHttpGateway
        {
            get
            {
                return WebConfigurationManager.AppSettings["sms-http-gateway"];
            }
        }

        public string SmsOAuthTokenUri
        {
            get
            {
                return WebConfigurationManager.AppSettings["sms-oauth-token-uri"];
            }
        }

        public string SmsOAuthSmsUri
        {
            get
            {
                return WebConfigurationManager.AppSettings["sms-oauth-sms-uri"];
            }
        }

        public string SmsOAuthKey
        {
            get
            {
                return WebConfigurationManager.AppSettings["sms-oauth-key"];
            }
        }

        public string SmsOAuthSecret
        {
            get
            {
                return WebConfigurationManager.AppSettings["sms-oauth-secret"];
            }
        }

        public string QueueProvider
        {
            get
            {
                return WebConfigurationManager.AppSettings["queue-provider"];
            }
        }

        public string QueueDefaultPriority
        {
            get
            {
                return WebConfigurationManager.AppSettings["queue-default-priority"];
            }
        }

        public string QueueNotifications
        {
            get
            {
                if (!string.IsNullOrEmpty(WebConfigurationManager.AppSettings["queue-default-priority"]))
                {
                    return WebConfigurationManager.AppSettings["queue-default-priority"];
                }
                else
                {
                    return QueueDefaultPriority;
                }
            }
        }

        public string ImagemagickTempPath
        {
            get
            {
                return WebConfigurationManager.AppSettings["imagemagick-temp-path"];
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

        public bool UseCdn
        {
            get
            {
                return WebConfigurationManager.AppSettings["cdn-enabled"] == "true";
            }
        }

        public string CdnStorageBucketDomain
        {
            get
            {
                if (core.Http.IsSecure)
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-secure-storage"];
                }
                else
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-storage"];
                }
            }
        }

        public string CdnIconBucketDomain
        {
            get
            {
                if (core.Http.IsSecure)
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-secure-icon"];
                }
                else
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-icon"];
                }
            }
        }

        public string CdnTileBucketDomain
        {
            get
            {
                if (core.Http.IsSecure)
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-secure-tile"];
                }
                else
                {
                return WebConfigurationManager.AppSettings["cdn-domain-tile"];
                }
            }
        }

        public string CdnSquareBucketDomain
        {
            get
            {
                if (core.Http.IsSecure)
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-secure-square"];
                }
                else
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-square"];
                }
            }
        }

        public string CdnHighBucketDomain
        {
            get
            {
                if (core.Http.IsSecure)
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-secure-high"];
                }
                else
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-high"];
                }
            }
        }

        public string CdnTinyBucketDomain
        {
            get
            {
                if (core.Http.IsSecure)
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-secure-tiny"];
                }
                else
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-tiny"];
                }
            }
        }

        public string CdnThumbBucketDomain
        {
            get
            {
                if (core.Http.IsSecure)
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-secure-thumb"];
                }
                else
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-thumb"];
                }
            }
        }

        public string CdnMobileBucketDomain
        {
            get
            {
                if (core.Http.IsSecure)
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-secure-mobile"];
                }
                else
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-mobile"];
                }
            }
        }

        public string CdnDisplayBucketDomain
        {
            get
            {
                if (core.Http.IsSecure)
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-secure-display"];
                }
                else
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-display"];
                }
            }
        }

        public string CdnFullBucketDomain
        {
            get
            {
                if (core.Http.IsSecure)
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-secure-full"];
                }
                else
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-full"];
                }
            }
        }

        public string CdnUltraBucketDomain
        {
            get
            {
                if (core.Http.IsSecure)
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-secure-ultra"];
                }
                else
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-ultra"];
                }
            }
        }

        public string CdnCoverBucketDomain
        {
            get
            {
                if (core.Http.IsSecure)
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-secure-cover"];
                }
                else
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-cover"];
                }
            }
        }

        public string CdnMobileCoverBucketDomain
        {
            get
            {
                if (core.Http.IsSecure)
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-secure-mobile-cover"];
                }
                else
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-mobile-cover"];
                }
            }
        }

        public string CdnStaticBucketDomain
        {
            get
            {
                if (core.Http.IsSecure)
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-secure-static"];
                }
                else
                {
                    return WebConfigurationManager.AppSettings["cdn-domain-static"];
                }
            }
        }

        public bool UseSecureCookies
        {
            get
            {
                return (WebConfigurationManager.AppSettings["secure-cookies"].ToLower() == "true");
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

        public bool UseTwitterAuthentication
        {
            get
            {
                return WebConfigurationManager.AppSettings["oauth-twitter"] == "true";
            }
        }

        public bool UseGoogleAuthentication
        {
            get
            {
                return WebConfigurationManager.AppSettings["oauth-google"] == "true";
            }
        }

        public bool UseFacebookAuthentication
        {
            get
            {
                return WebConfigurationManager.AppSettings["oauth-facebook"] == "true";
            }
        }

        public bool FacebookEnabled
        {
            get
            {
                return WebConfigurationManager.AppSettings["facebook-api-enabled"] == "true";
            }
        }

        public string TwitterName
        {
            get
            {
                return WebConfigurationManager.AppSettings["twitter-name"];
            }
        }

        public string TwitterApiKey
        {
            get
            {
                return WebConfigurationManager.AppSettings["twitter-api-key"];
            }
        }

        public string TwitterApiSecret
        {
            get
            {
                return WebConfigurationManager.AppSettings["twitter-api-secret"];
            }
        }

        public string GoogleApiKey
        {
            get
            {
                return WebConfigurationManager.AppSettings["google-api-key"];
            }
        }

        public string GoogleApiSecret
        {
            get
            {
                return WebConfigurationManager.AppSettings["google-api-secret"];
            }
        }

        public string FacebookApiAppid
        {
            get
            {
                return WebConfigurationManager.AppSettings["facebook-api-appid"];
            }
        }

        public string FacebookApiSecret
        {
            get
            {
                return WebConfigurationManager.AppSettings["facebook-api-secret"];
            }
        }

        public string TumblrApiKey
        {
            get
            {
                return WebConfigurationManager.AppSettings["tumblr-api-key"];
            }
        }

        public string TumblrApiSecret
        {
            get
            {
                return WebConfigurationManager.AppSettings["tumblr-api-secret"];
            }
        }

        public string WindowsNotificationKey
        {
            get
            {
                return WebConfigurationManager.AppSettings["windows-notification-key"];
            }
        }

        public string WindowsNotificationSecret
        {
            get
            {
                return WebConfigurationManager.AppSettings["windows-notification-secret"];
            }
        }
    }
}
